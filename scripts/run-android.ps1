# Builds and runs the Mix Server Android app on an Android emulator.

param(
    [string]$AvdName = "MixServer_API_36",
    [string]$ApiUrl = "http://10.0.2.2:5225",
    [switch]$RestartApi,
    [switch]$StartApi
)

$ErrorActionPreference = "Stop"

$repoRoot = Join-Path $PSScriptRoot ".."
$clientPath = Join-Path $repoRoot "src\clients\mix-server-client"
$androidPath = Join-Path $clientPath "android"
$androidPackageRoot = Join-Path $androidPath "app\src\main\java\com\mixserver\app"
$apiProjectPath = Join-Path $repoRoot "src\api\MixServer\MixServer.csproj"
$logDirectoryPath = Join-Path $repoRoot "data\logs"

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

function Get-AndroidSdkRoot {
    if ($env:ANDROID_SDK_ROOT) {
        return $env:ANDROID_SDK_ROOT
    }

    if ($env:ANDROID_HOME) {
        return $env:ANDROID_HOME
    }

    return Join-Path $env:LOCALAPPDATA "Android\Sdk"
}

function Update-MainActivity {
    $javaPath = Join-Path $androidPackageRoot "MainActivity.java"
    $kotlinPath = Join-Path $androidPackageRoot "MainActivity.kt"

    if (Test-Path $javaPath) {
        $updatedJava = @"
package com.mixserver.app;

import android.os.Bundle;
import com.getcapacitor.BridgeActivity;

public class MainActivity extends BridgeActivity {
}
"@
        [System.IO.File]::WriteAllText($javaPath, $updatedJava)
        return
    }

    if (Test-Path $kotlinPath) {
        $updatedKotlin = @"
package com.mixserver.app

import android.os.Bundle
import com.getcapacitor.BridgeActivity

class MainActivity : BridgeActivity()
"@
        [System.IO.File]::WriteAllText($kotlinPath, $updatedKotlin)
        return
    }

    throw "Could not find MainActivity.java or MainActivity.kt under $androidPackageRoot"
}

