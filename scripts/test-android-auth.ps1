# Runs the Android authentication end-to-end test against the emulator-hosted app.

param(
    [string]$AvdName = "MixServer_API_36",
    [string]$ApiUrl = "http://10.0.2.2:5225",
    [switch]$RestartApi,
    [switch]$StartApi,
    [int]$DevToolsPort = 9222,
    [string]$Password = "AndroidE2E!123",
    [string]$Username = "android-e2e"
)

$ErrorActionPreference = "Stop"

$repoRoot = Join-Path $PSScriptRoot ".."
$clientPath = Join-Path $repoRoot "src\clients\mix-server-client"
$e2ePath = Join-Path $clientPath "e2e\mobile"

function Get-AndroidSdkRoot {
    if ($env:ANDROID_SDK_ROOT) {
        return $env:ANDROID_SDK_ROOT
    }

    if ($env:ANDROID_HOME) {
        return $env:ANDROID_HOME
    }

    return Join-Path $env:LOCALAPPDATA "Android\Sdk"
}

function Assert-LastExitCode {
    param([string]$Action)

    if ($LASTEXITCODE -ne 0) {
        throw "$Action failed with exit code $LASTEXITCODE."
    }
}

function Wait-ForWebViewDevToolsSocket {
    param([string]$AdbPath)

    for ($i = 0; $i -lt 30; $i++) {
        $socket = (& $AdbPath shell cat /proc/net/unix | Select-String 'webview_devtools_remote_\d+' | Select-Object -First 1).Matches.Value
        if (-not [string]::IsNullOrWhiteSpace($socket)) {
            return $socket
        }

        Start-Sleep -Seconds 2
    }

    throw "Could not find the WebView DevTools socket for the Android app."
}

function Ensure-E2eDependencies {
    if (Test-Path (Join-Path $e2ePath "node_modules")) {
        return
    }

    Push-Location $e2ePath
    try {
        npm install
        Assert-LastExitCode -Action "Installing mobile E2E dependencies"
    }
    finally {
        Pop-Location
    }
}

$androidSdkRoot = Get-AndroidSdkRoot
$adbPath = Join-Path $androidSdkRoot "platform-tools\adb.exe"

if (-not (Test-Path $adbPath)) {
    throw "Android SDK tools are missing. Run pwsh scripts/setup-android.ps1 first."
}

Ensure-E2eDependencies

& (Join-Path $repoRoot "scripts\run-android.ps1") `
    -AvdName $AvdName `
    -ApiUrl $ApiUrl `
    -RestartApi:$RestartApi `
    -StartApi:$StartApi

$socket = Wait-ForWebViewDevToolsSocket -AdbPath $adbPath

try {
    & $adbPath forward --remove "tcp:$DevToolsPort" 2>$null | Out-Null
    & $adbPath forward "tcp:$DevToolsPort" "localabstract:$socket" | Out-Null
    Assert-LastExitCode -Action "Forwarding the WebView DevTools port"

    Push-Location $e2ePath
    try {
        $env:MIXSERVER_E2E_DEVTOOLS_URL = "http://127.0.0.1:$DevToolsPort"
        $env:MIXSERVER_E2E_PASSWORD = $Password
        $env:MIXSERVER_E2E_SERVER_URL = $ApiUrl
        $env:MIXSERVER_E2E_USERNAME = $Username

        npm run test:auth
        Assert-LastExitCode -Action "Running the Android authentication E2E test"
    }
    finally {
        Remove-Item Env:MIXSERVER_E2E_DEVTOOLS_URL -ErrorAction SilentlyContinue
        Remove-Item Env:MIXSERVER_E2E_PASSWORD -ErrorAction SilentlyContinue
        Remove-Item Env:MIXSERVER_E2E_SERVER_URL -ErrorAction SilentlyContinue
        Remove-Item Env:MIXSERVER_E2E_USERNAME -ErrorAction SilentlyContinue
        Pop-Location
    }
}
finally {
    & $adbPath forward --remove "tcp:$DevToolsPort" 2>$null | Out-Null
}
