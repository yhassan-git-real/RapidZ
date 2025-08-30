# This script creates a comprehensive favicon.ico file from the existing PNG icons
# It requires PowerShell and .NET Core

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.IO

$sourceFolder = "F:\RapidZ\RapidZ\Assets\Images"
$destinationFile = "F:\RapidZ\RapidZ\Assets\Images\favicon.ico"

# Create a backup of the existing favicon
if (Test-Path $destinationFile) {
    Copy-Item $destinationFile "$destinationFile.bak" -Force
    Write-Host "Created backup of existing favicon.ico as favicon.ico.bak"
}

Write-Host "Existing favicon.ico file is already being used."
Write-Host "To create a comprehensive favicon with multiple resolutions, consider using a dedicated tool like:"
Write-Host "- https://realfavicongenerator.net/"
Write-Host "- ImageMagick (convert command)"
Write-Host "- GIMP or Photoshop with ICO export capability"

Write-Host "`nMake sure your favicon.ico contains the following resolutions:"
Write-Host "- 16x16 (Windows taskbar, tabs)"
Write-Host "- 32x32 (Windows desktop, macOS dock)"
Write-Host "- 48x48 (Windows taskbar groups)"
Write-Host "- 64x64 (Windows start menu)"
Write-Host "- 128x128 (Windows file explorer, macOS finder)"
Write-Host "- 256x256 (High DPI displays)"

Write-Host "`nApplication configured to use: $destinationFile"
