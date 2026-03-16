# Builds and deploys the Mix Server iOS app to a physical iPhone device.

param(
    [string]$ApiUrl = "http://localhost:5225",
    [string]$DeviceId = "",
    [string]$DevelopmentTeam = "82KPTGL5R2",
    [switch]$RestartApi,
    [switch]$StartApi,
    [switch]$SkipBuild,
    [switch]$Launch
)

$ErrorActionPreference = "Stop"

$repoRoot = Join-Path $PSScriptRoot ".."
$clientPath = Join-Path $repoRoot "src/clients/mix-server-client"
$iosPath = Join-Path $clientPath "ios"
$iosAppPath = Join-Path $iosPath "App"
$apiProjectPath = Join-Path $repoRoot "src/api/MixServer/MixServer.csproj"
$logDirectoryPath = Join-Path $repoRoot "data/logs"
$buildPath = Join-Path $repoRoot "data/ios-build"

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

function Get-PhysicalDeviceId {
    if ($DeviceId) {
        return $DeviceId
    }

    $devices = xcrun xctrace list devices 2>&1
    $physicalDevices = @()

    $inDevicesSection = $false
    foreach ($line in $devices) {
        if ($line -match "^== Devices ==$") {
            $inDevicesSection = $true
            continue
        }
        if ($line -match "^== Simulators ==$") {
            break
        }
        if ($inDevicesSection -and $line -match "iPhone.*\(([0-9A-Fa-f-]+)\)") {
            $physicalDevices += @{
                Line = $line.Trim()
                Id   = $matches[1]
            }
        }
    }

    if ($physicalDevices.Count -eq 0) {
        throw "No physical iPhone found. Connect your device via USB or Wi-Fi and ensure it is trusted."
    }

    if ($physicalDevices.Count -gt 1) {
        Write-Host "Multiple physical iPhones detected:" -ForegroundColor Cyan
        for ($i = 0; $i -lt $physicalDevices.Count; $i++) {
            Write-Host "  [$i] $($physicalDevices[$i].Line)" -ForegroundColor White
        }

        $selection = Read-Host "Select device number (0-$($physicalDevices.Count - 1))"
        return $physicalDevices[[int]$selection].Id
    }

    $device = $physicalDevices[0]
    Write-Host "  [✓] Found device: $($device.Line)" -ForegroundColor Green
    return $device.Id
}

function Start-ApiProcess {
    if (-not $StartApi) {
        return
    }

    $isListening = $false
    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $tcpClient.Connect("localhost", 5225)
        $tcpClient.Close()
        $isListening = $true
    }
    catch {}

    if ($RestartApi -and $isListening) {
        $pid5225 = lsof -ti tcp:5225 2>/dev/null
        if ($pid5225) {
            kill -9 $pid5225 2>/dev/null
            Start-Sleep -Seconds 2
            $isListening = $false
        }
    }

    if ($isListening) {
        Write-Host "  [✓] API already running on $ApiUrl" -ForegroundColor Green
        return
    }

    New-Item -ItemType Directory -Path $logDirectoryPath -Force | Out-Null

    $stdoutPath = Join-Path $logDirectoryPath "ios-device-api.stdout.log"
    $stderrPath = Join-Path $logDirectoryPath "ios-device-api.stderr.log"

    Start-Process `
        -FilePath "dotnet" `
        -ArgumentList @("run", "--project", $apiProjectPath) `
        -WorkingDirectory $repoRoot `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath

    $apiReady = $false
    for ($i = 0; $i -lt 60; $i++) {
        Start-Sleep -Seconds 2
        try {
            $tcpClient = New-Object System.Net.Sockets.TcpClient
            $tcpClient.Connect("localhost", 5225)
            $tcpClient.Close()
            $apiReady = $true
            break
        }
        catch {}
    }

    if (-not $apiReady) {
        throw "The API did not start within 120 seconds. Check $stdoutPath and $stderrPath."
    }

    Write-Host "  [✓] API started on $ApiUrl" -ForegroundColor Green
}

function Update-InfoPlist {
    $infoPlistPath = Join-Path $iosAppPath "App/Info.plist"

    # Add background audio mode if not already present
    $bgModes = /usr/libexec/PlistBuddy -c "Print :UIBackgroundModes" $infoPlistPath 2>&1
    if ($LASTEXITCODE -ne 0) {
        /usr/libexec/PlistBuddy -c "Add :UIBackgroundModes array" $infoPlistPath | Out-Null
        /usr/libexec/PlistBuddy -c "Add :UIBackgroundModes:0 string audio" $infoPlistPath | Out-Null
    }
    elseif ($bgModes -notmatch "audio") {
        $count = ($bgModes | Select-String "^\s+\w" | Measure-Object).Count
        /usr/libexec/PlistBuddy -c "Add :UIBackgroundModes:$count string audio" $infoPlistPath | Out-Null
    }

    # Allow HTTP to localhost for local API access (ATS exception)
    $ats = /usr/libexec/PlistBuddy -c "Print :NSAppTransportSecurity" $infoPlistPath 2>&1
    if ($LASTEXITCODE -ne 0) {
        /usr/libexec/PlistBuddy -c "Add :NSAppTransportSecurity dict" $infoPlistPath | Out-Null
        /usr/libexec/PlistBuddy -c "Add :NSAppTransportSecurity:NSAllowsLocalNetworking bool true" $infoPlistPath | Out-Null
    }

    Write-Host "  [✓] Info.plist configured for background audio and local networking" -ForegroundColor Green
}

