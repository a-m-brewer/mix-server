# iOS development environment setup for Mix Server

param(
    [string]$SimulatorName = "iPhone 17"
)

$ErrorActionPreference = "Stop"

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

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Mix Server iOS Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Step "Step 1: Checking Xcode command-line tools..."

$xcodeSelectPath = xcode-select -p 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Xcode command-line tools are not installed. Run: xcode-select --install"
}

Write-Host "  [✓] Xcode tools at: $xcodeSelectPath" -ForegroundColor Green

Write-Step "Step 2: Checking Homebrew..."

if (-not (Get-Command brew -ErrorAction SilentlyContinue)) {
    throw "Homebrew is not installed. Install it from https://brew.sh and rerun this script."
}

Write-Host "  [✓] Homebrew is available" -ForegroundColor Green

Write-Step "Step 3: Installing ios-webkit-debug-proxy..."

if (-not (Get-Command ios_webkit_debug_proxy -ErrorAction SilentlyContinue)) {
    brew install ios-webkit-debug-proxy
    Assert-LastExitCode -Action "ios-webkit-debug-proxy installation"
}

Write-Host "  [✓] ios_webkit_debug_proxy is installed" -ForegroundColor Green

Write-Step "Step 4: Checking the '$SimulatorName' simulator..."

$simList = xcrun simctl list devices available 2>&1 | Select-String ([regex]::Escape($SimulatorName))
if (-not $simList) {
    throw "The '$SimulatorName' simulator is not available. Install it via Xcode > Settings > Platforms > iOS."
}

Write-Host "  [✓] Simulator '$SimulatorName' is available" -ForegroundColor Green

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "iOS setup complete" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Next step: pwsh scripts/run-ios.ps1 -StartApi" -ForegroundColor Cyan
