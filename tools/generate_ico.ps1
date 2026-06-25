param(
    [string]$png = "icon-512.png",
    [string]$out = "karaokeapp.ico"
)

if (-not (Test-Path $png)) {
    Write-Error "Input PNG not found: $png"
    exit 1
}

Add-Type -AssemblyName System.Drawing

$sizes = @(16,32,48,64,128,256)
$dataList = @()

$src = [System.Drawing.Image]::FromFile($png)

foreach ($s in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($src, 0, 0, $s, $s)
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bytes = $ms.ToArray()
    $dataList += ,@{ Size = $s; Bytes = $bytes }
    $g.Dispose()
    $bmp.Dispose()
    $ms.Dispose()
}
$src.Dispose()

# Build ICO
$outStream = New-Object System.IO.FileStream($out, [System.IO.FileMode]::Create)
$bw = New-Object System.IO.BinaryWriter($outStream)

# ICONDIR
$bw.Write([uint16]0) # reserved
$bw.Write([uint16]1) # type = 1 for icon
$bw.Write([uint16]($dataList.Count))

$offset = 6 + ($dataList.Count * 16)

foreach ($entry in $dataList) {
    $size = $entry.Size
    $bytes = $entry.Bytes
    $widthByte = if ($size -eq 256) { 0 } else { [byte]$size }
    $heightByte = if ($size -eq 256) { 0 } else { [byte]$size }
    $bw.Write([byte]$widthByte)
    $bw.Write([byte]$heightByte)
    $bw.Write([byte]0) # color count
    $bw.Write([byte]0) # reserved
    $bw.Write([uint16]1) # planes
    $bw.Write([uint16]32) # bitcount
    $bw.Write([uint32]($bytes.Length))
    $bw.Write([uint32]$offset)
    $offset += $bytes.Length
}

# Write image data
foreach ($entry in $dataList) {
    $bw.Write($entry.Bytes)
}

$bw.Flush()
$bw.Close()
$outStream.Close()

$sizesStr = ($dataList | ForEach-Object { $_.Size }) -join ', ' ; Write-Host "Generated $out with sizes: $sizesStr"