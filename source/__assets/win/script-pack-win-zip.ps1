#Requires -Version 7.0
<#
.SYNOPSIS
    Build (and optionally sign) the portable ZIP of ImageGlass.Win32, one .zip per architecture.

.DESCRIPTION
    Produces the archive published on GitHub Releases for users who do not want an installer:
    a self-contained AOT build plus the shared app assets (_themes, _credits, _ext_icons), packed
    under a single top-level folder named after the archive so extracting cannot scatter files.

    The archive is PORTABLE by default: an empty ".igportable" marker file is written next to
    ImageGlass.exe, which makes the app keep its settings (igconfig.json, _cache, _logs, _plugins,
    ...) in its own folder instead of %LocalAppData%\ImageGlass. The whole folder can then be moved
    or carried on a removable drive without losing the settings. Pass -NoPortable to omit the
    marker, so the archive behaves like the MSIX/installer build and stores settings per-user.

    A portable folder must be writable: if the marker is present in a folder the app cannot read and
    write (e.g. Program Files), the app reports the error and quits instead of silently falling back
    to %LocalAppData% behind the user's back. Do NOT ship the marker in the MSIX package: its
    payload folder is read-only, so every launch would fail.

    Output: __artifacts/bundle/ImageGlass_<label>_win-<arch>.zip

.PARAMETER Platform
    Target architecture: x64 (default) or arm64.

.PARAMETER Sign
    Authenticode-sign every payload .exe / .dll before packing (a ZIP itself cannot be signed).
    If no certificate is found the archive is still built, unsigned, with a warning.

.PARAMETER CertSubject
    Substring of the code-signing certificate Subject to select it from the Current User /
    Local Machine "My" store (passed to signtool /n). Ignored when -CertFile is supplied.

.PARAMETER CertFile
    Path to a PFX certificate to sign with instead of a store certificate.

.PARAMETER CertPassword
    Password for -CertFile (if any).

.PARAMETER TimestampUrl
    RFC-3161 timestamp server. Default: http://timestamp.sectigo.com

.PARAMETER NoPortable
    Omit the .igportable marker; the archive then stores settings in %LocalAppData%\ImageGlass.

.PARAMETER SkipPublish
    Reuse the existing __artifacts/publish/win-<arch> output instead of re-publishing
    (faster iteration; the archive may not reflect uncommitted source changes).

.EXAMPLE
    pwsh __assets/win/script-pack-win-zip.ps1 -Platform x64 -Sign
    # Signed portable x64 ZIP for GitHub Releases.

.EXAMPLE
    pwsh __assets/win/script-pack-win-zip.ps1 -Platform arm64 -NoPortable
    # arm64 ZIP that keeps settings in %LocalAppData% (no portable marker).
#>

[CmdletBinding()]
param(
    [ValidateSet('x64', 'arm64')]
    [string]$Platform = 'x64',

    [switch]$Sign,

    [string]$CertSubject = 'Duong Dieu Phap',
    [string]$CertFile = '',
    [string]$CertPassword = '',
    [string]$TimestampUrl = 'http://timestamp.sectigo.com',

    [switch]$NoPortable,

    [switch]$SkipPublish
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# --- Paths ---------------------------------------------------------------------
$WorkspaceDir = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$ProjectFile  = Join-Path $WorkspaceDir 'ImageGlass.Win32\ImageGlass.Win32.csproj'
$BuildProps   = Join-Path $WorkspaceDir 'Directory.Build.props'
$AppExtras    = Join-Path $WorkspaceDir '__assets\__app'
$DistDir      = Join-Path $WorkspaceDir '__artifacts\bundle'

# Marker file that turns on portable mode; must match Const.PORTABLE_MARKER_FILE.
$PortableMarker = '.igportable'

# --- Helpers -------------------------------------------------------------------

# Read a single <Tag>value</Tag> from Directory.Build.props.
function Get-BuildProp([string]$Tag) {
    $m = Select-String -Path $BuildProps -Pattern "<$Tag>(.*?)</$Tag>" | Select-Object -First 1
    if ($m) { return $m.Matches[0].Groups[1].Value.Trim() }
    return ''
}

# Locate signtool.exe, preferring the newest Windows SDK and an x64 host build.
function Find-SdkTool([string]$Name) {
    $roots = @(
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin",
        "${env:ProgramFiles}\Windows Kits\10\bin"
    ) | Where-Object { $_ -and (Test-Path $_) }

    foreach ($root in $roots) {
        $hit = Get-ChildItem -Path $root -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match '^10\.' } |
            Sort-Object { [version]$_.Name } -Descending |
            ForEach-Object { Join-Path $_.FullName "x64\$Name" } |
            Where-Object { Test-Path $_ } |
            Select-Object -First 1
        if ($hit) { return $hit }
    }

    $onPath = Get-Command $Name -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }

    throw "Could not find $Name. Install the Windows 10/11 SDK (includes signtool)."
}

