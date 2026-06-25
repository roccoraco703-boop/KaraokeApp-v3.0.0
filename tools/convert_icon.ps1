param(
    [string]$png = "icon-512.png",
    [string]$out = "karaokeapp.ico"
)

# Requires ImageMagick (magick.exe) installed and in PATH
if (-Not (Get-Command magick -ErrorAction SilentlyContinue)) {
    Write-Error "ImageMagick 'magick' not found in PATH. Install ImageMagick and try again."
    exit 1
}

Write-Host "Generating multi-resolution ICO from $png -> $out"
magick convert "$png" -resize 256x256 "$png-256.png"
magick convert "$png" -resize 128x128 "$png-128.png"
magick convert "$png" -resize 64x64 "$png-64.png"
magick convert "$png" -resize 48x48 "$png-48.png"
magick convert "$png" -resize 32x32 "$png-32.png"
magick convert "$png" -resize 16x16 "$png-16.png"

magick convert "$png-16.png","$png-32.png","$png-48.png","$png-64.png","$png-128.png","$png-256.png" -background none -alpha remove -alpha off -strip -colors 256 "$out"

Write-Host "Generated $out"

# Cleanup
Remove-Item "$png-16.png","$png-32.png","$png-48.png","$png-64.png","$png-128.png","$png-256.png" -ErrorAction SilentlyContinue
