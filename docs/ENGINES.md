# Optional conversion engines

Images, PDF tools/rasterization/text extraction, subtitles, ZIP/TAR and checksums are included. The three engines below are separately installed and are **not redistributed in HandyLime’s binary archive**. Their own license and installation prompts apply.

## Windows Package Manager

In the app, **Settings → Conversion engines → Install with winget** installs a missing engine and displays its result in the Activity window. The button downloads the publisher's package and passes winget's package/source agreement acceptance flags; use manual installation below if you prefer to review those prompts interactively. Windows App Installer must provide `winget.exe`. A cancelled installer can leave an incomplete external installation; repair it through its normal installer. HandyLime does not bundle these tools.

Open a normal PowerShell window and run only the packages you need:

```powershell
# Audio and video (includes ffprobe)
winget install --id Gyan.FFmpeg --exact --source winget

# Word, spreadsheets, slides and office PDF export
winget install --id TheDocumentFoundation.LibreOffice --exact --source winget

# Markdown, HTML and EPUB
winget install --id JohnMacFarlane.Pandoc --exact --source winget
```

Alternatively run `scripts/Install-Engines.ps1` from the source checkout, or `Install-Engines.ps1` from a portable release. `-Set Media`, `-Set Office` or `-Set Documents` installs just one category. The script does not silently accept package agreements. Installers may request elevation; HandyLime itself does not.

HandyLime checks the current user/machine PATH and common winget locations as well as its process PATH, so a fresh engine installation is usually found without restarting. In Settings, every required engine should say **Ready**. If it does not, restart or click **Locate .exe…** and choose the appropriate executable. Choosing an executable authorizes HandyLime to run that program for conversions; choose trusted vendor distributions.

## Manual installation

- [FFmpeg official download page](https://ffmpeg.org/download.html) links to Windows builds. Use a build with `libx264`, `libvpx-vp9`, `libmp3lame`, `libopus` and `libvorbis`. Put `ffmpeg.exe` and `ffprobe.exe` together. Both are required. Gyan’s full build is covered by the integration tests.
- [LibreOffice](https://www.libreoffice.org/download/download-libreoffice/): the default location is `C:\Program Files\LibreOffice\program\soffice.exe`. HandyLime uses its own temporary profile, so it does not attach to an open personal LibreOffice session. Third-party Office layouts and embedded objects may render differently.
- [Pandoc](https://pandoc.org/installing.html): locate `pandoc.exe`. No TeX installation is needed for the routes offered here; PDF output for office files uses LibreOffice.

For a portable personal installation, the app also searches:

```text
HandyLime/tools/ffmpeg/ffmpeg.exe
HandyLime/tools/ffmpeg/ffprobe.exe
HandyLime/tools/ffmpeg/bin/ffmpeg.exe
HandyLime/tools/ffmpeg/bin/ffprobe.exe
HandyLime/tools/libreoffice/soffice.exe
HandyLime/tools/pandoc/pandoc.exe
```

A manually configured path takes priority, followed by these locations, known installation directories and PATH. FFprobe is also discovered beside the selected FFmpeg executable. A green Ready badge checks existence, not every codec; a missing encoder is reported by the actual conversion.

## CI and testing

The GitHub workflow uses Chocolatey to install FFmpeg, LibreOffice and Pandoc into the ephemeral runner. These installations are used for tests only. They are not copied into the portable release.

The test executable accepts `HANDYLIME_FFMPEG`, `HANDYLIME_LIBREOFFICE` and `HANDYLIME_PANDOC` environment variables containing executable paths. This allows testing a portable engine without modifying the machine PATH.

## Troubleshooting

| Problem | What to check |
|---|---|
| Engine says not installed | Restart the app, then explicitly locate its `.exe` |
| FFprobe not installed | Keep FFprobe beside FFmpeg from the same release |
| Unknown encoder | Use a full FFmpeg distribution with the codecs listed above |
| No LibreOffice output | Confirm the input is supported and not password-protected; inspect Activity |
| File cannot be written | Choose a writable output directory in Settings |
| Empty PDF text | The PDF may be scanned; OCR is not included |
| Archive refused | Check expansion limits, symlinks, invalid names or traversal entries |
| App starts but tray icon is hidden | Expand Windows’ tray overflow area |

Download and engine setup require internet access. Converting files afterwards is local. Keep third-party engines updated independently of HandyLime.