# Whether a usable code-signing certificate is available; also records which store it lives in so
# signtool searches the same one. Returns $false rather than throwing: the caller then packs UNSIGNED.
$script:UseMachineStore = $false
function Test-SigningCert {
    if ($CertFile) {
        if (Test-Path $CertFile) { return $true }
        Write-Warning "Certificate file not found: $CertFile"
        return $false
    }
    foreach ($store in @(
            @{ Path = 'Cert:\CurrentUser\My';  Machine = $false },
            @{ Path = 'Cert:\LocalMachine\My'; Machine = $true })) {
        $cert = Get-ChildItem $store.Path -CodeSigningCert -ErrorAction SilentlyContinue |
            Where-Object { $_.Subject -like "*$CertSubject*" -and $_.HasPrivateKey } |
            Select-Object -First 1
        if ($cert) {
            $script:UseMachineStore = $store.Machine
            return $true
        }
    }
    return $false
}

# Sign one or more files with signtool (Authenticode, SHA-256, timestamped).
# Returns $true on success, $false on failure (never throws).
function Invoke-SignTool([string]$SignTool, [string[]]$Files) {
    if ($Files.Count -eq 0) { return $true }
    $common = @('sign', '/fd', 'SHA256', '/tr', $TimestampUrl, '/td', 'SHA256')
    if ($CertFile) {
        $common += @('/f', $CertFile)
        if ($CertPassword) { $common += @('/p', $CertPassword) }
    }
    else {
        $common += @('/n', $CertSubject, '/a')
        if ($script:UseMachineStore) { $common += '/sm' }
    }
    & $SignTool @common @Files
    return ($LASTEXITCODE -eq 0)
}

# --- Version -------------------------------------------------------------------
$igVersion = Get-BuildProp 'IgVersion'
if (-not $igVersion) { throw "Could not read <IgVersion> from $BuildProps" }
$igReleaseType = Get-BuildProp 'IgReleaseType'

# Mirrors the GitHub release tag/asset naming: <version>-<releasetype>, no "v" prefix.
$relLabel = if ($igReleaseType) { "$igVersion-$igReleaseType" } else { $igVersion }

# --- Plan ----------------------------------------------------------------------
$rid         = "win-$Platform"
$msbuildPlat = if ($Platform -eq 'x64') { 'x64' } else { 'ARM64' }
$publishDir  = Join-Path $WorkspaceDir "__artifacts\publish\$rid"
$stagingDir  = Join-Path $DistDir "$rid-zip"
$archiveName = "ImageGlass_${relLabel}_${rid}"
# The staged folder name becomes the single top-level folder inside the archive, named after the
# archive itself (matches the previous releases).
$payloadDir  = Join-Path $stagingDir $archiveName
$outZip      = Join-Path $DistDir "$archiveName.zip"

$script:doSign = [bool]$Sign
if ($script:doSign -and -not (Test-SigningCert)) {
    Write-Warning "No signing certificate found: building an UNSIGNED archive."
    $script:doSign = $false
}
$signtool = if ($script:doSign) { Find-SdkTool 'signtool.exe' } else { '' }

Write-Host "==> Packing ImageGlass $igVersion as ZIP ($Platform)"
Write-Host "    Portable    : $(if ($NoPortable) { 'no (no marker file)' } else { "yes ($PortableMarker)" })"
Write-Host "    Signed      : $(if ($script:doSign) { 'yes (payload binaries)' } else { 'no' })"
Write-Host "    Output      : $outZip"
if ($script:doSign) { Write-Host "    signtool    : $signtool" }

