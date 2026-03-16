# Runs the iOS authentication end-to-end test against the simulator-hosted app.
#
# The test is implemented as an XCUITest (LoginAuthUITests) and is driven by
# xcodebuild — no additional proxy or browser-automation tooling is required.

param(
    [string]$SimulatorName = "iPhone 17",
    [string]$ApiUrl = "http://localhost:5225",
    [switch]$RestartApi,
    [switch]$StartApi,
    [string]$Password = "AndroidE2E!123",
    [string]$Username = "android-e2e"
)

$ErrorActionPreference = "Stop"

$repoRoot    = Join-Path $PSScriptRoot ".."
$clientPath  = Join-Path $repoRoot "src/clients/mix-server-client"
$iosAppPath  = Join-Path $clientPath "ios/App"
$buildPath   = Join-Path $repoRoot "data/ios-build"
$logDir      = Join-Path $repoRoot "data/logs"

function Assert-LastExitCode {
    param([string]$Action)

    if ($LASTEXITCODE -ne 0) {
        throw "$Action failed with exit code $LASTEXITCODE."
    }
}

# ── Build, sync, and install the app ─────────────────────────────────────────

& (Join-Path $repoRoot "scripts/run-ios.ps1") `
    -SimulatorName $SimulatorName `
    -ApiUrl $ApiUrl `
    -RestartApi:$RestartApi `
    -StartApi:$StartApi

# ── Run the XCUITest ──────────────────────────────────────────────────────────

Write-Host ""
Write-Host "========================================"  -ForegroundColor Cyan
Write-Host "Running iOS authentication XCUITest"      -ForegroundColor Cyan
Write-Host "========================================"  -ForegroundColor Cyan

New-Item -ItemType Directory -Path $logDir -Force | Out-Null
$resultBundlePath = Join-Path $logDir "ios-auth-xcresult"

# xcodebuild refuses to overwrite an existing result bundle.
Remove-Item $resultBundlePath          -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "$resultBundlePath.xcresult" -Recurse -Force -ErrorAction SilentlyContinue

# ── Ensure the simulator uses an English keyboard ────────────────────────────
#
# If the simulator's active input method is a non-Latin IME (e.g. Chinese
# Pinyin), XCUITest's typeText sends characters through the IME and produces
# unexpected input.  Setting AppleKeyboards to the English QWERTY layout before
# the test run prevents this.  The XCUITest also passes -AppleLanguages /
# -AppleLocale launch arguments as a belt-and-braces measure.

xcrun simctl spawn booted defaults write -g AppleKeyboards -array "en_US@hw=US;sw=QWERTY" 2>$null
Write-Host "  [✓] Simulator keyboard set to English" -ForegroundColor Green

# ── Seed the server URL in localStorage so the login form submits directly ───
#
# The Angular login component reloads the page when the submitted server URL
# differs from the one stored in localStorage (to reinitialise the API token).
# Pre-seeding the correct URL prevents that reload on subsequent test runs.
# On a genuinely fresh install the database won't exist yet; in that case the
# XCUITest handles the reload transparently via its two-attempt login loop.

$appBundleId = "com.mixserver.app"
$appContainer = xcrun simctl get_app_container booted $appBundleId data 2>$null
if ($LASTEXITCODE -eq 0 -and $appContainer) {
    $webKitDefaultDir = Join-Path $appContainer "Library/WebKit/com.mixserver.app/WebsiteData/Default"
    $lsDb = Get-ChildItem -Path $webKitDefaultDir -Filter "localstorage.sqlite3" -Recurse `
                -ErrorAction SilentlyContinue |
            Select-Object -First 1 -ExpandProperty FullName
    if ($lsDb) {
        sqlite3 $lsDb "INSERT OR REPLACE INTO ItemTable (key, value) VALUES ('mixserver_server_url', '$ApiUrl');"
        Write-Host "  [✓] Seeded server URL in localStorage" -ForegroundColor Green
    } else {
        Write-Host "  [!] localStorage not found — first run will seed it via the login form" -ForegroundColor Yellow
    }
}

Push-Location $iosAppPath
try {
    # Environment variables are forwarded to the XCUITest via the TEST_RUNNER_
    # prefix, which xcodebuild passes through to ProcessInfo.environment in the
    # test host process.
    xcodebuild test `
        -project "App.xcodeproj" `
        -scheme "LoginAuthUITests" `
        -destination "platform=iOS Simulator,name=$SimulatorName" `
        -configuration Debug `
        -derivedDataPath $buildPath `
        -clonedSourcePackagesDirPath "$buildPath/SourcePackages" `
        -resultBundlePath $resultBundlePath `
        CODE_SIGN_IDENTITY="" `
        CODE_SIGNING_REQUIRED=NO `
        CODE_SIGNING_ALLOWED=NO `
        TEST_RUNNER_MIXSERVER_E2E_SERVER_URL="$ApiUrl" `
        TEST_RUNNER_MIXSERVER_E2E_USERNAME="$Username" `
        TEST_RUNNER_MIXSERVER_E2E_PASSWORD="$Password"

    Assert-LastExitCode -Action "Running the iOS authentication XCUITest"
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "========================================"  -ForegroundColor Cyan
Write-Host "iOS authentication test passed"           -ForegroundColor Green
Write-Host "========================================"  -ForegroundColor Cyan
Write-Host "Result bundle: $resultBundlePath.xcresult" -ForegroundColor Cyan
