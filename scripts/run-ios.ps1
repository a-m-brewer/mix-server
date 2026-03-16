# Builds and runs the Mix Server iOS app on an iPhone simulator.

param(
    [string]$SimulatorName = "iPhone 17",
    [string]$ApiUrl = "http://localhost:5225",
    [switch]$RestartApi,
    [switch]$StartApi
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

function Get-SimulatorUdid {
    param([string]$Name)

    $lines = xcrun simctl list devices available 2>&1
    foreach ($line in $lines) {
        if ($line -match "^\s+$([regex]::Escape($Name))\s+\(([0-9A-F-]+)\)") {
            return $matches[1]
        }
    }

    throw "Simulator '$Name' not found. Run: xcrun simctl list devices available"
}

function Get-SimulatorState {
    param([string]$Udid)

    $line = xcrun simctl list devices 2>&1 | Select-String $Udid | Select-Object -First 1
    if ($line -and $line.Line -match "\((\w+)\)\s*$") {
        return $matches[1]
    }

    return "Unknown"
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
        Write-Host "  [✓] API already running on http://localhost:5225" -ForegroundColor Green
        return
    }

    New-Item -ItemType Directory -Path $logDirectoryPath -Force | Out-Null

    $stdoutPath = Join-Path $logDirectoryPath "ios-api.stdout.log"
    $stderrPath = Join-Path $logDirectoryPath "ios-api.stderr.log"

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

    Write-Host "  [✓] API started on http://localhost:5225" -ForegroundColor Green
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

    # Allow HTTP to localhost for simulator API access (ATS exception)
    $ats = /usr/libexec/PlistBuddy -c "Print :NSAppTransportSecurity" $infoPlistPath 2>&1
    if ($LASTEXITCODE -ne 0) {
        /usr/libexec/PlistBuddy -c "Add :NSAppTransportSecurity dict" $infoPlistPath | Out-Null
        /usr/libexec/PlistBuddy -c "Add :NSAppTransportSecurity:NSAllowsLocalNetworking bool true" $infoPlistPath | Out-Null
    }

    Write-Host "  [✓] Info.plist configured for background audio and local networking" -ForegroundColor Green
}

function Add-UITestToXcodeProject {
    param([string]$ProjectPath)

    $uiTestSourceDir  = Join-Path $iosAppPath "LoginAuthUITests"
    $uiTestSwiftFile  = Join-Path $uiTestSourceDir "LoginAuthUITests.swift"

    if (-not (Test-Path $uiTestSwiftFile)) {
        Write-Host "  [!] LoginAuthUITests.swift not found at $uiTestSwiftFile — skipping UITest target registration." -ForegroundColor Yellow
        return
    }

    if (-not (Get-Command ruby -ErrorAction SilentlyContinue)) {
        Write-Host "  [!] Ruby not found — skipping UITest target registration." -ForegroundColor Yellow
        return
    }

    ruby -e "require 'xcodeproj'" 2>/dev/null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  [!] xcodeproj gem not available. Install with: gem install xcodeproj" -ForegroundColor Yellow
        return
    }

    $rubyScript = @'
require 'xcodeproj'
require 'fileutils'

project_path    = ARGV[0]
ui_test_src_dir = ARGV[1]
target_name     = 'LoginAuthUITests'

project     = Xcodeproj::Project.open(project_path)
main_target = project.targets.find { |t| t.name == 'App' }
abort("Target 'App' not found in #{project_path}") unless main_target

# ── UITest target ─────────────────────────────────────────────────────────────

test_target = project.targets.find { |t| t.name == target_name }

