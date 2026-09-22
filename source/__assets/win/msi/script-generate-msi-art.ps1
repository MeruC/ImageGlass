#Requires -Version 7.0
<#
.SYNOPSIS
    Generate the two WixUI images for the MSI installer from the app logo.

.DESCRIPTION
    The MSI wizard shows two images, referenced by UI.wxs as WixUIBannerBmp / WixUIDialogBmp.
    They are generated rather than checked in as opaque binaries, so they stay in sync with
    __assets/logo_c_512.png the same way the MSIX asset set does.

        banner.png   493 x 58  logical   top strip of every wizard page
        dialog.png   493 x 312 logical   left panel of the Complete page

    Both are rendered at -Scale times those logical sizes. The MSI Bitmap control stretches its
    image to the control unless msidbControlAttributesFixedSize is set, and WixUI does not set
    it, so an oversized source is downscaled into place and stays sharp on a high-DPI monitor.
    At 100% DPI the control is 493 px wide, so scale 3 covers up to 300%.

    PNG, not BMP: the Bitmap control accepts any WIC format on Windows 8 and later, and the app
    requires Windows 10. A 3x BMP would be ~4 MB, the same PNG is a fraction of that.

    Layouts are dictated by where WixUI paints its own text, which is drawn over these images:
      * the banner's left ~340 logical px carries the page title, so the logo goes hard right;
      * the Complete page puts its text and its "Launch ImageGlass" checkbox on the right, and
        that checkbox is not transparent, so everything right of the logo panel is painted in
        the dialog face colour. Anything else (white, say) shows up as a grey band behind it.

.PARAMETER Source
    Source logo (square, high-res). Default: __assets/logo_c_512.png.

.PARAMETER OutDir
    Output folder. Default: the assets folder next to this script.

.PARAMETER Scale
    Resolution multiplier over the logical size. Default 3 (covers up to 300% display scaling).

.EXAMPLE
    pwsh __assets/win/msi/script-generate-msi-art.ps1
    # Regenerate banner.png and dialog.png at 3x from __assets/logo_c_512.png.

.EXAMPLE
    pwsh __assets/win/msi/script-generate-msi-art.ps1 -Source __assets/logo_p_512.png -Scale 4
    # Render from the Pro logo at 4x.
#>

[CmdletBinding()]
param(
    [string]$Source = '',
    [string]$OutDir = '',

    [ValidateRange(1, 6)]
    [int]$Scale = 3
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# --- Paths ---------------------------------------------------------------------
$WorkspaceDir = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
if (-not $Source) { $Source = Join-Path $WorkspaceDir '__assets\logo_c_512.png' }
if (-not $OutDir) { $OutDir = Join-Path $PSScriptRoot 'assets' }

if (-not (Test-Path $Source)) { throw "Source image not found: $Source" }

Add-Type -AssemblyName System.Drawing

# --- Logical layout (multiplied by $Scale at render time) ----------------------
$BannerW = 493
$BannerH = 58
$DialogW = 493
$DialogH = 312

# Logo panel on the Complete page; must stay clear of the text controls at ~180 logical px.
$PanelW = 175

# COLOR_BTNFACE, the colour MSI paints its dialogs and its non-transparent controls with.
$DialogFace = [System.Drawing.Color]::FromArgb(240, 240, 240)
$White      = [System.Drawing.Color]::FromArgb(255, 255, 255)
$PanelLight = [System.Drawing.Color]::FromArgb(242, 247, 251)
$PanelDeep  = [System.Drawing.Color]::FromArgb(219, 233, 245)
$Divider    = [System.Drawing.Color]::FromArgb(205, 218, 229)

# --- Helpers -------------------------------------------------------------------

# A canvas configured for high-quality downscaling of the logo.
function New-Canvas([int]$Width, [int]$Height) {
    $bmp = [System.Drawing.Bitmap]::new($Width, $Height,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode  = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode    = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.SmoothingMode      = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    return @($bmp, $g)
}

function Save-Png([System.Drawing.Bitmap]$Bitmap, [string]$Path) {
    $Bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $kb = [math]::Round((Get-Item $Path).Length / 1KB)
    Write-Host ("    {0,-12}: {1} x {2}  ({3} KB)" -f
        [System.IO.Path]::GetFileName($Path), $Bitmap.Width, $Bitmap.Height, $kb)
}

# --- Render --------------------------------------------------------------------
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

Write-Host "==> Generating MSI installer artwork"
Write-Host "    Source      : $Source"
Write-Host "    Scale       : ${Scale}x (logical 493x58 and 493x312)"
Write-Host "    Output      : $OutDir"

$src = [System.Drawing.Image]::FromFile((Resolve-Path $Source).Path)
try {
    # --- banner.png: white strip, logo hard right, title text painted over the left ---
    $canvas = New-Canvas ($BannerW * $Scale) ($BannerH * $Scale)
    $bmp = $canvas[0]
    $g   = $canvas[1]
    try {
        $g.Clear($White)

        $logo = 44 * $Scale
        $x    = ($BannerW * $Scale) - $logo - (10 * $Scale)
        $y    = [int](((($BannerH * $Scale)) - $logo) / 2)
        $g.DrawImage($src, $x, $y, $logo, $logo)

        Save-Png $bmp (Join-Path $OutDir 'banner.png')
    }
    finally {
        $g.Dispose()
        $bmp.Dispose()
    }

    # --- dialog.png: tinted logo panel, rest in the dialog face colour ---
    $canvas = New-Canvas ($DialogW * $Scale) ($DialogH * $Scale)
    $bmp = $canvas[0]
    $g   = $canvas[1]
    try {
        $g.Clear($DialogFace)

        $panel = [System.Drawing.Rectangle]::new(0, 0, $PanelW * $Scale, $DialogH * $Scale)
        $brush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
            $panel, $PanelLight, $PanelDeep,
            [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
        try { $g.FillRectangle($brush, $panel) } finally { $brush.Dispose() }

        $pen = [System.Drawing.Pen]::new($Divider, [float]$Scale)
        try { $g.DrawLine($pen, ($PanelW * $Scale), 0, ($PanelW * $Scale), ($DialogH * $Scale)) }
        finally { $pen.Dispose() }

        $logo = 108 * $Scale
        $x    = [int]((($PanelW * $Scale) - $logo) / 2)
        $y    = [int]((($DialogH * $Scale) - $logo) / 2) - (20 * $Scale)
        $g.DrawImage($src, $x, $y, $logo, $logo)

        Save-Png $bmp (Join-Path $OutDir 'dialog.png')
    }
    finally {
        $g.Dispose()
        $bmp.Dispose()
    }
}
finally {
    $src.Dispose()
}

Write-Host ''
Write-Host 'Done.'
Write-Host "  Artwork : $OutDir"
Write-Host '  Next    : rebuild the installer (pack-win-x64-msi) to pick it up.'
