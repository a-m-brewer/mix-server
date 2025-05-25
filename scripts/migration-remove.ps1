$repoRoot = Join-Path -Path "$PSScriptRoot" -ChildPath ".."
$infrastructureProj = Join-Path -Path $repoRoot  -ChildPath "src" -AdditionalChildPath "api", "MixServer.Infrastructure"

dotnet ef migrations remove --project (Resolve-Path $infrastructureProj).Path