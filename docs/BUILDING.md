# Building and publishing HandyLime

## Build locally

Use Windows x64 with the .NET 10 SDK. Dependency versions and integrity hashes are pinned in the lock files.

```powershell
dotnet restore HandyLime.slnx --locked-mode --configfile NuGet.Config
dotnet build HandyLime.slnx -c Release --no-restore -warnaserror
dotnet run --project tests/HandyLime.Tests -c Release --no-build -- --require-engines
```

The standalone test runner writes JUnit XML and a summary to TestResults; use `dotnet run`, not `dotnet test`. `--require-engines` fails if FFmpeg/FFprobe, LibreOffice or Pandoc are unavailable. See [engine setup](ENGINES.md) and [validation details](TESTING.md).

## Package both editions

```powershell
./scripts/Publish.ps1
./scripts/Test-Packages.ps1
```

Packaging defaults to both editions. `-Edition Portable` or `-Edition Lite` builds only that edition. The script refuses to overwrite an existing app ZIP; move previous artifacts aside before rebuilding.

- **HandyLime-windows-x64.zip**: self-contained app, with .NET Desktop Runtime.
- **HandyLime-windows-x64-lite.zip**: same app, framework-dependent; requires .NET Desktop Runtime 10 x64.
- **HandyLime-corresponding-source.zip**: native dependency source snapshots. Kept separate from app downloads; publish alongside both editions.

All ZIPs receive SHA-256 sidecars. No optional conversion-engine binaries are copied into the app. Trimming is disabled, and the native image DLL stays replaceable. The Lite archive intentionally omits the .NET runtime, not conversion features.

`Test-Packages.ps1` checks each ZIP hash, extracts the package, verifies runtime configuration/required payloads and starts its supported non-interactive brand-export path. That path initializes WPF and the native image policy. Lite verification requires an installed Desktop Runtime (the SDK also includes it). For a private SDK install, set `DOTNET_ROOT_X64` to its directory.

## GitHub Actions

Push the source contents, including `.github`, into the repository root. A push or pull request runs **Windows build and tests**: locked restore, warning-free build, real-engine tests, packaging and extracted-package startup checks.

Actions uploads three independent artifacts. Users do not need to download the library source bundle to run the program. ZIP artifacts use no additional outer compression; their inner app archives are already compressed.

## Publish a public Release

Use either method after updating `Directory.Build.props` and `CHANGELOG.md`:

1. Open **Actions → Windows build and tests → Run workflow** on the default branch. Enable **Publish this version to Releases**, then run. After successful tests, the workflow creates the matching version tag and a public Release.
2. Push a matching version tag, for example:

```powershell
git tag v0.3.1
git push origin v0.3.1
```

The tag must match the project version. Public Releases contain Portable, Lite and the source companion as separate assets with checksums. The release notes explain which file users need in English and Persian. The workflow uses the repository's built-in token; no personal access token or custom secret is required. Publishing is limited to tag pushes or an explicit manual publish from the default branch. Pull requests cannot publish.

Use a new version for a new release. An existing version is not silently replaced. A regular branch push builds artifacts without publishing. Preview status and limitations remain visible in the user guide.

## Source ZIP for sharing

```powershell
./scripts/Pack-Source.ps1 -Destination ../HandyLime-source.zip
```

The source ZIP excludes bin, obj, artifacts, local tools and test scratch data. Native corresponding source is supplied separately, with notices and rebuild instructions in [REBUILD-NATIVE.md](REBUILD-NATIVE.md).