# --- 1. Publish a fresh self-contained AOT build -------------------------------
if ($SkipPublish -and (Test-Path (Join-Path $publishDir 'ImageGlass.exe'))) {
    Write-Host "    reusing publish output: $publishDir"
}
else {
    Write-Host "    publishing $rid (Release, AOT, self-contained)"
    if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
    & dotnet publish $ProjectFile -c Release -r $rid -p:Platform=$msbuildPlat -o $publishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $rid (exit $LASTEXITCODE)." }
    # Bundle the shared app assets (themes, credits, etc.), mirroring the publish-win tasks.
    Copy-Item -Path (Join-Path $AppExtras '*') -Destination $publishDir -Recurse -Force
}
if (-not (Test-Path (Join-Path $publishDir 'ImageGlass.exe'))) {
    throw "Publish did not produce ImageGlass.exe in $publishDir"
}

# --- 2. Stage the payload ------------------------------------------------------
if (Test-Path $stagingDir) { Remove-Item $stagingDir -Recurse -Force }
New-Item -ItemType Directory -Path $payloadDir -Force | Out-Null
Copy-Item -Path (Join-Path $publishDir '*') -Destination $payloadDir -Recurse -Force

# Drop debug symbols: they bloat the archive and are not part of the product.
Get-ChildItem -Path $payloadDir -Recurse -Include '*.pdb' -File -ErrorAction SilentlyContinue |
    Remove-Item -Force

# The exportable Store license belongs to the msstore MSIX only; a stale -SkipPublish reuse must
# never leak it into a public archive.
$storeLicenseDir = Join-Path $payloadDir '_store'
if (Test-Path $storeLicenseDir) {
    Write-Warning "Removing '$storeLicenseDir' from the archive: the store license ships in the msstore package only."
    Remove-Item $storeLicenseDir -Recurse -Force
}

# --- 3. Portable marker --------------------------------------------------------
if (-not $NoPortable) {
    # empty file: only its presence matters
    [System.IO.File]::WriteAllBytes((Join-Path $payloadDir $PortableMarker), @())
    Write-Host "    wrote portable marker: $PortableMarker"
}

# --- 4. Sign the payload binaries ----------------------------------------------
if ($script:doSign) {
    $binaries = Get-ChildItem -Path $payloadDir -Recurse -Include '*.exe', '*.dll' -File |
        Select-Object -ExpandProperty FullName
    Write-Host "    signing $($binaries.Count) payload binary file(s)"
    if (-not (Invoke-SignTool -SignTool $signtool -Files $binaries)) {
        Write-Warning "Could not sign payload binaries: the archive will be left UNSIGNED."
        $script:doSign = $false
    }
}

# --- 5. Pack -------------------------------------------------------------------
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null
if (Test-Path $outZip) { Remove-Item $outZip -Force }

# ZipFile (not Compress-Archive): it is much faster on this many files and, unlike a wildcard
# Compress-Archive, it never skips the dot-prefixed marker file.
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    $payloadDir, $outZip,
    [System.IO.Compression.CompressionLevel]::Optimal,
    $true)   # includeBaseDirectory: everything lands under the archive-named folder

$zipSizeMb = [math]::Round((Get-Item $outZip).Length / 1MB, 1)

# --- 6. Clean up staging -------------------------------------------------------
Remove-Item $stagingDir -Recurse -Force -ErrorAction SilentlyContinue

# --- Done ----------------------------------------------------------------------
Write-Host ''
Write-Host 'Done.'
Write-Host "  Archive : $outZip ($zipSizeMb MB)"
Write-Host "  Portable: $(if ($NoPortable) { 'no' } else { "yes (extract to a writable folder)" })"
if ($Sign -and -not $script:doSign) {
    Write-Host '  Signed  : no (no signing certificate was found)'
    Write-Host '  Next    : sign the binaries and repack before publishing the release.'
}
elseif ($script:doSign) {
    Write-Host '  Signed  : yes (payload binaries)'
    Write-Host '  Next    : upload to the GitHub release for this version.'
}
