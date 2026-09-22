#Requires -Version 7.0
<#
.SYNOPSIS
    Publish a fresh AOT build of ImageGlass.Win32 and deploy it straight to a local folder,
    for iterating on a dev machine without going through the MSI/MSIX/ZIP release packagers.

.DESCRIPTION
    Publishes the same self-contained AOT build the release packagers use, then copies it over
    an existing local install (e.g. "E:\#Tools\ImageGlass"). Before overwriting, the current
    contents of that folder are zipped into "<DeployDir>\_backups\ImageGlass_backup_<timestamp>.zip",
    and only the newest -KeepBackups zips are kept.

    No ".igportable" marker is written, so the deployed copy keeps using
    %LocalAppData%\ImageGlass_10\igconfig.json for settings, matching a normal (non-portable)
    local install.

.PARAMETER Platform
    Target architecture: x64 (default) or arm64.

.PARAMETER DeployDir
    Folder to deploy into. Default: E:\#Tools\ImageGlass

.PARAMETER KeepBackups
    How many of the most recent backup zips to keep in "<DeployDir>\_backups". Default: 3.

.PARAMETER SkipPublish
    Reuse the existing __artifacts/publish/win-<arch> output instead of re-publishing
    (faster iteration; the deploy may not reflect uncommitted source changes).

.EXAMPLE
    pwsh __assets/win/script-deploy-local.ps1
    # Publish and deploy to E:\#Tools\ImageGlass, keeping the last 3 backups.
#>

[CmdletBinding()]
param(
    [ValidateSet('x64', 'arm64')]
    [string]$Platform = 'x64',

    [string]$DeployDir = 'E:\#Tools\ImageGlass',

    [int]$KeepBackups = 3,

    [switch]$SkipPublish
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# --- Paths ---------------------------------------------------------------------
$WorkspaceDir = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$ProjectFile  = Join-Path $WorkspaceDir 'ImageGlass.Win32\ImageGlass.Win32.csproj'
$AppExtras    = Join-Path $WorkspaceDir '__assets\__app'
$rid          = "win-$Platform"
$msbuildPlat  = if ($Platform -eq 'x64') { 'x64' } else { 'ARM64' }
$publishDir   = Join-Path $WorkspaceDir "__artifacts\publish\$rid"
$BackupDir    = Join-Path $DeployDir '_backups'

# The NativeAOT linker shells out to vswhere.exe to locate MSVC; a plain (non-Developer) shell
# often doesn't have it on PATH, which fails link.exe with an unhelpful exit code 123.
$vswhereDir = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer'
if ((Test-Path $vswhereDir) -and ($env:PATH -notlike "*$vswhereDir*")) {
    $env:PATH = "$env:PATH;$vswhereDir"
}

Write-Host "==> Deploying ImageGlass ($Platform) to $DeployDir"

# --- 1. Publish a fresh self-contained AOT build -------------------------------
if ($SkipPublish -and (Test-Path (Join-Path $publishDir 'ImageGlass.exe'))) {
    Write-Host "    reusing publish output: $publishDir"
}
else {
    Write-Host "    publishing $rid (Release, AOT, self-contained)"
    if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
    & dotnet publish $ProjectFile -c Release -r $rid -p:Platform=$msbuildPlat -o $publishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $rid (exit $LASTEXITCODE)." }
    Copy-Item -Path (Join-Path $AppExtras '*') -Destination $publishDir -Recurse -Force
}
if (-not (Test-Path (Join-Path $publishDir 'ImageGlass.exe'))) {
    throw "Publish did not produce ImageGlass.exe in $publishDir"
}

# Debug symbols are huge and not useful for a local deploy.
Get-ChildItem -Path $publishDir -Recurse -Include '*.pdb' -File -ErrorAction SilentlyContinue |
    Remove-Item -Force

# --- 2. Back up whatever is currently deployed ---------------------------------
if (Test-Path $DeployDir) {
    $existingFiles = Get-ChildItem -Path $DeployDir -Force -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -ne '_backups' }

    if ($existingFiles) {
        New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null

        $stamp     = Get-Date -Format 'yyyyMMdd-HHmmss'
        $backupZip = Join-Path $BackupDir "ImageGlass_backup_$stamp.zip"
        Write-Host "    backing up current deploy: $backupZip"

        # Stage outside DeployDir so the in-progress zip can never be swept up by itself,
        # and so Compress-Archive never sees the (excluded) _backups folder.
        $stageDir = Join-Path ([System.IO.Path]::GetTempPath()) "ig-deploy-backup-$PID"
        if (Test-Path $stageDir) { Remove-Item $stageDir -Recurse -Force }
        New-Item -ItemType Directory -Path $stageDir -Force | Out-Null
        try {
            $existingFiles | Copy-Item -Destination $stageDir -Recurse -Force
            Compress-Archive -Path (Join-Path $stageDir '*') -DestinationPath $backupZip -Force
        }
        finally {
            Remove-Item $stageDir -Recurse -Force -ErrorAction SilentlyContinue
        }

        # Prune to the newest $KeepBackups.
        Get-ChildItem -Path $BackupDir -Filter 'ImageGlass_backup_*.zip' |
            Sort-Object LastWriteTime -Descending |
            Select-Object -Skip $KeepBackups |
            ForEach-Object {
                Write-Host "    pruning old backup: $($_.Name)"
                Remove-Item $_.FullName -Force
            }
    }

    # Clear the deploy dir (but keep _backups) before copying the new build in.
    $existingFiles | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
}
else {
    New-Item -ItemType Directory -Path $DeployDir -Force | Out-Null
}

# --- 3. Deploy -------------------------------------------------------------------
Write-Host "    copying build to $DeployDir"
Copy-Item -Path (Join-Path $publishDir '*') -Destination $DeployDir -Recurse -Force

$backupCount = if (Test-Path $BackupDir) { @(Get-ChildItem $BackupDir -Filter '*.zip').Count } else { 0 }

Write-Host ''
Write-Host 'Done.'
Write-Host "  Deployed : $DeployDir"
Write-Host "  Backups  : $BackupDir ($backupCount kept, max $KeepBackups)"
