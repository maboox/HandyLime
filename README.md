<div align="center">

<img src="docs/assets/handylime.png" width="92" alt="HandyLime lime mark" />

# HandyLime

### New format. Same flow.

A local file-conversion workspace and drag wheel for Windows.

**Windows x64 · .NET 10 · Open source · No accounts · No conversion uploads**

[Get started](#get-started) · [Build on GitHub](#build-on-github-actions) · [Formats](docs/FORMATS.md) · [Engine setup](docs/ENGINES.md) · [فارسی](docs/README.fa.md)

</div>

Drop a file, tap **Shift**, and choose a format around your pointer. Use **Alt** for tools or **Ctrl** for file actions. Prefer a regular window? Drop files into the workspace, choose an operation, use the format/tool filter when needed, and watch the queue in Activity.

HandyLime is an independent implementation inspired by the workflow of [Tangerine for Mac](https://tangerineformac.com/). It is **not an official port, a licensed Tangerine product, or a promise of feature parity**. No Tangerine code, branding, screenshots, icons or proprietary assets are included. The original lime brand mark is MIT licensed; interface icons come from [Lucide](https://lucide.dev/) under its ISC/MIT notices.

**Status: 0.3.0 preview.** The HandyLime update adds a lime identity, light/dark/system themes, custom window headers, cached photo previews, clearer metadata inspection and visual clip ordering. Real conversion engines and tests are included. Review [tested behavior and remaining manual checks](docs/TESTING.md) and [known limitations](#known-limitations) before distributing it as a stable release.

## What you can do

- Convert images; resize, crop with draggable handles, rotate, mirror, adjust light/color, draw annotations, add backgrounds, cover an area, strip metadata and make collages.
- Convert audio/video; trim, crop, change speed, normalize audio, change channels, bleep audio, export frames, split and join videos, or create an audio waveform video.
- Merge, split and rearrange PDFs. Export every page to PNG/JPEG at 300 DPI or extract selectable text.
- Convert office documents, slides and spreadsheets through LibreOffice; Markdown/EPUB/HTML through Pandoc.
- Convert SRT/VTT subtitles, including Unicode text. Export plain transcripts.
- Create ZIP/TAR archives, safely extract ZIP/TAR/TAR.GZ/GZIP and generate SHA-256 checksums.
- Float files on a small shelf, copy files or paths, copy/move to a folder, reveal in Explorer, or open a terminal in the containing directory.
- Process multiple files sequentially with a separate progress window, cancellation, readable errors, Open result / Show in folder buttons and unique output names.

Some operations require optional engines. The exact routes and limitations live in [FORMATS.md](docs/FORMATS.md); a format name is not a promise that every codec, encryption method or embedded object is supported.

## Get started

1. Download the **HandyLime-windows-x64** artifact from a successful Actions run (or the portable ZIP from a release).
2. Extract the complete archive into a writable folder. Keep its files together.
3. Run `HandyLime.exe`. No administrator access or .NET installation is needed for the self-contained release.
4. Images, PDF tools, archives and subtitles work immediately. For audio/video or office documents, use **Settings → Conversion engines → Install with winget**, or install the [optional engines](docs/ENGINES.md) manually. Video requires both FFmpeg and FFprobe. Missing engines open a visible explanation and Settings.
5. Add files using drag and drop, **Add files**, **Ctrl+O**, or **Ctrl+V** after copying files in Explorer.

The default destination is beside the source. Conversions and edits create a new file such as `photo-jpg.jpg`, then `photo-jpg (1).jpg` on a repeat. They do not overwrite the source. **Move to… is a separate, explicitly confirmed file action and changes the original location.**

The unsigned preview may display a Windows reputation warning. Builds are not code-signed; organizations that require signed binaries should build and sign with their own certificate. No certificate or signing secret is bundled.

## Drag wheel

Start dragging a real file from Explorer, move a little, and then tap:

| Key while dragging | Wheel |
|---|---|
| Shift | Formats common to the dragged files |
| Alt | Tools common to the dragged files |
| Ctrl | File actions |
| Tab | Next page when there are more than eight options |
| Same Shift / Alt / Ctrl key again | Hide that wheel without sending Escape to the source drag |
| Escape | Cancel (Windows can also cancel the underlying drag) |

Release the modifier while keeping the mouse down; the wheel stays until you drop or release the drag. Move onto a rounded segment and release to apply. The center cancels. Tap the same modifier again to hide the wheel, or a different modifier to switch its category. Tools and video-to-GIF open a visual editor before processing. The **Open wheel** button opens a clickable wheel for files already in the workspace. The wheel uses Lucide icons, a file thumbnail/size center and a lime selection state.

## Visual editing and conversion feedback

Choose **Settings → Appearance → Light / Dark / System**. The selection is saved; System follows Windows app appearance changes. HandyLime windows use a matching custom header with move, resize, minimize, maximize/restore and close controls. Windows file/folder pickers remain operating-system dialogs.

- **Video → GIF:** choose a time range using the two timeline handles, scrub through frames, set 1–30 fps and 64–1920 px width, and preview up to five seconds before saving. GIF has no audio. Long/high-resolution GIFs can be large.
- **Trim and split:** a filmstrip, playhead, playback controls and range handles replace seconds-only dialogs. For Split, move the playhead, click **Split here**, then drag numbered markers to refine cuts. Equal-length splitting is also available.
- **Crop and cover:** resize the eight handles or drag inside the rectangle. Crop offers Free, 1:1, 16:9, 9:16, 4:3 and 3:4 presets plus pixel fields. Video tools keep the timeline visible. H.264 output pads odd dimensions by one pixel when necessary.
- **Photo adjustments:** live output preview, comparison with the original, resettable sliders for exposure, brightness, contrast, highlights, shadows, whites, blacks, gamma, warmth, tint, saturation, vibrance, hue and sharpening.
- **Responsive photo preview:** images are decoded once into a bounded in-memory preview. Slider changes are coalesced, dragging light/rotation controls uses a smaller preview, and releasing the pointer refines it. No temporary encoded file is written for each movement. The full original is read when saving. Preview timing is shown below the image; this is measured locally, not a performance guarantee for every machine. Compression previews show image geometry/colors, not exact codec artifacts or final file size.
- **Collage:** select 2–100 different images before opening the tool. Change columns, cell size, spacing/background and source order while seeing the layout. Full-resolution output uses the chosen cell size; the preview is scaled to fit.
- **Join videos:** numbered thumbnail cards show the saved left-to-right order. Select a card to scrub/play that clip, move it earlier/later, or use Play sequence to review the sequence. Export normalizes clips to 1280 × 720 / 30 fps / stereo.
- **Metadata:** the metadata tools display searchable, selectable property/value tables. Save clean copy removes removable tags/profiles from a new output. Technical properties needed to decode a file remain. For a batch, the table describes the first file.
- **Annotations:** draw arrows, lines, rectangles, ellipses, text and freehand strokes; change color/size, select and move marks, undo, delete or reset. Saved marks are burned into the new image.
- **Audio and PDFs:** audio tools show a waveform and time selection; PDF tools show page thumbnails with move, rotate, duplicate and remove controls. Merge and collage inputs can be reordered.

Press **Save new copy** to queue the operation. A separate Activity window stays available even when the main window is hidden, showing the current stage, progress when the engine provides it, cancellation, errors and output buttons. Palette generation and some native operations can show an indeterminate stage before measurable progress arrives. Closing this window hides it; it does not cancel a job. Use its Cancel button to stop a job.

Short media previews use a temporary encoded sample. The final output uses the original file and the full selected interval. The preview is representative rather than a promise of byte-identical output. For a batch of separate edits, the editor previews the first file and applies the settings to each input; files that cannot accept those settings report errors individually.

The wheel sees file paths only after Windows supplies a file-drop payload. It does not inspect Explorer’s selected files or record keystrokes. It samples only mouse/modifier/control-key state. Windows does not offer a universal “file drag began” notification, so a Shift/Alt/Ctrl modified non-file drag can briefly show an empty wheel. Use Settings or the tray menu to pause it.

Closing the main window keeps HandyLime in the tray by default. Double-click the tray icon to reopen it; use **Quit** to exit. This behavior is configurable. It does not add itself to startup. To launch manually in the tray, use `HandyLime.exe --tray`.

## Build on GitHub Actions

You do not need to edit the workflow with your username or repository name.

1. Create an empty GitHub repository.
2. Extract the source ZIP. Upload/push the **contents of the `HandyLime` folder** to the repository root, including the hidden `.github` directory. Do not upload the ZIP itself as the repository’s only file.
3. Open **Actions → Windows build and tests**. A push starts it automatically. For a fork, enable Actions first if GitHub asks.
4. The workflow installs test engines, restores locked dependencies, builds with warnings treated as errors, runs real conversion tests, and publishes a self-contained x64 app.
5. When the run is green, open **Artifacts → HandyLime-windows-x64**. The artifact contains the portable ZIP and its SHA-256 checksum. GitHub may require you to sign in to download build artifacts.

To start a build manually, use **Run workflow** on the repository’s default branch. No repository secrets are required. Pull requests get read-only repository access. Pushing a `v*` tag also creates a **draft release** after the tests pass; inspect its assets and notes before publishing it.

Example after creating a repository (replace the URL):

```powershell
git init
git add .
git commit -m "Initial HandyLime preview"
git branch -M main
git remote add origin https://github.com/YOUR-NAME/YOUR-REPOSITORY.git
git push -u origin main

# Optional draft release:
git tag v0.3.0
git push origin v0.3.0
```

The workflow targets `windows-latest`. Downloading third-party test tools depends on upstream package availability. A failed dependency installation fails the job rather than silently skipping the integration suite.

## Build locally

Install the **.NET 10 SDK** and use Windows 11 x64 (Windows 10 build 19041+ is the API floor, but supported OS servicing depends on Microsoft’s lifecycle). The release architecture is x64; there is no native ARM64 or x86 release in this preview.

```powershell
dotnet restore HandyLime.slnx --locked-mode --configfile NuGet.Config
dotnet build HandyLime.slnx -c Release --no-restore
dotnet run --project tests/HandyLime.Tests -c Release --no-build
dotnet run --project src/HandyLime.App -c Release --no-build
```

The test runner is a standalone executable with failure exit codes and JUnit XML output in `TestResults/`; use `dotnet run`, not `dotnet test`. To require every optional engine, append `-- --require-engines`. Without that flag, missing engine suites are explicitly reported as skipped.

```powershell
./scripts/Publish.ps1
```

`Publish.ps1` creates a fresh staging folder and a ZIP under `artifacts/`. It refuses to overwrite an existing final ZIP. Source dependencies are pinned in project and lock files. The SDK uses the current .NET 10 feature band so releases receive runtime security updates; published binaries are deterministic for the same SDK and inputs, not guaranteed byte-identical across SDK updates.

## Privacy and file safety

- No accounts, telemetry, analytics, update pings, upload service or conversion server.
- Engine installation, dependency restore and explicitly opening documentation websites need the network. Converting local files does not use an application upload path.
- Files can contain external references. Pandoc runs with `--sandbox`; FFmpeg uses a local protocol allowlist; SVG external resources are rejected; LibreOffice uses a separate profile with macros and link updates disabled. These are safeguards, **not a complete security sandbox**.
- Each job writes to a uniquely named `.handylime-*` work folder in the destination, then moves a successful result into place. Failed/cancelled work folders are removed where the filesystem permits it. A crash or forced process termination can leave a work folder behind.
- Archive extraction rejects traversal, absolute/device names, alternate data streams, links and special TAR entries. The limits are 10,000 entries and 4 GiB of expanded data.
- Image processing has resource limits. Native image/PDF operations cannot always be interrupted mid-call; cancellation is observed at the next boundary and before committing output. External processes are terminated as a process tree.
- Settings are in `%LOCALAPPDATA%\HandyLime\settings.json`. Activity history stays in memory. Error messages can contain local paths; redact them before posting an issue.

## Known limitations

- This is not all 275 Tangerine conversion routes or every advanced editor. See [RESEARCH.md](docs/RESEARCH.md) for the feature comparison.
- No Apple Pages/RTFD, AirDrop, RAR, OCR, QR reader, PDF-to-editable-Office reconstruction, exact-byte-size compression, automatic face/text detection or arbitrary metadata editing.
- HEIC/HEIF input depends on the bundled decoder; **HEIC output is intentionally not offered**. Animated/multipage image input uses the first frame/page. Q8 output does not preserve HDR/16-bit image data. JPEG backgrounds are flattened to white.
- PDF text export preserves text content, not original layout. Scanned PDFs without selectable text fail with a useful message. Merging/reordering PDFs may discard bookmarks, signatures, forms or other document-level features.
- CSV/TSV exports use the first sheet. Locale, encodings and complex Office formatting can affect fidelity. Markdown/EPUB routes use Pandoc and do not promise pixel-perfect typography.
- Media operations process the first video/audio stream unless noted. Metadata removal does not guarantee anonymity or erase visible/watermarked information. Compression is quality-based; a smaller output is not guaranteed.
- The cover tools burn one solid rectangle; video cover applies during one time interval. The timeline supports single-source trim/split/cover operations, not a multitrack video editor. No automated tracking or multi-region video cover.
- Photo adjustments use this project's own 8-bit algorithms. They do not reproduce Tangerine's color science. PDF thumbnail editing currently supports up to 300 pages per input document. Frame-step buttons advance by 1/30 second; variable-frame-rate files are not indexed frame by frame.
- Dragging from an elevated app into a normal app is restricted by Windows. Run both at the same normal privilege level. Virtual cloud/Outlook attachments without real file paths are unsupported; save them locally first.
- Global drag behavior, multiple monitors/mixed DPI, different Windows themes and assistive technologies still require broader hands-on validation. The conventional workspace is the fallback. The wheel is pointer-first; the workspace provides standard keyboard-focusable controls.
- Copy/move actions are sequential and can partially complete before a later item fails; completed items are not rolled back. Directory moves across volumes can fail. Existing destinations are never overwritten.

## Project layout

```text
src/HandyLime.Core/   Catalog, converters, job orchestration and validation
src/HandyLime.App/    Native WPF workspace, wheel, tray and file actions
tests/HandyLime.Tests/  Executable regression/integration suite
scripts/             Packaging, icon generation and optional engine setup
docs/                Formats, setup, research, testing and Persian guide
licenses/            Upstream license and attribution texts
.github/             Build/release workflow and contribution templates
```

## Contributing and license

Read [CONTRIBUTING.md](CONTRIBUTING.md), [SECURITY.md](SECURITY.md), and [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). Contributions should include real input/output tests for new conversion routes. Do not copy another app’s proprietary assets or imply endorsement.

HandyLime’s own source, original icon and interface are [MIT licensed](LICENSE). Third-party components retain their own licenses. FFmpeg, LibreOffice and Pandoc are invoked as separately installed tools and are not bundled. Magick.NET includes native dependencies with additional notices and source obligations; release packaging includes the corresponding source material described in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
