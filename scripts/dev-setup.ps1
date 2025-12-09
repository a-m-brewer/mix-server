# Development Environment Setup Script for Mix Server
# This script automates the initial setup of the development environment

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Mix Server Development Environment Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$repoRoot = Join-Path -Path "$PSScriptRoot" -ChildPath ".."

# Function to check if a command exists
function Test-CommandExists {
    param($Command)
    $null -ne (Get-Command $Command -ErrorAction SilentlyContinue)
}

# Step 1: Check Prerequisites
Write-Host "Step 1: Checking prerequisites..." -ForegroundColor Yellow

$allPrerequisitesMet = $true

# Check .NET SDK
if (Test-CommandExists "dotnet") {
    $dotnetVersion = dotnet --version
    Write-Host "  [✓] .NET SDK installed: $dotnetVersion" -ForegroundColor Green
} else {
    Write-Host "  [✗] .NET SDK not found. Please install .NET 10.0 SDK or later." -ForegroundColor Red
    Write-Host "      Download from: https://dotnet.microsoft.com/download" -ForegroundColor Red
    $allPrerequisitesMet = $false
}

# Check Node.js
if (Test-CommandExists "node") {
    $nodeVersion = node --version
    Write-Host "  [✓] Node.js installed: $nodeVersion" -ForegroundColor Green
} else {
    Write-Host "  [✗] Node.js not found. Please install Node.js LTS (22+)." -ForegroundColor Red
    Write-Host "      Download from: https://nodejs.org/" -ForegroundColor Red
    $allPrerequisitesMet = $false
}

# Check npm
if (Test-CommandExists "npm") {
    $npmVersion = npm --version
    Write-Host "  [✓] npm installed: $npmVersion" -ForegroundColor Green
} else {
    Write-Host "  [✗] npm not found. It should be installed with Node.js." -ForegroundColor Red
    $allPrerequisitesMet = $false
}

# Check PowerShell version
$psVersion = $PSVersionTable.PSVersion
if ($psVersion.Major -ge 7) {
    Write-Host "  [✓] PowerShell Core installed: $psVersion" -ForegroundColor Green
} else {
    Write-Host "  [!] PowerShell $psVersion detected. PowerShell Core 7+ is recommended." -ForegroundColor Yellow
    Write-Host "      Download from: https://github.com/PowerShell/PowerShell/releases" -ForegroundColor Yellow
}

# Check ffmpeg (optional)
if (Test-CommandExists "ffmpeg") {
    $ffmpegVersion = ffmpeg -version | Select-Object -First 1
    Write-Host "  [✓] ffmpeg installed: $($ffmpegVersion.Split(' ')[2])" -ForegroundColor Green
} else {
    Write-Host "  [!] ffmpeg not found (optional for transcoding features)." -ForegroundColor Yellow
    Write-Host "      Download from: https://ffmpeg.org/download.html" -ForegroundColor Yellow
}

Write-Host ""

if (-not $allPrerequisitesMet) {
    Write-Host "ERROR: Required prerequisites are missing. Please install them and run this script again." -ForegroundColor Red
    exit 1
}

# Step 2: Create data directory
Write-Host "Step 2: Creating data directory..." -ForegroundColor Yellow
$dataDir = Join-Path -Path $repoRoot -ChildPath "data"
if (-not (Test-Path $dataDir)) {
    New-Item -ItemType Directory -Path $dataDir -Force | Out-Null
    Write-Host "  [✓] Created data directory at: $dataDir" -ForegroundColor Green
} else {
    Write-Host "  [✓] Data directory already exists: $dataDir" -ForegroundColor Green
}

# Create a .gitkeep file to track the directory
$gitkeepPath = Join-Path -Path $dataDir -ChildPath ".gitkeep"
if (-not (Test-Path $gitkeepPath)) {
    New-Item -ItemType File -Path $gitkeepPath -Force | Out-Null
    Write-Host "  [✓] Created .gitkeep in data directory" -ForegroundColor Green
}

# Create a media subdirectory for sample audio files
$mediaDir = Join-Path -Path $dataDir -ChildPath "media"
if (-not (Test-Path $mediaDir)) {
    New-Item -ItemType Directory -Path $mediaDir -Force | Out-Null
    Write-Host "  [✓] Created media directory at: $mediaDir" -ForegroundColor Green
    Write-Host "      You can place your audio files here for testing" -ForegroundColor Cyan
} else {
    Write-Host "  [✓] Media directory already exists: $mediaDir" -ForegroundColor Green
}

Write-Host ""

# Step 3: Create appsettings.Local.json if it doesn't exist
Write-Host "Step 3: Creating local configuration..." -ForegroundColor Yellow
$appsettingsLocalPath = Join-Path -Path $repoRoot -ChildPath "src" -AdditionalChildPath "api", "MixServer", "appsettings.Local.json"

