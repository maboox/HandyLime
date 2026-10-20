# Third-party notices

HandyLime’s own source and original icon/interface are MIT licensed. Libraries and external tools keep their own licenses. No Tangerine code, screenshots, logo, icon, brand assets or extracted resources are shipped. The name Tangerine appears only to identify the independently researched inspiration; there is no affiliation or endorsement.

## Bundled libraries

| Component | Pinned version | License and attribution |
|---|---|---|
| Lucide SVG interface icons | 1.59.0 | ISC; Feather-derived icons retain MIT attribution. Complete upstream text in `licenses/Lucide-LICENSE.txt`; vendored selection and source URL in `licenses/lucide-manifest.json`. |
| Magick.NET / Magick.NET.Core | 15.0.0 | Apache-2.0; Copyright Dirk Lemstra. Full license in `licenses/Magick.NET-Apache-2.0.txt`. |
| Native ImageMagick and codec dependencies | As listed in `licenses/native-libraries.md` | ImageMagick license and component-specific licenses, including LGPL. Complete native attribution/license text in `licenses/Magick.NET-NOTICE.txt`. Corresponding source is packaged alongside binary releases; see `docs/REBUILD-NATIVE.md`. |
| PDFsharp | 6.2.4 | MIT; Copyright empira. `licenses/PDFsharp-MIT.txt`. This exact version is intentionally used; later licenses differ. |
| PdfPig | 0.1.16 | Apache-2.0; UglyToad/PdfPig contributors. `licenses/PdfPig-Apache-2.0.txt`, including upstream notices. |
| .NET runtime / Windows Desktop runtime and Microsoft package dependencies | .NET 10; resolved versions in lock files | MIT and third-party notices; `licenses/dotnet-MIT.txt` and `licenses/dotnet-THIRD-PARTY-NOTICES.txt`. Self-contained publishing selects the servicing runtime provided by the SDK. |
| System.Security.Cryptography.Pkcs | 10.0.13 | MIT; Microsoft. Explicit patched version overrides PDFsharp’s older transitive dependency. |

The full transitive package list and integrity hashes are in `packages.lock.json` for each project. Windows APIs and installed Segoe UI fonts are used through the operating system; font files are not redistributed. Lucide SVGs are embedded resources and require no runtime download. `scripts/Make-Icon.ps1` regenerates the original MIT-licensed HandyLime fruit mark as PNG and a seven-size ICO using its WPF vector source in `Brand.cs`.

## Separately installed, not redistributed

- **FFmpeg/FFprobe:** LGPL/GPL depending on the selected build and codecs. See [official licensing](https://ffmpeg.org/legal.html). HandyLime invokes it as an external program and ships neither its executables nor its codec binaries.
- **LibreOffice:** MPL-2.0 and other applicable component licenses. See [LibreOffice licensing](https://www.libreoffice.org/about-us/licenses/). It is installed separately.
- **Pandoc:** GPL-2.0-or-later. See [Pandoc source/license](https://github.com/jgm/pandoc). It is installed separately.

The setup script delegates installation to Windows Package Manager and leaves package agreements visible. The optional in-app **Install with winget** button uses winget's agreement-acceptance flags, as explained next to the button and in the engine guide. CI’s third-party engine installations remain on the ephemeral runner and are not copied to the release.

## Redistribution

Preserve this file, the license directory and the corresponding-source companion archive when redistributing a binary release. Publish the complete HandyLime source for the matching release as well. License obligations and patent availability for particular media formats can vary with the selected engine/build and jurisdiction; the project does not grant third-party patent rights beyond the relevant licenses. A fork that changes dependencies or bundles optional engines must reassess its own redistribution requirements.
