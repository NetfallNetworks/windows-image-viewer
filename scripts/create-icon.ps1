#!/usr/bin/env pwsh
# Creates a multi-resolution Windows .ico file from a PNG image
# This creates icons at standard Windows sizes: 16x16, 32x32, 48x48, 256x256

param(
    [Parameter(Mandatory=$false)]
    [string]$InputPng = "icon-source.png",

    [Parameter(Mandatory=$false)]
    [string]$OutputIco = "src\WallpaperApp.TrayApp\Resources\app.ico"
)

# Change to repository root (script is in scripts/ subdirectory)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
Set-Location $repoRoot

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Creating Windows Icon from PNG" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Working directory: $repoRoot" -ForegroundColor Gray
Write-Host ""

# Resolve full paths
$InputPng = Join-Path $repoRoot $InputPng
$OutputIco = Join-Path $repoRoot $OutputIco

# Check if input file exists
if (-not (Test-Path $InputPng)) {
    Write-Host "ERROR: Input file not found: $InputPng" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please save your icon as 'icon-source.png' in the root directory," -ForegroundColor Yellow
    Write-Host "or specify a different path with -InputPng" -ForegroundColor Yellow
    exit 1
}

# Load the image
Add-Type -AssemblyName System.Drawing

try {
    Write-Host "Loading source image: $InputPng" -ForegroundColor Green
    $originalImage = [System.Drawing.Image]::FromFile($InputPng)

    Write-Host "Original size: $($originalImage.Width)x$($originalImage.Height)" -ForegroundColor Gray
    Write-Host ""

    # Create icons at multiple resolutions
    $sizes = @(16, 32, 48, 256)
    $bitmaps = @()

    foreach ($size in $sizes) {
        Write-Host "Creating ${size}x${size} icon..." -ForegroundColor Green
        $bitmap = New-Object System.Drawing.Bitmap($size, $size)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.DrawImage($originalImage, 0, 0, $size, $size)
        $graphics.Dispose()
        $bitmaps += $bitmap
    }

    # Create the output directory if it doesn't exist
    $outputDir = Split-Path $OutputIco -Parent
    if (-not (Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    # Save as .ico file
    Write-Host ""
    Write-Host "Saving multi-resolution icon: $OutputIco" -ForegroundColor Green

    # Create a FileStream for the .ico file
    $fs = [System.IO.FileStream]::new($OutputIco, [System.IO.FileMode]::Create)
    $writer = [System.IO.BinaryWriter]::new($fs)

    # ICO file header
    $writer.Write([uint16]0)      # Reserved, must be 0
    $writer.Write([uint16]1)      # Type: 1 = .ico
    $writer.Write([uint16]$bitmaps.Count)  # Number of images

    # Calculate offsets
    $offset = 6 + (16 * $bitmaps.Count)  # Header + directory entries

    # Write directory entries
    $imageData = @()
    foreach ($bitmap in $bitmaps) {
        $ms = New-Object System.IO.MemoryStream
        $bitmap.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $data = $ms.ToArray()
        $ms.Dispose()
        $imageData += $data

        # Width and Height: 0 means 256 pixels (byte can only hold 0-255)
        $width = if ($bitmap.Width -eq 256) { 0 } else { $bitmap.Width }
        $height = if ($bitmap.Height -eq 256) { 0 } else { $bitmap.Height }

        $writer.Write([byte]$width)             # Width (0 = 256)
        $writer.Write([byte]$height)            # Height (0 = 256)
        $writer.Write([byte]0)                  # Color palette (0 = no palette)
        $writer.Write([byte]0)                  # Reserved
        $writer.Write([uint16]1)                # Color planes
        $writer.Write([uint16]32)               # Bits per pixel
        $writer.Write([uint32]$data.Length)     # Image data size
        $writer.Write([uint32]$offset)          # Image data offset

        $offset += $data.Length
    }

    # Write image data
    foreach ($data in $imageData) {
        $writer.Write($data)
    }

    $writer.Close()
    $fs.Close()

    # Cleanup
    foreach ($bitmap in $bitmaps) {
        $bitmap.Dispose()
    }
    $originalImage.Dispose()

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "SUCCESS! Icon created at:" -ForegroundColor Green
    Write-Host "  $OutputIco" -ForegroundColor White
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. The icon is already configured in the project" -ForegroundColor Gray
    Write-Host "  2. Run .\scripts\build.bat to rebuild with the new icon" -ForegroundColor Gray
    Write-Host "  3. Run .\scripts\install-tray-app.ps1 to install" -ForegroundColor Gray

} catch {
    Write-Host "ERROR: Failed to create icon" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
