# VERSION 2.0 - Script de création d'icône RunDeck
# Crée une icône PNG et ICO avec "RD" sur fond gradient violet
# Palette alignée sur le thème C# Design Pro de l'application

Add-Type -AssemblyName System.Drawing

# Crée une image 256x256
[int]$size = 256
$bitmap = New-Object System.Drawing.Bitmap($size, $size)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Active l'antialiasing
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

# Fond sombre (BackgroundDark de l'app)
$bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(11, 14, 20))
$graphics.FillRectangle($bgBrush, 0, 0, $size, $size)

# Rectangle arrondi avec gradient violet (#6C5CE7 → #A855F7)
[int]$margin = 20
[int]$rectSize = $size - ($margin * 2)
[int]$radius = 40

$rect = New-Object System.Drawing.Rectangle($margin, $margin, $rectSize, $rectSize)
$gradientBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Point($margin, $margin)),
    (New-Object System.Drawing.Point(($size - $margin), ($size - $margin))),
    [System.Drawing.Color]::FromArgb(108, 92, 231),   # #6C5CE7
    [System.Drawing.Color]::FromArgb(168, 85, 247)    # #A855F7
)

# Dessiner le rectangle arrondi via GraphicsPath
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
[int]$right = $rect.X + $rect.Width - $radius
[int]$bottom = $rect.Y + $rect.Height - $radius

$path.AddArc($rect.X, $rect.Y, $radius, $radius, 180, 90)
$path.AddArc($right, $rect.Y, $radius, $radius, 270, 90)
$path.AddArc($right, $bottom, $radius, $radius, 0, 90)
$path.AddArc($rect.X, $bottom, $radius, $radius, 90, 90)
$path.CloseFigure()
$graphics.FillPath($gradientBrush, $path)

# Texte "RD" en blanc, centré
$font = New-Object System.Drawing.Font("Segoe UI", 100, [System.Drawing.FontStyle]::Bold)
$textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$format = New-Object System.Drawing.StringFormat
$format.Alignment = [System.Drawing.StringAlignment]::Center
$format.LineAlignment = [System.Drawing.StringAlignment]::Center

$graphics.DrawString("RD", $font, $textBrush,
    (New-Object System.Drawing.RectangleF(0, 0, $size, $size)), $format)

# Sauvegarde PNG
$iconsDir = Join-Path $PSScriptRoot "Icons"
if (-not (Test-Path $iconsDir)) {
    New-Item -ItemType Directory -Path $iconsDir -Force | Out-Null
}

$pngPath = Join-Path $iconsDir "app-icon.png"
$bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "PNG cree : $pngPath" -ForegroundColor Green

# Création du ICO (multi-résolutions : 16, 32, 48, 256)
$icoPath = Join-Path $iconsDir "app.ico"

[int[]]$sizes = @(16, 32, 48, 256)
$memoryStream = New-Object System.IO.MemoryStream

# En-tête ICO (6 octets)
$iconDir = [byte[]]@(0, 0, 1, 0) + [BitConverter]::GetBytes([int16]$sizes.Count)
$memoryStream.Write($iconDir, 0, $iconDir.Length)

[int]$imageDataOffset = 6 + (16 * $sizes.Count)
$imageDataList = @()

foreach ($s in $sizes) {
    $resizedBitmap = New-Object System.Drawing.Bitmap($s, $s)
    $resizedGraphics = [System.Drawing.Graphics]::FromImage($resizedBitmap)
    $resizedGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $resizedGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $resizedGraphics.DrawImage($bitmap, 0, 0, $s, $s)

    $ms = New-Object System.IO.MemoryStream
    $resizedBitmap.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $imageData = $ms.ToArray()
    $imageDataList += ,@($s, $imageData)

    # Largeur/Hauteur 256 → encodée comme 0 dans le format ICO
    [byte]$w = if ($s -eq 256) { 0 } else { $s }
    [byte]$h = if ($s -eq 256) { 0 } else { $s }

    $lenBytes = [BitConverter]::GetBytes([int32]$imageData.Length)
    $offBytes = [BitConverter]::GetBytes([int32]$imageDataOffset)

    $entry = [byte[]]@($w, $h, 0, 0, 1, 0, 32, 0) + $lenBytes + $offBytes

    $memoryStream.Write($entry, 0, $entry.Length)
    $imageDataOffset += $imageData.Length

    $resizedGraphics.Dispose()
    $resizedBitmap.Dispose()
    $ms.Dispose()
}

# Écrit les données d'image
foreach ($item in $imageDataList) {
    $memoryStream.Write($item[1], 0, $item[1].Length)
}

# Sauvegarde le fichier ICO
[System.IO.File]::WriteAllBytes($icoPath, $memoryStream.ToArray())
Write-Host "ICO cree : $icoPath" -ForegroundColor Green

# Nettoyage
$memoryStream.Dispose()
$path.Dispose()
$graphics.Dispose()
$bitmap.Dispose()
$bgBrush.Dispose()
$gradientBrush.Dispose()
$textBrush.Dispose()
$font.Dispose()

Write-Host ""
Write-Host "Icones RunDeck creees avec succes !" -ForegroundColor Cyan
Write-Host "   - PNG : $pngPath"
Write-Host "   - ICO : $icoPath"
