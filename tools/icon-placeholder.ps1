Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap 512,512
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$rect = New-Object System.Drawing.Rectangle 0,0,512,512
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(10,20,30)), ([System.Drawing.Color]::FromArgb(8,56,101)), 45
$g.FillRectangle($brush, $rect)
$g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(40,56,189,255))), 86,86,340,340)
$micBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(230,230,230))
$g.FillRectangle($micBrush, 236,140,40,120)
$g.FillEllipse($micBrush, 226,120,60,40)
$g.FillRectangle($micBrush, 246,260,20,80)
$font = New-Object System.Drawing.Font('Segoe UI',48)
$noteBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255,56,189,255))
$g.DrawString('♪', $font, $noteBrush, 120, 160)
$g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(55,56,189,255))), 96,96,320,320)
$bmp.Save('icon-512.png',[System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose()
$bmp.Dispose()
Write-Host 'Generated icon-512.png'