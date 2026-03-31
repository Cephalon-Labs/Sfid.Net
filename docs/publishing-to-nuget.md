# Releasing And Publishing Packages

## Package IDs

The repository is prepared to produce two packages:

- `Sfid.Net`
- `Sfid.EntityFramework`

Both packages now include:

- NuGet package metadata
- repository information
- README packaging
- license packaging
- symbol package generation (`.snupkg`)

## Local Validation

Run the local pack script before publishing:

```powershell
pwsh ./eng/Pack.ps1
```

This script performs the following steps:

1. restores the solution
2. runs the full test suite
3. resolves the shared package version from the projects' `AssemblyVersion`
4. packs all NuGet packages into `artifacts/nuget`

## Local Publish

Set your NuGet API key in the environment:

```powershell
$env:NUGET_API_KEY = "YOUR_API_KEY"
```

Then publish:

```powershell
pwsh ./eng/Publish-NuGet.ps1
```

The publish script:

1. optionally repacks the solution
2. resolves the shared package version from the projects' `AssemblyVersion`
3. pushes only the matching `.nupkg` files to `https://api.nuget.org/v3/index.json`
4. pushes the matching `.snupkg` symbol packages
5. skips duplicates to make reruns safer

## GitHub Actions

Two workflows are included:

- `.github/workflows/ci.yml`
  Restores, tests, validates the shared assembly version, and packs the solution on pushes to `main` and on pull requests.
- `.github/workflows/publish.yml`
  Publishes packages to GitHub Packages when a GitHub Release is published for a tag that matches the assembly version.

## Required Secret For NuGet.org

Set the following environment variable before using the local publish script:

- `NUGET_API_KEY`

## Recommended Release Flow

1. update `AssemblyVersion` and `FileVersion` in `src/Sfid.Net/Sfid.Net.csproj` and `src/Sfid.EntityFramework/Sfid.EntityFramework.csproj`
2. run `pwsh ./eng/Pack.ps1`
3. review the generated packages in `artifacts/nuget`
4. create a GitHub Release whose tag is either the exact assembly version such as `1.0.1` or the prefixed form `v1.0.1`
5. confirm that the release workflow published the `.nupkg` files to GitHub Packages
6. if you also want NuGet.org publication, run `pwsh ./eng/Publish-NuGet.ps1`

## Versioning Notes

- Package versions are derived automatically from the projects' `AssemblyVersion`.
- The helper script `eng/Get-AssemblyVersion.ps1` validates that publishable projects stay on the same version before packing or publishing.
- The release workflow rejects mismatched release tags so Git tags, GitHub Releases, and published packages stay aligned.