if (-not (Test-Path $appsettingsLocalPath)) {
    # Get the relative path to media directory from MixServer project
    $relativeMediaPath = "../../../data/media"
    
    $appsettingsContent = @"
{
  "RootFolder": {
    "Children": "$relativeMediaPath"
  }
}
"@
    
    $appsettingsContent | Out-File -FilePath $appsettingsLocalPath -Encoding utf8
    Write-Host "  [✓] Created appsettings.Local.json" -ForegroundColor Green
    Write-Host "      Location: $appsettingsLocalPath" -ForegroundColor Cyan
    Write-Host "      Configured to use: $mediaDir" -ForegroundColor Cyan
} else {
    Write-Host "  [✓] appsettings.Local.json already exists" -ForegroundColor Green
    Write-Host "      Location: $appsettingsLocalPath" -ForegroundColor Cyan
}

Write-Host ""

# Step 4: Install Angular dependencies
Write-Host "Step 4: Installing Angular dependencies..." -ForegroundColor Yellow
$angularClientPath = Join-Path -Path $repoRoot -ChildPath "src" -AdditionalChildPath "clients", "mix-server-client"

if (Test-Path $angularClientPath) {
    Push-Location $angularClientPath
    try {
        Write-Host "  Installing npm packages (this may take a few minutes)..." -ForegroundColor Cyan
        npm ci 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  [✓] Angular dependencies installed successfully" -ForegroundColor Green
        } else {
            Write-Host "  [!] npm ci failed, trying npm install..." -ForegroundColor Yellow
            npm install
            if ($LASTEXITCODE -eq 0) {
                Write-Host "  [✓] Angular dependencies installed successfully" -ForegroundColor Green
            } else {
                Write-Host "  [✗] Failed to install Angular dependencies" -ForegroundColor Red
            }
        }
    }
    finally {
        Pop-Location
    }
} else {
    Write-Host "  [✗] Angular client directory not found at: $angularClientPath" -ForegroundColor Red
}

Write-Host ""

# Step 5: Build Angular client
Write-Host "Step 5: Building Angular client..." -ForegroundColor Yellow
if (Test-Path $angularClientPath) {
    Push-Location $angularClientPath
    try {
        Write-Host "  Building Angular application (this may take a few minutes)..." -ForegroundColor Cyan
        npx ng build 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  [✓] Angular client built successfully" -ForegroundColor Green
        } else {
            Write-Host "  [✗] Failed to build Angular client" -ForegroundColor Red
            Write-Host "      You may need to build it manually: cd $angularClientPath && npx ng build" -ForegroundColor Yellow
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host ""

# Step 6: Link wwwroot
Write-Host "Step 6: Linking wwwroot to Angular build output..." -ForegroundColor Yellow
$linkWwwrootScript = Join-Path -Path $PSScriptRoot -ChildPath "link_wwwroot.ps1"
if (Test-Path $linkWwwrootScript) {
    & $linkWwwrootScript
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [✓] wwwroot linked successfully" -ForegroundColor Green
    } else {
        Write-Host "  [!] Failed to link wwwroot" -ForegroundColor Yellow
    }
} else {
    Write-Host "  [✗] link_wwwroot.ps1 script not found" -ForegroundColor Red
}

Write-Host ""

# Step 7: Restore .NET dependencies
Write-Host "Step 7: Restoring .NET dependencies..." -ForegroundColor Yellow
Push-Location $repoRoot
try {
    dotnet restore MixServer.sln 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [✓] .NET dependencies restored successfully" -ForegroundColor Green
    } else {
        Write-Host "  [✗] Failed to restore .NET dependencies" -ForegroundColor Red
    }
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Place audio files in: $mediaDir" -ForegroundColor Cyan
Write-Host "  2. Build the solution: dotnet build MixServer.sln" -ForegroundColor Cyan
Write-Host "  3. Run the API: dotnet run --project src/api/MixServer/MixServer.csproj" -ForegroundColor Cyan
Write-Host "  4. Access the application:" -ForegroundColor Cyan
Write-Host "     - Angular UI: http://localhost:4200" -ForegroundColor Cyan
Write-Host "     - API: http://localhost:5225" -ForegroundColor Cyan
Write-Host ""
Write-Host "For development with live reload:" -ForegroundColor Yellow
Write-Host "  - Frontend: cd src/clients/mix-server-client && npx ng serve" -ForegroundColor Cyan
Write-Host "  - Backend: dotnet watch --project src/api/MixServer/MixServer.csproj" -ForegroundColor Cyan
Write-Host ""
Write-Host "See AGENTS.md for more detailed information about the project structure and workflows." -ForegroundColor Cyan
Write-Host ""