unless test_target
  test_target = project.new_target(:ui_test_bundle, target_name, :ios, '16.4')

  test_target.build_configurations.each do |config|
    config.build_settings['PRODUCT_NAME']                            = target_name
    config.build_settings['PRODUCT_MODULE_NAME']                     = target_name
    config.build_settings['BUNDLE_IDENTIFIER']                       = 'com.mixserver.app.LoginAuthUITests'
    config.build_settings['PRODUCT_BUNDLE_IDENTIFIER']               = 'com.mixserver.app.LoginAuthUITests'
    config.build_settings['TEST_TARGET_NAME']                        = 'App'
    config.build_settings['IPHONEOS_DEPLOYMENT_TARGET']              = '16.4'
    config.build_settings['SWIFT_VERSION']                           = '5.0'
    config.build_settings['CODE_SIGN_IDENTITY']                      = ''
    config.build_settings['CODE_SIGNING_REQUIRED']                   = 'NO'
    config.build_settings['CODE_SIGNING_ALLOWED']                    = 'NO'
    config.build_settings['ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES']   = 'YES'
  end

  test_target.add_dependency(main_target)

  test_group = project.main_group.find_subpath(target_name, false) ||
               project.main_group.new_group(target_name, target_name)

  swift_filename = 'LoginAuthUITests.swift'
  unless test_group.files.any? { |f| f.display_name == swift_filename }
    file_ref = test_group.new_file(swift_filename)
    test_target.source_build_phase.add_file_reference(file_ref)
    puts "  Added #{swift_filename} to Xcode project"
  end

  project.save
  puts "  UITest target '#{target_name}' registered in Xcode project"
else
  puts "  UITest target '#{target_name}' already registered"
end

# ── Shared scheme ─────────────────────────────────────────────────────────────
# Create a shared xcscheme so that `xcodebuild test -scheme LoginAuthUITests`
# works without needing Xcode to generate one interactively.

schemes_dir  = File.join(project_path, 'xcshareddata', 'xcschemes')
scheme_path  = File.join(schemes_dir, "#{target_name}.xcscheme")

app_uuid  = main_target.uuid
test_uuid = test_target.uuid

# ── App.xcscheme (regular build / run) ───────────────────────────────────────
app_scheme_path = File.join(schemes_dir, 'App.xcscheme')
unless File.exist?(app_scheme_path)
  FileUtils.mkdir_p(schemes_dir)
  File.write(app_scheme_path, <<~XML)
    <?xml version="1.0" encoding="UTF-8"?>
    <Scheme LastUpgradeVersion="1530" version="1.7">
       <BuildAction parallelizeBuildables="YES" buildImplicitDependencies="YES">
          <BuildActionEntries>
             <BuildActionEntry buildForTesting="YES" buildForRunning="YES" buildForProfiling="YES" buildForArchiving="YES" buildForAnalyzing="YES">
                <BuildableReference
                   BuildableIdentifier = "primary"
                   BlueprintIdentifier = "#{app_uuid}"
                   BuildableName = "App.app"
                   BlueprintName = "App"
                   ReferencedContainer = "container:App.xcodeproj">
                </BuildableReference>
             </BuildActionEntry>
          </BuildActionEntries>
       </BuildAction>
       <TestAction
          buildConfiguration = "Debug"
          selectedDebuggerIdentifier = "Xcode.DebuggerFoundation.Debugger.LLDB"
          selectedLauncherIdentifier = "Xcode.DebuggerFoundation.Launcher.LLDB"
          shouldUseLaunchSchemeArgsEnv = "YES">
          <Testables/>
       </TestAction>
       <LaunchAction
          buildConfiguration = "Debug"
          selectedDebuggerIdentifier = "Xcode.DebuggerFoundation.Debugger.LLDB"
          selectedLauncherIdentifier = "Xcode.DebuggerFoundation.Launcher.LLDB"
          launchStyle = "0"
          useCustomWorkingDirectory = "NO"
          ignoresPersistentStateOnLaunch = "NO"
          debugDocumentVersioning = "YES"
          debugServiceExtension = "internal"
          allowLocationSimulation = "YES">
          <BuildableProductRunnable runnableDebuggingMode = "0">
             <BuildableReference
                BuildableIdentifier = "primary"
                BlueprintIdentifier = "#{app_uuid}"
                BuildableName = "App.app"
                BlueprintName = "App"
                ReferencedContainer = "container:App.xcodeproj">
             </BuildableReference>
          </BuildableProductRunnable>
       </LaunchAction>
       <ProfileAction
          buildConfiguration = "Release"
          shouldUseLaunchSchemeArgsEnv = "YES"
          savedToolIdentifier = ""
          useCustomWorkingDirectory = "NO"
          debugDocumentVersioning = "YES">
          <BuildableProductRunnable runnableDebuggingMode = "0">
             <BuildableReference
                BuildableIdentifier = "primary"
                BlueprintIdentifier = "#{app_uuid}"
                BuildableName = "App.app"
                BlueprintName = "App"
                ReferencedContainer = "container:App.xcodeproj">
             </BuildableReference>
          </BuildableProductRunnable>
       </ProfileAction>
       <AnalyzeAction buildConfiguration = "Debug"/>
       <ArchiveAction buildConfiguration = "Release" revealArchiveInOrganizer = "YES"/>
    </Scheme>
  XML
  puts "  Created shared App.xcscheme"
