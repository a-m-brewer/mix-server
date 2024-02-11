$dotnetVersion = "net8.0"
$rootProj = Join-Path -Path "$PSScriptRoot" -ChildPath ".."

$mixServerDist = Join-Path -Path $rootProj  -ChildPath "src" -AdditionalChildPath "clients", "mix-server-client", "dist", "mix-server-client"

if (-Not (Test-Path $mixServerDist)) {
    New-Item -Force -ItemType Directory -Path $mixServerDist
}

$apiDebugBin = Join-Path -Path $rootProj -ChildPath "src" -AdditionalChildPath "api", "MixServer", "bin", "Debug", $dotnetVersion
$wwwRootName = "wwwroot"
$apiWwwRoot = Join-Path -Path $apiDebugBin -ChildPath $wwwRootName
if (Test-Path $apiWwwRoot) {
    Remove-Item -Force -Recurse $apiWwwRoot
}

New-Item -ItemType SymbolicLink -Path $apiDebugBin -Name $wwwRootName -Value $mixServerDist

Write-Host "wwwroot => mix-server-client junction created successfully" -ForegroundColor Green