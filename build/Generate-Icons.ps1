[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$repoRoot = Split-Path -Parent $PSScriptRoot
$assetDirectory = Join-Path $repoRoot 'assets'
New-Item -ItemType Directory -Force -Path $assetDirectory | Out-Null

function New-RoundedRectangle([System.Drawing.RectangleF]$Rectangle, [float]$Radius) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = $Radius * 2
    $path.AddArc($Rectangle.X, $Rectangle.Y, $diameter, $diameter, 180, 90)
    $path.AddArc($Rectangle.Right - $diameter, $Rectangle.Y, $diameter, $diameter, 270, 90)
    $path.AddArc($Rectangle.Right - $diameter, $Rectangle.Bottom - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($Rectangle.X, $Rectangle.Bottom - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-ZipNestBitmap([int]$Size, [bool]$SetupBadge) {
    $bitmap = New-Object System.Drawing.Bitmap $Size, $Size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $scale = $Size / 256.0

    $shadowBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(45, 13, 24, 42))
    $shadowPath = New-RoundedRectangle (New-Object System.Drawing.RectangleF (12*$scale), (15*$scale), (232*$scale), (228*$scale)) (48*$scale)
    $graphics.FillPath($shadowBrush, $shadowPath)

    $backgroundBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush `
        (New-Object System.Drawing.PointF (25*$scale), (20*$scale)), `
        (New-Object System.Drawing.PointF (230*$scale), (238*$scale)), `
        ([System.Drawing.Color]::FromArgb(31, 56, 96)), `
        ([System.Drawing.Color]::FromArgb(12, 30, 55))
    $backgroundPath = New-RoundedRectangle (New-Object System.Drawing.RectangleF (12*$scale), (10*$scale), (232*$scale), (228*$scale)) (48*$scale)
    $graphics.FillPath($backgroundBrush, $backgroundPath)

    $backBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(91, 198, 178))
    $tabPoints = @(
        (New-Object System.Drawing.PointF (38*$scale), (83*$scale)),
        (New-Object System.Drawing.PointF (38*$scale), (65*$scale)),
        (New-Object System.Drawing.PointF (96*$scale), (65*$scale)),
        (New-Object System.Drawing.PointF (116*$scale), (83*$scale)),
        (New-Object System.Drawing.PointF (218*$scale), (83*$scale)),
        (New-Object System.Drawing.PointF (218*$scale), (185*$scale)),
        (New-Object System.Drawing.PointF (38*$scale), (185*$scale))
    )
    $graphics.FillPolygon($backBrush, $tabPoints)

    $frontBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush `
        (New-Object System.Drawing.PointF (42*$scale), (92*$scale)), `
        (New-Object System.Drawing.PointF (214*$scale), (205*$scale)), `
        ([System.Drawing.Color]::FromArgb(91, 210, 189)), `
        ([System.Drawing.Color]::FromArgb(42, 153, 163))
    $frontPath = New-RoundedRectangle (New-Object System.Drawing.RectangleF (32*$scale), (86*$scale), (192*$scale), (121*$scale)) (19*$scale)
    $graphics.FillPath($frontBrush, $frontPath)

    $zipperBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(236, 242, 246))
    $graphics.FillRectangle($zipperBrush, (122*$scale), (86*$scale), (12*$scale), (88*$scale))
    for ($y = 93; $y -lt 168; $y += 14) {
        $graphics.FillRectangle($zipperBrush, (112*$scale), ($y*$scale), (12*$scale), (7*$scale))
        $graphics.FillRectangle($zipperBrush, (134*$scale), (($y+7)*$scale), (12*$scale), (7*$scale))
    }
    $pullPath = New-RoundedRectangle (New-Object System.Drawing.RectangleF (113*$scale), (166*$scale), (30*$scale), (29*$scale)) (7*$scale)
    $graphics.FillPath($zipperBrush, $pullPath)
    $holeBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(42, 153, 163))
    $graphics.FillEllipse($holeBrush, (122*$scale), (174*$scale), (12*$scale), (12*$scale))

    if ($SetupBadge) {
        $badgeBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(242, 151, 55))
        $graphics.FillEllipse($badgeBrush, (151*$scale), (151*$scale), (91*$scale), (91*$scale))
        $borderPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), (7*$scale)
        $graphics.DrawEllipse($borderPen, (154*$scale), (154*$scale), (84*$scale), (84*$scale))
        $arrowPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), (12*$scale)
        $arrowPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $arrowPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $graphics.DrawLine($arrowPen, (196*$scale), (174*$scale), (196*$scale), (213*$scale))
        $graphics.DrawLine($arrowPen, (179*$scale), (198*$scale), (196*$scale), (215*$scale))
        $graphics.DrawLine($arrowPen, (213*$scale), (198*$scale), (196*$scale), (215*$scale))
        $borderPen.Dispose()
        $arrowPen.Dispose()
        $badgeBrush.Dispose()
    }

    $shadowBrush.Dispose(); $backgroundBrush.Dispose(); $backBrush.Dispose(); $frontBrush.Dispose()
    $zipperBrush.Dispose(); $holeBrush.Dispose(); $shadowPath.Dispose(); $backgroundPath.Dispose()
    $frontPath.Dispose(); $pullPath.Dispose(); $graphics.Dispose()
    return $bitmap
}

function Write-Ico([string]$Path, [bool]$SetupBadge) {
    $sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
    $images = @()
    foreach ($size in $sizes) {
        $bitmap = New-ZipNestBitmap $size $SetupBadge
        $stream = New-Object System.IO.MemoryStream
        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        $images += ,$stream.ToArray()
        $stream.Dispose(); $bitmap.Dispose()
    }
    $file = [System.IO.File]::Create($Path)
    $writer = New-Object System.IO.BinaryWriter $file
    $writer.Write([UInt16]0); $writer.Write([UInt16]1); $writer.Write([UInt16]$images.Count)
    $offset = 6 + (16 * $images.Count)
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $dimension = if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] }
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
        $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([UInt16]1); $writer.Write([UInt16]32)
        $writer.Write([UInt32]$images[$i].Length); $writer.Write([UInt32]$offset)
        $offset += $images[$i].Length
    }
    foreach ($image in $images) { $writer.Write($image) }
    $writer.Dispose(); $file.Dispose()
}

Write-Ico (Join-Path $assetDirectory 'ZipNest.ico') $false
Write-Ico (Join-Path $assetDirectory 'ZipNestSetup.ico') $true

$preview = New-ZipNestBitmap 512 $false
$preview.Save((Join-Path $assetDirectory 'ZipNest.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()
$setupPreview = New-ZipNestBitmap 512 $true
$setupPreview.Save((Join-Path $assetDirectory 'ZipNestSetup.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$setupPreview.Dispose()

Write-Host 'Generated ZipNest application and setup icons.'
