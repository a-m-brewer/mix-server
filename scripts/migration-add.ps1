[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$MigrationName
)

$repoRoot = Join-Path -Path "$PSScriptRoot" -ChildPath ".."
$infrastructreProj = Join-Path -Path $repoRoot  -ChildPath "src" -AdditionalChildPath "api", "MixServer.Infrastructure"

dotnet ef migrations add "$MigrationName"  --project $infrastructreProj
