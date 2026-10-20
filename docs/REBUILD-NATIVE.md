# Corresponding source and replacement of native components

The self-contained release is accompanied by `HandyLime-corresponding-source.zip`. Keep that archive available beside the binary archive when mirroring a release. It contains pinned source archives for Magick.NET, Magick.Native, ImageMagick, the dependency build system and its pinned component repositories. Exact references are in `licenses/source-manifest.json`; the library versions from upstream are in `licenses/native-libraries.md`. `scripts/Bundle-Sources.ps1` fetches these from their official repositories and writes checksums. The upstream source archives retain their own license files.

The native Windows codec DLL contains components under LGPL and other licenses. This is why only mentioning Magick.NET’s Apache license is insufficient. `Magick.NET-NOTICE.txt` contains the native notices, including applicable LGPL/GPL license text. Nothing in HandyLime’s license restricts modifying/replacing these components or reverse engineering for debugging modifications to them.

## Source correspondence

- Magick.NET 15.0.0: commit `79bb8700d7fe3a564f62926ab299fc2520cb2269`.
- Its `src/Magick.Native/Magick.Native.version` selects native release `2026.1010.1943`.
- Its generated `metadata/libraries.md` identifies native commit `4dcdad28530c14c8d35687fdeb1ecc8d39b01e30`.
- Native `src/ImageMagick/ImageMagick.commit` selects ImageMagick commit `211e5b975a0019a88dcef5f84c1193f46738d815`.
- That ImageMagick revision’s Windows dependency script selects `ImageMagick/Dependencies` release `2026.10.09.2225`. The manifest contains the exact component commits from that release’s `clone-dependencies.sh`, including build recipes and optional/non-Windows sources for completeness.

No modifications to these third-party libraries were made. The application’s image security policy is application code and is included in this repository.

## Rebuilding or replacing

1. Extract the corresponding-source archive. Each `.tar.gz` is a complete source snapshot of the named repository at the specified revision. Preserve its directory contents and license files when extracting it.
2. Read `Building.md` in Magick.Native and its `.github/workflows/main.yml`. For Windows, use its documented Visual Studio C++/Windows SDK toolchain and the Q8, x64, non-OpenMP configuration used here. The source bundle includes ImageMagick and dependency sources; follow the pinned Dependencies repository’s build instructions to build modified libraries instead of downloading its prebuilt library artifact.
3. Place the ImageMagick source at `Magick.Native/src/ImageMagick/ImageMagick` and use the pinned upstream build/configuration scripts. Build the matching native `Magick.Native-Q8-x64.dll`. Do not run an upstream “update to latest” script if reproducing this pinned version.
4. Build HandyLime from its public source as described in README. Magick.NET’s managed source and build instructions are also in the source bundle if you need to modify the interop layer.
5. Quit HandyLime and replace `Magick.Native-Q8-x64.dll` in the portable publish directory with the ABI-compatible rebuilt DLL. The app is not single-file packed, does not verify a vendor-specific DLL signature, and does not prohibit replacement. Rebuild the managed interop layer too if you change the native ABI.
6. Run the regression suite against your modified libraries and test representative images before using the result on important files.

Rebuilding the entire upstream native toolchain is more involved than building the C# application and was not performed during HandyLime’s initial validation. Upstream build tools, scripts and corresponding source are supplied; no claim of bit-for-bit reproduction across compiler versions is made. If updating Magick.NET, regenerate and review this source correspondence and its native notices before distributing another binary.
