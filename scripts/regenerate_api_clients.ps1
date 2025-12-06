$ErrorActionPreference = "Stop"

# Requires version of nswag CLI that matches the installed version in Mix Server
npm install -g nswag@14.2.0

# Frontend
$frontendPath = Join-Path -Path $PSScriptRoot -ChildPath ".." -AdditionalChildPath "src","clients","mix-server-client"

$frontendNswagConfigsPath = Join-Path -Path $frontendPath -ChildPath "nswag-configs"

Push-Location $frontendNswagConfigsPath

nswag run

Pop-Location
