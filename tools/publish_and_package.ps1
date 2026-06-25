param(
    [string]$solution = "KaraokeApp.slnx",
    [string]$configuration = "Release",
    [string]$rid = "win-x64",
    [string]$output = "release_package",
    [string]$version = "3.0.0"
)

$root = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $root

Write-Host "Cleaning solution..."
dotnet clean $solution -c $configuration

Write-Host "Publishing self-contained single-file..."
dotnet publish $solution -c $configuration -r $rid --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -p:Version=$version -o "$root\$output\KaraokeApp"

Write-Host "Creating release structure..."
$dest = Join-Path $root $output
New-Item -ItemType Directory -Path $dest -Force | Out-Null

$structure = @("Data","Logs","Cache","Songs","Presets","VLC","Docs")
foreach ($d in $structure) {
    New-Item -ItemType Directory -Path (Join-Path $dest $d) -Force | Out-Null
}

Write-Host "Copying published files..."
Copy-Item -Path "$root\$output\KaraokeApp\*" -Destination $dest -Recurse -Force

Write-Host "Adding docs and placeholders..."
# Create placeholder docs if not present
if (-Not (Test-Path "$root\Docs\README.pdf")) { New-Item -ItemType File -Path (Join-Path $dest "Docs\README.pdf") -Force | Out-Null }
if (-Not (Test-Path "$root\License.txt")) { New-Item -ItemType File -Path (Join-Path $dest "License.txt") -Force | Out-Null }
if (-Not (Test-Path "$root\Changelog.txt")) { New-Item -ItemType File -Path (Join-Path $dest "Changelog.txt") -Force | Out-Null }

Write-Host "Release package ready at: $dest"
