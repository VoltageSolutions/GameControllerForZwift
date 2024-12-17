# Fetch-SDL2.ps1

param (
    [string]$Version = "2.30.10",
    [string]$BuildConfig = "Debug",
    [string]$TargetFramework = "net9.0",
    [string]$ProjectDir = $PSScriptRoot
)

# Enable verbose output for debugging
$VerbosePreference = "Continue"

Write-Host "Starting script with the following parameters:"
Write-Host "Version: $Version"
Write-Host "BuildConfig: $BuildConfig"
Write-Host "TargetFramework: $TargetFramework"
Write-Host "ProjectDir: $ProjectDir"

# Remove any extra quotes that may have been passed around the project directory
$ProjectDir = $ProjectDir.Trim('"')
Write-Host "ProjectDir after trimming quotes: $ProjectDir"

# Construct the target directory dynamically
$TargetDir = Join-Path $ProjectDir "bin\$BuildConfig\$TargetFramework"
Write-Host "Target directory before trim: '$TargetDir'"

# Ensure no trailing or leading quotes in the path
$TargetDir = $TargetDir.Trim('"')
Write-Host "Target directory after trim: '$TargetDir'"

# Check if the target directory exists and is valid
if (-not (Test-Path $TargetDir)) {
    Write-Host "Target directory does not exist, creating it..."
    New-Item -ItemType Directory -Path $TargetDir | Out-Null
}

# Define the download URL
$url = "https://github.com/libsdl-org/SDL/releases/download/release-$Version/SDL2-$Version-win32-x64.zip"
Write-Host "Download URL: $url"

# Define temporary download path
$tempZip = Join-Path $ProjectDir "SDL2-$Version-win32-x64.zip"
Write-Host "Temporary ZIP file: $tempZip"

# Download the ZIP file
Write-Host "Downloading SDL2 version $Version..."
try {
    Invoke-WebRequest -Uri $url -OutFile $tempZip -ErrorAction Stop
    Write-Host "Download successful."
} catch {
    Write-Host "Error downloading SDL2: $_"
    exit 1
}

# Create a temporary extraction folder
$tempExtractDir = Join-Path $ProjectDir "SDL2_temp"
Write-Host "Temporary extraction folder: $tempExtractDir"

if (Test-Path $tempExtractDir) {
    Write-Host "Removing existing temporary extraction folder."
    Remove-Item -Recurse -Force $tempExtractDir
}

Write-Host "Creating temporary extraction folder."
New-Item -ItemType Directory -Path $tempExtractDir | Out-Null

# Unzip the file
Write-Host "Unpacking SDL2.dll..."
try {
    Expand-Archive -Path $tempZip -DestinationPath $tempExtractDir -Force -ErrorAction Stop
    Write-Host "Unpacking successful."
} catch {
    Write-Host "Error unpacking ZIP file: $_"
    exit 1
}

# Copy SDL2.dll to the target directory
$sourceDll = Join-Path $tempExtractDir "SDL2.dll"
Write-Host "Source SDL2.dll: $sourceDll"

if (Test-Path $sourceDll) {
    Write-Host "Found SDL2.dll, copying to $TargetDir..."
    try {
        Copy-Item -Path $sourceDll -Destination $TargetDir -Force -ErrorAction Stop
        Write-Host "SDL2.dll copied successfully."
    } catch {
        Write-Host "Error copying SDL2.dll: $_"
        exit 1
    }
} else {
    Write-Host "SDL2.dll not found in the extracted files!"
    exit 1
}

# Clean up temporary files
Write-Host "Cleaning up temporary files."
Remove-Item $tempZip -Force
Remove-Item -Recurse -Force $tempExtractDir

# Delete the 'libs' folder (adjust the path if necessary)
$libsDir = Join-Path $ProjectDir "libs"
if (Test-Path $libsDir) {
    Write-Host "Removing 'libs' folder."
    Remove-Item -Recurse -Force $libsDir
}

Write-Host "SDL2.dll successfully updated and 'libs' folder deleted!"
exit 0
