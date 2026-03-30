# Publishing To NuGet

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
pwsh ./eng/Pack.ps1 -Configuration Release -Version 0.1.0
```

This script performs the following steps:

1. restores the solution
2. runs the full test suite
3. packs all NuGet packages into `artifacts/nuget`

## Local Publish

Set your NuGet API key in the environment:

```powershell
$env:NUGET_API_KEY = "YOUR_API_KEY"
```

Then publish:

```powershell
pwsh ./eng/Publish-NuGet.ps1 -Configuration Release -Version 0.1.0
```

The publish script:

1. optionally repacks the solution
2. pushes `.nupkg` files to `https://api.nuget.org/v3/index.json`
3. pushes `.snupkg` symbol packages
4. skips duplicates to make reruns safer

## GitHub Actions

Two workflows are included:

- `.github/workflows/ci.yml`
  Restores, tests, and packs the solution on pushes to `main` and on pull requests.
- `.github/workflows/publish.yml`
  Publishes packages either from a manual workflow dispatch or from a tag in the form `vX.Y.Z`.

## Required Secret

Set the following repository secret before using the publish workflow:

- `NUGET_API_KEY`

## Recommended Release Flow

1. run `pwsh ./eng/Pack.ps1 -Configuration Release -Version X.Y.Z`
2. review the generated packages in `artifacts/nuget`
3. push a Git tag such as `vX.Y.Z`, or trigger the publish workflow manually with the same version
4. confirm that both package and symbol uploads succeeded on NuGet

## Versioning Notes

- The repository now defaults to `VersionPrefix = 0.1.0` for packable projects.
- You can override the version at publish time with `-p:Version=X.Y.Z` or by passing `-Version` to the PowerShell scripts.
- Keeping the version external to source control makes it easier to promote the same commit through preview and stable releases.
