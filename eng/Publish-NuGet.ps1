param(
    [string]$Configuration = "Release",
    [string]$Output = "artifacts/nuget",
    [string]$Version,
    [string]$Source = "https://api.nuget.org/v3/index.json",
    [switch]$SkipPack
)

$ErrorActionPreference = "Stop"

$repositoryRoot = Join-Path $PSScriptRoot ".."
$outputPath = Join-Path $repositoryRoot $Output
$apiKey = $env:NUGET_API_KEY

if ([string]::IsNullOrWhiteSpace($apiKey)) {
    throw "Set the NUGET_API_KEY environment variable before publishing."
}

if (-not $SkipPack) {
    & (Join-Path $PSScriptRoot "Pack.ps1") -Configuration $Configuration -Output $Output -Version $Version
}

$packages = Get-ChildItem -LiteralPath $outputPath -Filter *.nupkg |
    Where-Object { $_.Name -notlike "*.symbols.nupkg" }

foreach ($package in $packages) {
    dotnet nuget push $package.FullName --api-key $apiKey --source $Source --skip-duplicate
}

$symbolPackages = Get-ChildItem -LiteralPath $outputPath -Filter *.snupkg

foreach ($package in $symbolPackages) {
    dotnet nuget push $package.FullName --api-key $apiKey --source $Source --skip-duplicate
}