# ──────────────────────────────────────────────
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Mix Server iOS Device Deploy" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Step "Step 1: Detecting physical iPhone..."
$deviceUdid = Get-PhysicalDeviceId

Write-Step "Step 2: Starting the API..."
Start-ApiProcess

if (-not $SkipBuild) {
    Write-Step "Step 3: Building and syncing the Capacitor iOS project..."

    Push-Location $clientPath
    try {
        if (-not (Test-Path (Join-Path $clientPath "node_modules/@capacitor/cli"))) {
            npm install
            Assert-LastExitCode -Action "Installing client npm dependencies"
        }

        if (-not (Test-Path (Join-Path $clientPath "node_modules/@capacitor/ios"))) {
            npm install "@capacitor/ios"
            Assert-LastExitCode -Action "Installing @capacitor/ios"
        }

        if (-not (Test-Path $iosPath)) {
            npx cap add ios
            Assert-LastExitCode -Action "Adding the Capacitor iOS platform"
        }

        npm run cap:build
        Assert-LastExitCode -Action "Building and syncing the Capacitor iOS project"
    }
    finally {
        Pop-Location
    }

    Write-Step "Step 4: Applying iOS configuration..."
    Update-InfoPlist

    Write-Step "Step 5: Resolving Swift Package Manager dependencies..."

    New-Item -ItemType Directory -Path $buildPath -Force | Out-Null
    Push-Location $iosAppPath
    try {
        xcodebuild `
            -project "App.xcodeproj" `
            -resolvePackageDependencies `
            -clonedSourcePackagesDirPath "$buildPath/SourcePackages"
        Assert-LastExitCode -Action "Resolving Swift Package Manager dependencies"
    }
    finally {
        Pop-Location
    }

    Write-Step "Step 6: Building the iOS app for device..."

    Push-Location $iosAppPath
    try {
        xcodebuild `
            -project "App.xcodeproj" `
            -scheme "App" `
            -destination "generic/platform=iOS" `
            -configuration Debug `
            -derivedDataPath $buildPath `
            -clonedSourcePackagesDirPath "$buildPath/SourcePackages" `
            -allowProvisioningUpdates `
            DEVELOPMENT_TEAM=$DevelopmentTeam `
            CODE_SIGN_IDENTITY="Apple Development" `
            CODE_SIGN_STYLE=Automatic `
            CODE_SIGNING_REQUIRED=YES `
            CODE_SIGNING_ALLOWED=YES `
            build
        Assert-LastExitCode -Action "Building the iOS app for device"
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Host "  [⏭] Skipping build (--SkipBuild)" -ForegroundColor Cyan
}

Write-Step "Step 7: Installing on physical device..."

$appPath = Join-Path $buildPath "Build/Products/Debug-iphoneos/App.app"
if (-not (Test-Path $appPath)) {
    throw "Built app not found at: $appPath. Run without -SkipBuild first."
}

Write-Host "  Ensure your iPhone is unlocked before continuing." -ForegroundColor Magenta

$installOutput = xcrun devicectl device install app --device $deviceUdid $appPath 2>&1
if ($LASTEXITCODE -ne 0) {
    if ($installOutput -match "DeviceLocked") {
        Write-Host ""
        Write-Host "  [!] Device is locked. Please unlock your iPhone and press Enter to retry." -ForegroundColor Red
        Read-Host "  Press Enter when your iPhone is unlocked"

        xcrun devicectl device install app --device $deviceUdid $appPath 2>&1
        Assert-LastExitCode -Action "Installing the app on device (retry)"
    }
    else {
        Write-Host $installOutput
        throw "Installing the app on device failed with exit code $LASTEXITCODE."
    }
}

Write-Host "  [✓] App installed on device" -ForegroundColor Green

if ($Launch) {
    Write-Step "Step 8: Launching Mix Server on device..."
    xcrun devicectl device process launch --device $deviceUdid com.mixserver.app 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  [!] Could not auto-launch. Please tap the MixServer icon on your device." -ForegroundColor Yellow
    }
    else {
        Write-Host "  [✓] App launched on device" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Mix Server deployed to physical iPhone" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Device: $deviceUdid" -ForegroundColor Cyan
Write-Host ""
Write-Host "Quick re-deploy (skip web build):" -ForegroundColor White
Write-Host "  pwsh scripts/run-ios-device.ps1 -SkipBuild -Launch" -ForegroundColor White
Write-Host ""
Write-Host "Full re-deploy:" -ForegroundColor White
Write-Host "  pwsh scripts/run-ios-device.ps1 -Launch" -ForegroundColor White
Write-Host ""
Write-Host "With API server:" -ForegroundColor White
Write-Host "  pwsh scripts/run-ios-device.ps1 -StartApi -Launch" -ForegroundColor White
Write-Host ""
Write-Host "NOTE: On first launch, you may need to trust the developer certificate" -ForegroundColor Yellow
Write-Host "on your iPhone: Settings > General > VPN & Device Management" -ForegroundColor Yellow
