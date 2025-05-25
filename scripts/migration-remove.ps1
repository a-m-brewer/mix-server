$repoRoot = Join-Path -Path "$PSScriptRoot" -ChildPath ".."
$infrastructreProj = Join-Path -Path $repoRoot  -ChildPath "src" -AdditionalChildPath "api", "MixServer.Infrastructure"

dotnet ef migrations remove --project (Resolve-Path $infrastructreProj).Path