else
  puts "  App.xcscheme already exists"
end

# ── LoginAuthUITests.xcscheme (UITest) ────────────────────────────────────────
unless File.exist?(scheme_path)
  FileUtils.mkdir_p(schemes_dir)

  scheme_xml = <<~XML
    <?xml version="1.0" encoding="UTF-8"?>
    <Scheme LastUpgradeVersion="1530" version="1.7">
       <BuildAction parallelizeBuildables="YES" buildImplicitDependencies="YES">
          <BuildActionEntries>
             <BuildActionEntry buildForTesting="YES" buildForRunning="YES" buildForProfiling="NO" buildForArchiving="NO" buildForAnalyzing="YES">
                <BuildableReference
                   BuildableIdentifier = "primary"
                   BlueprintIdentifier = "#{app_uuid}"
                   BuildableName = "App.app"
                   BlueprintName = "App"
                   ReferencedContainer = "container:App.xcodeproj">
                </BuildableReference>
             </BuildActionEntry>
             <BuildActionEntry buildForTesting="YES" buildForRunning="NO" buildForProfiling="NO" buildForArchiving="NO" buildForAnalyzing="YES">
                <BuildableReference
                   BuildableIdentifier = "primary"
                   BlueprintIdentifier = "#{test_uuid}"
                   BuildableName = "#{target_name}.xctest"
                   BlueprintName = "#{target_name}"
                   ReferencedContainer = "container:App.xcodeproj">
                </BuildableReference>
             </BuildActionEntry>
          </BuildActionEntries>
       </BuildAction>
       <TestAction
          buildConfiguration = "Debug"
          selectedDebuggerIdentifier = "Xcode.DebuggerFoundation.Debugger.LLDB"
          selectedLauncherIdentifier = "Xcode.DebuggerFoundation.Launcher.LLDB"
          shouldUseLaunchSchemeArgsEnv = "YES">
          <Testables>
             <TestableReference skipped = "NO">
                <BuildableReference
                   BuildableIdentifier = "primary"
                   BlueprintIdentifier = "#{test_uuid}"
                   BuildableName = "#{target_name}.xctest"
                   BlueprintName = "#{target_name}"
                   ReferencedContainer = "container:App.xcodeproj">
                </BuildableReference>
             </TestableReference>
          </Testables>
       </TestAction>
       <LaunchAction
          buildConfiguration = "Debug"
          selectedDebuggerIdentifier = "Xcode.DebuggerFoundation.Debugger.LLDB"
          selectedLauncherIdentifier = "Xcode.DebuggerFoundation.Launcher.LLDB"
          launchStyle = "0"
          useCustomWorkingDirectory = "NO"
          ignoresPersistentStateOnLaunch = "NO"
          debugDocumentVersioning = "YES"
          debugServiceExtension = "internal"
          allowLocationSimulation = "YES">
          <BuildableProductRunnable runnableDebuggingMode = "0">
             <BuildableReference
                BuildableIdentifier = "primary"
                BlueprintIdentifier = "#{app_uuid}"
                BuildableName = "App.app"
                BlueprintName = "App"
                ReferencedContainer = "container:App.xcodeproj">
             </BuildableReference>
          </BuildableProductRunnable>
       </LaunchAction>
       <ProfileAction
          buildConfiguration = "Release"
          shouldUseLaunchSchemeArgsEnv = "YES"
          savedToolIdentifier = ""
          useCustomWorkingDirectory = "NO"
          debugDocumentVersioning = "YES">
          <BuildableProductRunnable runnableDebuggingMode = "0">
             <BuildableReference
                BuildableIdentifier = "primary"
                BlueprintIdentifier = "#{app_uuid}"
                BuildableName = "App.app"
                BlueprintName = "App"
                ReferencedContainer = "container:App.xcodeproj">
             </BuildableReference>
          </BuildableProductRunnable>
       </ProfileAction>
       <AnalyzeAction buildConfiguration = "Debug"/>
       <ArchiveAction buildConfiguration = "Release" revealArchiveInOrganizer = "YES"/>
    </Scheme>
  XML

  File.write(scheme_path, scheme_xml)
  puts "  Created shared scheme: #{scheme_path}"
