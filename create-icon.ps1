# VERSION 1.40 - Script de création d'icône
# Crée une icône PNG et ICO pour l'application

Add-Type -AssemblyName System.Drawing

# Crée une image 256x256
$size = 256
$bitmap = New-Object System.Drawing.Bitmap($size, $size)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Active l'antialiasing
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

# Fond dégradé bleu
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Point(0, 0)),
    (New-Object System.Drawing.Point($size, $size)),
    [System.Drawing.Color]::FromArgb(0, 122, 204),   # #007ACC
    [System.Drawing.Color]::FromArgb(0, 90, 158)     # #005A9E
)
$graphics.FillRectangle($brush, 0, 0, $size, $size)

# Texte "PS" (PowerShell)
$font = New-Object System.Drawing.Font("Segoe UI", 120, [System.Drawing.FontStyle]::Bold)
$textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$format = New-Object System.Drawing.StringFormat
$format.Alignment = [System.Drawing.StringAlignment]::Center
$format.LineAlignment = [System.Drawing.StringAlignment]::Center

$graphics.DrawString("PS", $font, $textBrush, 
    (New-Object System.Drawing.RectangleF(0, 0, $size, $size)), $format)

# Sauvegarde PNG
$iconsDir = Join-Path $PSScriptRoot "Resources"
if (-not (Test-Path $iconsDir)) {
    New-Item -ItemType Directory -Path $iconsDir -Force | Out-Null
}

$pngPath = Join-Path $iconsDir "app-icon.png"
$bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "✅ PNG créé : $pngPath" -ForegroundColor Green

# Création du ICO (multi-résolutions : 16, 32, 48, 256)
$icoPath = Join-Path $iconsDir "app-icon.ico"

# Crée les différentes tailles
$sizes = @(16, 32, 48, 256)
$memoryStream = New-Object System.IO.MemoryStream

# En-tête ICO (6 octets)
$iconDir = [byte[]]@(0, 0, 1, 0) + [BitConverter]::GetBytes([int16]$sizes.Count)
$memoryStream.Write($iconDir, 0, $iconDir.Length)

$imageDataOffset = 6 + (16 * $sizes.Count)
$imageDataList = @()

foreach ($s in $sizes) {
    $resizedBitmap = New-Object System.Drawing.Bitmap($s, $s)
    $resizedGraphics = [System.Drawing.Graphics]::FromImage($resizedBitmap)
    $resizedGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQualityBicubic
    $resizedGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $resizedGraphics.DrawImage($bitmap, 0, 0, $s, $s)
    
    # Sauvegarde en PNG dans un MemoryStream
    $ms = New-Object System.IO.MemoryStream
    $resizedBitmap.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $imageData = $ms.ToArray()
    $imageDataList += ,@($s, $imageData)
    
    # Entry ICO (16 octets)
    $entry = [byte[]]@(
        $s,          # Largeur
        $s,          # Hauteur
        0,           # Nombre de couleurs (0 = plus de 256)
        0,           # Réservé
        1, 0,        # Color planes
        32, 0,       # Bits par pixel
        [BitConverter]::GetBytes($imageData.Length)[0],
        [BitConverter]::GetBytes($imageData.Length)[1],
        [BitConverter]::GetBytes($imageData.Length)[2],
        [BitConverter]::GetBytes($imageData.Length)[3],
        [BitConverter]::GetBytes($imageDataOffset)[0],
        [BitConverter]::GetBytes($imageDataOffset)[1],
        [BitConverter]::GetBytes($imageDataOffset)[2],
        [BitConverter]::GetBytes($imageDataOffset)[3]
    )
    
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
Write-Host "✅ ICO créé : $icoPath" -ForegroundColor Green

# Nettoyage
$memoryStream.Dispose()
$graphics.Dispose()
$bitmap.Dispose()
$brush.Dispose()
$textBrush.Dispose()
$font.Dispose()

Write-Host ""
Write-Host "🎨 Icônes créées avec succès !" -ForegroundColor Cyan
Write-Host "   - PNG pour la fenêtre : $pngPath"
Write-Host "   - ICO pour l'exe : $icoPath"
