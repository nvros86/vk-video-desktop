Add-Type -AssemblyName System.Drawing

$Sizes = @(16, 32, 48, 64, 128, 256)
$RootDir = Split-Path (Split-Path $PSScriptRoot) -Parent
$SourcePath = Join-Path $RootDir "src\VKVideoDesktop.App\Assets\Square150x150Logo.scale-200.png"
$OutputPath = Join-Path $RootDir "src\VKVideoDesktop.App\Assets\icon.ico"

if (-not (Test-Path $SourcePath)) {
    Write-Error "Исходный файл не найден: $SourcePath"
    exit 1
}

Write-Output "Источник: $SourcePath"
Write-Output "Цель: $OutputPath"

$sourceImage = [System.Drawing.Image]::FromFile($SourcePath)
Write-Output "Размер исходника: $($sourceImage.Width)x$($sourceImage.Height)"

$pngDataList = [System.Collections.ArrayList]@()

foreach ($size in $Sizes) {
    $bitmap = New-Object System.Drawing.Bitmap($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.DrawImage($sourceImage, 0, 0, $size, $size)

    $imgMs = New-Object System.IO.MemoryStream
    $bitmap.Save($imgMs, [System.Drawing.Imaging.ImageFormat]::Png)
    $data = $imgMs.ToArray()
    [void]$pngDataList.Add($data)

    $graphics.Dispose()
    $bitmap.Dispose()
    $imgMs.Dispose()

    Write-Output "  ${size}x${size}: $($data.Length) bytes"
}

$sourceImage.Dispose()

$count = $Sizes.Length
$headerSize = 6 + ($count * 16)

$totalSize = $headerSize
foreach ($d in $pngDataList) { $totalSize += $d.Length }

$output = New-Object byte[]($totalSize)

# ICONDIR
$output[0] = 0
$output[1] = 0
$output[2] = 1
$output[3] = 0
$output[4] = [byte]($count -band 0xFF)
$output[5] = [byte](($count -shr 8) -band 0xFF)

# ICONDIRENTRY for each image
$curOffset = $headerSize
for ($i = 0; $i -lt $count; $i++) {
    $entryBase = 6 + ($i * 16)
    $w = if ($Sizes[$i] -eq 256) { 0 } else { $Sizes[$i] }
    $h = if ($Sizes[$i] -eq 256) { 0 } else { $Sizes[$i] }
    $dataLen = $pngDataList[$i].Length

    $output[$entryBase + 0] = [byte]$w
    $output[$entryBase + 1] = [byte]$h
    $output[$entryBase + 2] = 0
    $output[$entryBase + 3] = 0
    $output[$entryBase + 4] = 1
    $output[$entryBase + 5] = 0
    $output[$entryBase + 6] = 32
    $output[$entryBase + 7] = 0

    $output[$entryBase + 8] = [byte]($dataLen -band 0xFF)
    $output[$entryBase + 9] = [byte](($dataLen -shr 8) -band 0xFF)
    $output[$entryBase + 10] = [byte](($dataLen -shr 16) -band 0xFF)
    $output[$entryBase + 11] = [byte](($dataLen -shr 24) -band 0xFF)

    $output[$entryBase + 12] = [byte]($curOffset -band 0xFF)
    $output[$entryBase + 13] = [byte](($curOffset -shr 8) -band 0xFF)
    $output[$entryBase + 14] = [byte](($curOffset -shr 16) -band 0xFF)
    $output[$entryBase + 15] = [byte](($curOffset -shr 24) -band 0xFF)

    $curOffset += $dataLen
}

# PNG data
$pos = $headerSize
for ($i = 0; $i -lt $count; $i++) {
    [System.Array]::Copy($pngDataList[$i], 0, $output, $pos, $pngDataList[$i].Length)
    $pos += $pngDataList[$i].Length
}

[System.IO.File]::WriteAllBytes($OutputPath, $output)

$finalSize = (Get-Item $OutputPath).Length
Write-Output "ICO создан: $OutputPath ($finalSize байт)"