else
  puts "  Shared scheme already exists"
end
'@

    $tempScript = [System.IO.Path]::GetTempFileName() + ".rb"
    [System.IO.File]::WriteAllText($tempScript, $rubyScript)

    try {
        ruby $tempScript $ProjectPath $uiTestSourceDir
        Assert-LastExitCode -Action "Registering UITest target in Xcode project"
        Write-Host "  [✓] XCUITest target registered" -ForegroundColor Green
    }
    finally {
        Remove-Item $tempScript -ErrorAction SilentlyContinue
    }
}

function Boot-Simulator {
    param([string]$Udid)

    $state = Get-SimulatorState -Udid $Udid
    if ($state -ne "Booted") {
        xcrun simctl boot $Udid
        Assert-LastExitCode -Action "Booting the simulator"

        for ($i = 0; $i -lt 30; $i++) {
            Start-Sleep -Seconds 2
            if ((Get-SimulatorState -Udid $Udid) -eq "Booted") {
                break
            }
        }
    }

    # Open Simulator.app so the simulator window is visible
    Open-Application "Simulator"

    Write-Host "  [✓] Simulator booted and visible" -ForegroundColor Green
}

function Open-Application {
    param([string]$AppName)

    $appPath = "/Applications/$AppName.app"
    if (Test-Path $appPath) {
        Start-Process "open" -ArgumentList @("-a", $AppName) -Wait:$false
    }
}

# ──────────────────────────────────────────────
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Mix Server iOS Run" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$simulatorUdid = Get-SimulatorUdid -Name $SimulatorName

Write-Step "Step 1: Starting the API..."
Start-ApiProcess

Write-Step "Step 2: Building and syncing the Capacitor iOS project..."

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

Write-Step "Step 3: Applying iOS configuration..."

$xcodeProjectPath = Join-Path $iosAppPath "App.xcodeproj"

Update-InfoPlist

Write-Host "  [✓] iOS configuration applied" -ForegroundColor Green

Write-Step "Step 3b: Registering the XCUITest target in the Xcode project..."
Add-UITestToXcodeProject -ProjectPath $xcodeProjectPath

Write-Step "Step 4: Booting the $SimulatorName simulator..."
Boot-Simulator -Udid $simulatorUdid

Write-Step "Step 5: Building the iOS app..."

New-Item -ItemType Directory -Path $buildPath -Force | Out-Null
Push-Location $iosAppPath
try {
    # Resolve Swift Package Manager dependencies before building
    xcodebuild `
        -project "App.xcodeproj" `
        -resolvePackageDependencies `
        -clonedSourcePackagesDirPath "$buildPath/SourcePackages"
    Assert-LastExitCode -Action "Resolving Swift Package Manager dependencies"

    xcodebuild `
        -project "App.xcodeproj" `
        -scheme "App" `
        -destination "platform=iOS Simulator,name=$SimulatorName" `
        -configuration Debug `
        -derivedDataPath $buildPath `
        -clonedSourcePackagesDirPath "$buildPath/SourcePackages" `
        CODE_SIGN_IDENTITY="" `
        CODE_SIGNING_REQUIRED=NO `
        CODE_SIGNING_ALLOWED=NO `
        build
    Assert-LastExitCode -Action "Building the iOS app"
}
finally {
    Pop-Location
}

Write-Step "Step 6: Installing and launching Mix Server..."

$appPath = Join-Path $buildPath "Build/Products/Debug-iphonesimulator/App.app"
if (-not (Test-Path $appPath)) {
    throw "Built app not found at: $appPath"
}

xcrun simctl install $simulatorUdid $appPath
Assert-LastExitCode -Action "Installing the app on the simulator"

xcrun simctl launch $simulatorUdid "com.mixserver.app"
Assert-LastExitCode -Action "Launching the app"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Mix Server is running on iOS Simulator" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Simulator: $SimulatorName ($simulatorUdid)" -ForegroundColor Cyan
Write-Host "If this is the first launch, sign in with the server URL: $ApiUrl" -ForegroundColor Cyan
