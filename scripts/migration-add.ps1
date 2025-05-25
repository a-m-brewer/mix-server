[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$MigrationName
)

$repoRoot = Join-Path -Path "$PSScriptRoot" -ChildPath ".."
$infrastructureProj = Join-Path -Path $repoRoot  -ChildPath "src" -AdditionalChildPath "api", "MixServer.Infrastructure"

dotnet ef migrations add "$MigrationName"  --project $infrastructureProj
