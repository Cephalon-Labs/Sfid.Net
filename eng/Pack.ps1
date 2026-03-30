param(
    [string]$Configuration = "Release",
    [string]$Output = "artifacts/nuget",
    [string]$Version
)

$ErrorActionPreference = "Stop"

$solution = Join-Path $PSScriptRoot "..\Sfid.Net.slnx"
$outputPath = Join-Path $PSScriptRoot "..\$Output"

New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

dotnet restore $solution
dotnet test $solution -c $Configuration --no-restore

$packArguments = @(
    "pack", $solution,
    "-c", $Configuration,
    "--no-restore",
    "-o", $outputPath,
    "-p:ContinuousIntegrationBuild=true"
)

if (-not [string]::IsNullOrWhiteSpace($Version)) {
    $packArguments += "-p:Version=$Version"
}

dotnet @packArguments