function Update-RootBuildGradle {
    $rootBuildGradlePath = Join-Path $androidPath "build.gradle"
    $content = Get-Content $rootBuildGradlePath -Raw

    if ($content -notmatch "kotlin-gradle-plugin") {
        $content = $content.Replace(
@"
        classpath 'com.google.gms:google-services:4.4.4'
"@,
@"
        classpath 'com.google.gms:google-services:4.4.4'
        classpath 'org.jetbrains.kotlin:kotlin-gradle-plugin:2.2.21'
"@)
    }

    [System.IO.File]::WriteAllText($rootBuildGradlePath, $content)
}

function Update-BuildGradle {
    $buildGradlePath = Join-Path $androidPath "app\build.gradle"
    $content = Get-Content $buildGradlePath -Raw

    [System.IO.File]::WriteAllText($buildGradlePath, $content)
}

function Update-AndroidManifest {
    $manifestPath = Join-Path $androidPath "app\src\main\AndroidManifest.xml"
    $content = Get-Content $manifestPath -Raw

    [System.IO.File]::WriteAllText($manifestPath, $content)
}

function Start-ApiProcess {
    if (-not $StartApi) {
        return
    }

    $existingApiConnection = Get-NetTCPConnection -LocalPort 5225 -State Listen -ErrorAction SilentlyContinue |
        Select-Object -First 1

    if ($RestartApi -and $existingApiConnection) {
        $existingApiProcess = Get-Process -Id $existingApiConnection.OwningProcess -ErrorAction SilentlyContinue

        if ($null -eq $existingApiProcess) {
            throw "Found a listening process on port 5225, but could not resolve its process details."
        }

        if ($existingApiProcess.ProcessName -notin @("dotnet", "MixServer")) {
            throw "Refusing to stop process '$($existingApiProcess.ProcessName)' on port 5225. Restart it manually or rerun without -RestartApi."
        }

        Stop-Process -Id $existingApiProcess.Id -Force
        Start-Sleep -Seconds 2
    }

    if (Test-NetConnection -ComputerName "localhost" -Port 5225 -InformationLevel Quiet) {
        Write-Host "  [✓] API already running on http://localhost:5225" -ForegroundColor Green
        return
    }

    New-Item -ItemType Directory -Path $logDirectoryPath -Force | Out-Null

    $stdoutPath = Join-Path $logDirectoryPath "android-api.stdout.log"
    $stderrPath = Join-Path $logDirectoryPath "android-api.stderr.log"

    Start-Process `
        -FilePath "dotnet" `
        -ArgumentList @("run", "--project", $apiProjectPath) `
        -WorkingDirectory $repoRoot `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath `
        -WindowStyle Hidden

    $apiReady = $false
    for ($i = 0; $i -lt 60; $i++) {
        Start-Sleep -Seconds 2
        if (Test-NetConnection -ComputerName "localhost" -Port 5225 -InformationLevel Quiet) {
            $apiReady = $true
            break
        }
    }

    if (-not $apiReady) {
        throw "The API did not start within 120 seconds. Check $stdoutPath and $stderrPath."
    }

    Write-Host "  [✓] API started on http://localhost:5225" -ForegroundColor Green
}

function Wait-ForEmulatorBoot {
    param([string]$AdbPath)

    & $AdbPath wait-for-device | Out-Null

    for ($i = 0; $i -lt 90; $i++) {
        $bootCompleted = (& $AdbPath shell getprop sys.boot_completed 2>$null).Trim()
        if ($bootCompleted -eq "1") {
            return
        }

        Start-Sleep -Seconds 2
    }

    throw "The Android emulator did not finish booting in time."
}

function Set-EnvironmentVariable {
    param(
        [string]$Name,
        [string]$Value
    )

    $previousValue = [Environment]::GetEnvironmentVariable($Name)
    Set-Item -Path "Env:$Name" -Value $Value
    return $previousValue
}

function Restore-EnvironmentVariable {
    param(
        [string]$Name,
        [string]$PreviousValue
    )

    if ($null -eq $PreviousValue) {
        Remove-Item -Path "Env:$Name" -ErrorAction SilentlyContinue
        return
    }

    Set-Item -Path "Env:$Name" -Value $PreviousValue
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Mix Server Android Run" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$androidSdkRoot = Get-AndroidSdkRoot
$adbPath = Join-Path $androidSdkRoot "platform-tools\adb.exe"
$emulatorPath = Join-Path $androidSdkRoot "emulator\emulator.exe"
$javaHome = if ($env:JAVA_HOME) { $env:JAVA_HOME } else { "C:\Program Files\Android\Android Studio\jbr" }

if (-not (Test-Path $adbPath) -or -not (Test-Path $emulatorPath)) {
    throw "Android SDK tools are missing. Run pwsh scripts/setup-android.ps1 first."
}

$env:JAVA_HOME = $javaHome
$env:ANDROID_HOME = $androidSdkRoot
$env:ANDROID_SDK_ROOT = $androidSdkRoot
$env:Path = "$env:Path;$($androidSdkRoot)\platform-tools;$($androidSdkRoot)\emulator"

Write-Step "Step 1: Starting the API..."
Start-ApiProcess

Write-Step "Step 2: Building and syncing the Capacitor Android project..."

Push-Location $clientPath
try {
    if (-not (Test-Path $androidPath)) {
        npx cap add android
        Assert-LastExitCode -Action "Adding the Capacitor Android platform"
    }

    $previousAndroidScheme = Set-EnvironmentVariable -Name "CAPACITOR_ANDROID_SCHEME" -Value "http"
    $previousAllowMixedContent = Set-EnvironmentVariable -Name "CAPACITOR_ANDROID_ALLOW_MIXED_CONTENT" -Value "true"
    $previousCleartext = Set-EnvironmentVariable -Name "CAPACITOR_SERVER_CLEARTEXT" -Value "true"

    Write-Host "  [i] Building the Android app with HTTP localhost and cleartext enabled for emulator API access" -ForegroundColor Cyan

    npm run cap:build
    Assert-LastExitCode -Action "Building and syncing the Capacitor Android project"
}
finally {
    Restore-EnvironmentVariable -Name "CAPACITOR_ANDROID_ALLOW_MIXED_CONTENT" -PreviousValue $previousAllowMixedContent
    Restore-EnvironmentVariable -Name "CAPACITOR_ANDROID_SCHEME" -PreviousValue $previousAndroidScheme
    Restore-EnvironmentVariable -Name "CAPACITOR_SERVER_CLEARTEXT" -PreviousValue $previousCleartext
    Pop-Location
}

Write-Step "Step 3: Applying Android configuration..."

Update-MainActivity
Update-RootBuildGradle
Update-BuildGradle
Update-AndroidManifest

Write-Host "  [✓] Android configuration applied" -ForegroundColor Green

Write-Step "Step 4: Starting the Android emulator..."

$runningDevices = & $adbPath devices
if ($runningDevices -notmatch "emulator-") {
    Start-Process -FilePath $emulatorPath -ArgumentList @("-avd", $AvdName)
    Wait-ForEmulatorBoot -AdbPath $adbPath
}
else {
    Write-Host "  [✓] Reusing existing emulator" -ForegroundColor Green
}

Write-Step "Step 5: Installing and launching Mix Server..."

Push-Location $androidPath
try {
    & .\gradlew.bat installDebug
    Assert-LastExitCode -Action "Installing the Android debug build"
}
finally {
    Pop-Location
}

& $adbPath shell monkey -p com.mixserver.app -c android.intent.category.LAUNCHER 1 | Out-Null
Assert-LastExitCode -Action "Launching the Android app"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Mix Server is running on Android" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "If this is the first launch, sign in with the server URL: $ApiUrl" -ForegroundColor Cyan
