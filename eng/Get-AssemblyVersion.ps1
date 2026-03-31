param(
    [string[]]$Projects = @(
        "src/Sfid.Net/Sfid.Net.csproj",
        "src/Sfid.EntityFramework/Sfid.EntityFramework.csproj"
    )
)

$ErrorActionPreference = "Stop"

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$versions = @()

foreach ($project in $Projects) {
    $projectPath = Join-Path $repositoryRoot $project

    if (-not (Test-Path -LiteralPath $projectPath)) {
        throw "Project file '$project' was not found."
    }

    [xml]$projectXml = Get-Content -LiteralPath $projectPath -Raw
    $assemblyVersionNode = $projectXml.Project.PropertyGroup.AssemblyVersion |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -First 1

    if ($null -eq $assemblyVersionNode) {
        throw "Project '$project' does not define an AssemblyVersion."
    }

    $versions += [string]$assemblyVersionNode
}

$distinctVersions = @($versions | Sort-Object -Unique)

if ($distinctVersions.Count -ne 1) {
    $details = for ($index = 0; $index -lt $Projects.Count; $index++) {
        "$($Projects[$index]) => $($versions[$index])"
    }

    throw "AssemblyVersion must match across publishable projects. Found:`n$($details -join [Environment]::NewLine)"
}

Write-Output $distinctVersions[0]
