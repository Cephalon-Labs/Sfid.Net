param(
    [string]$Configuration = "Release",
    [string]$Output = "artifacts/nuget"
)

$ErrorActionPreference = "Stop"

$solution = Join-Path $PSScriptRoot "..\Sfid.Net.slnx"
$outputPath = Join-Path $PSScriptRoot "..\$Output"
$version = & (Join-Path $PSScriptRoot "Get-AssemblyVersion.ps1")
$projectsToPack = @(
    (Join-Path $PSScriptRoot "..\src\Sfid.Net\Sfid.Net.csproj"),
    (Join-Path $PSScriptRoot "..\src\Sfid.EntityFramework\Sfid.EntityFramework.csproj")
)

New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

Write-Host "Packing NuGet artifacts for assembly version $version"

dotnet restore $solution
dotnet test $solution -c $Configuration --no-restore

foreach ($project in $projectsToPack) {
    dotnet pack $project `
        -c $Configuration `
        --no-restore `
        -o $outputPath `
        -p:ContinuousIntegrationBuild=true
}
