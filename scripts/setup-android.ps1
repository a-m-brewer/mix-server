# Android development environment setup for Mix Server

param(
    [string]$AvdName = "MixServer_API_36",
    [string]$AndroidApiLevel = "36",
    [string]$BuildToolsVersion = "36.0.0",
    [string]$SystemImageFlavor = "google_apis",
    [string]$SystemImageArchitecture = "x86_64"
)

$ErrorActionPreference = "Stop"

$androidStudioPath = "C:\Program Files\Android\Android Studio"
$androidSdkRoot = Join-Path $env:LOCALAPPDATA "Android\Sdk"
$commandLineToolsUrl = "https://dl.google.com/android/repository/commandlinetools-win-14742923_latest.zip"
$commandLineToolsRoot = Join-Path $androidSdkRoot "cmdline-tools"
$commandLineToolsLatestPath = Join-Path $commandLineToolsRoot "latest"
$systemImagePackage = "system-images;android-$AndroidApiLevel;$SystemImageFlavor;$SystemImageArchitecture"

function Write-Step {
    param([string]$Message)

    Write-Host ""
    Write-Host $Message -ForegroundColor Yellow
}

function Assert-LastExitCode {
    param([string]$Action)

    if ($LASTEXITCODE -ne 0) {
        throw "$Action failed with exit code $LASTEXITCODE."
    }
}

function Add-UserPathEntry {
    param([string]$PathEntry)

    $userPath = [Environment]::GetEnvironmentVariable("Path", "User") ?? ""
    $pathEntries = $userPath.Split(';', [System.StringSplitOptions]::RemoveEmptyEntries) |
        ForEach-Object { $_.Trim() }

    if ($pathEntries -contains $PathEntry) {
        return
    }

    $newUserPath = if ([string]::IsNullOrWhiteSpace($userPath)) {
        $PathEntry
    }
    else {
        "$userPath;$PathEntry"
    }

    [Environment]::SetEnvironmentVariable("Path", $newUserPath, "User")
    $env:Path = "$env:Path;$PathEntry"
}

function Install-CommandLineTools {
    param([string]$DestinationPath)

    $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "mix-server-android-tools"
    $zipPath = Join-Path $tempRoot "commandlinetools.zip"
    $extractPath = Join-Path $tempRoot "extract"

    if (Test-Path $tempRoot) {
        Remove-Item $tempRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $extractPath -Force | Out-Null

    Write-Host "  Downloading Android command-line tools..." -ForegroundColor Cyan
    Invoke-WebRequest -Uri $commandLineToolsUrl -OutFile $zipPath

    Write-Host "  Extracting Android command-line tools..." -ForegroundColor Cyan
    Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force

    if (Test-Path $DestinationPath) {
        Remove-Item $DestinationPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $DestinationPath -Force | Out-Null
    Copy-Item (Join-Path $extractPath "cmdline-tools\*") $DestinationPath -Recurse -Force
}

function Get-SdkManagerPath {
    return Join-Path $commandLineToolsLatestPath "bin\sdkmanager.bat"
}

function Get-AvdManagerPath {
    return Join-Path $commandLineToolsLatestPath "bin\avdmanager.bat"
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Mix Server Android Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Step "Step 1: Checking Android Studio..."

if (-not (Test-Path (Join-Path $androidStudioPath "jbr\bin\java.exe"))) {
    if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
        throw "Android Studio is not installed and winget is unavailable. Install Android Studio, then rerun this script."
    }

    Write-Host "  Installing Android Studio via winget..." -ForegroundColor Cyan
    winget install --id Google.AndroidStudio --exact --source winget --accept-package-agreements --accept-source-agreements --disable-interactivity
    Assert-LastExitCode -Action "Android Studio installation"
}

$javaHome = Join-Path $androidStudioPath "jbr"
$env:JAVA_HOME = $javaHome
[Environment]::SetEnvironmentVariable("JAVA_HOME", $javaHome, "User")

Write-Host "  [✓] Android Studio runtime available at $javaHome" -ForegroundColor Green

Write-Step "Step 2: Installing Android SDK command-line tools..."

New-Item -ItemType Directory -Path $androidSdkRoot -Force | Out-Null
New-Item -ItemType Directory -Path $commandLineToolsRoot -Force | Out-Null

if (-not (Test-Path (Get-SdkManagerPath))) {
    Install-CommandLineTools -DestinationPath $commandLineToolsLatestPath
}

$env:ANDROID_HOME = $androidSdkRoot
$env:ANDROID_SDK_ROOT = $androidSdkRoot
[Environment]::SetEnvironmentVariable("ANDROID_HOME", $androidSdkRoot, "User")
[Environment]::SetEnvironmentVariable("ANDROID_SDK_ROOT", $androidSdkRoot, "User")

Add-UserPathEntry -PathEntry (Join-Path $androidSdkRoot "platform-tools")
Add-UserPathEntry -PathEntry (Join-Path $androidSdkRoot "emulator")
Add-UserPathEntry -PathEntry (Join-Path $commandLineToolsLatestPath "bin")

$sdkManagerPath = Get-SdkManagerPath
$avdManagerPath = Get-AvdManagerPath

Write-Host "  [✓] Android SDK root: $androidSdkRoot" -ForegroundColor Green

Write-Step "Step 3: Accepting licenses and installing SDK packages..."

$licenseInput = ("y`n" * 20)
$licenseInput | & $sdkManagerPath --sdk_root="$androidSdkRoot" --licenses | Out-Null

& $sdkManagerPath --sdk_root="$androidSdkRoot" `
    "platform-tools" `
    "emulator" `
    "build-tools;$BuildToolsVersion" `
    "platforms;android-$AndroidApiLevel" `
    $systemImagePackage
Assert-LastExitCode -Action "Android SDK package installation"

Write-Host "  [✓] Installed platform-tools, emulator, API $AndroidApiLevel, build-tools $BuildToolsVersion, and $systemImagePackage" -ForegroundColor Green

Write-Step "Step 4: Creating the Android Virtual Device..."

$avdList = & $avdManagerPath list avd | Out-String

if ($avdList -notmatch "Name:\s+$([regex]::Escape($AvdName))") {
    "no" | & $avdManagerPath create avd --force -n $AvdName -k $systemImagePackage
    Assert-LastExitCode -Action "AVD creation"
    Write-Host "  [✓] Created AVD $AvdName" -ForegroundColor Green
}
else {
    Write-Host "  [✓] AVD $AvdName already exists" -ForegroundColor Green
}

Write-Step "Step 5: Checking emulator acceleration..."

$emulatorPath = Join-Path $androidSdkRoot "emulator\emulator.exe"
if (Test-Path $emulatorPath) {
    & $emulatorPath -accel-check
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Android setup complete" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "AVD name: $AvdName" -ForegroundColor Cyan
Write-Host "Next step: pwsh scripts/run-android.ps1 -StartApi" -ForegroundColor Cyan
