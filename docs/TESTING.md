# Validation and release checklist

The automated suite uses synthetic local fixtures and actual file decoders. The console test runner exits nonzero on failure and writes JUnit XML plus a summary under `TestResults/`.

## Automated coverage

- PNG conversion to JPEG, WebP, AVIF, TIFF, BMP, GIF, ICO and PDF; output decoding and dimensions.
- Exact crop/resize dimensions, aspect preservation, photo operations, real redaction pixels, collage geometry and restricted SVG.
- Original-file hash preservation, output name collisions and pre-cancelled job cleanup.
- Unicode subtitle roundtrips, timing and malformed-input validation.
- PDF text extraction, ordered/duplicated/rotated pages, merge, split and all-page 300 DPI PNG/JPEG rendering.
- ZIP/TAR roundtrip, malicious traversal rejection, extraction cleanup, source-folder/output recursion protection and SHA-256.
- Numeric validation and common-operation selection for mixed files.
- GIF selected interval, output dimensions, changing decoded frames and progress events; exact-duration split markers; odd-dimension crop padding; decoded scrub previews and audio waveforms.
- Drawn annotation shapes and Unicode text, invalid geometry/colors, neutral photo adjustment pixel identity, tone/color changes, independent PDF page rotations and thumbnail rendering.
- Real FFmpeg media conversions, audio encoders, trim/crop/speed, silent video, splitting/joining, burned video cover, frames, audio bleep, loudness, channel conversion, waveform video and child-process cancellation.
- Real LibreOffice TXT → DOCX → PDF, CSV → XLSX → TSV, direct CSV ↔ TSV and HTML resource-folder output.
- Real Pandoc Markdown → DOCX/EPUB/HTML when installed; CI requires it.

These tests do not cover every codec, source-format combination or third-party document feature. Passing a representative test does not imply perfect fidelity for every file.

## Local validation record

The 0.3.1 packaging-only update was built with zero warnings/errors. Both Portable and Lite ZIPs were extracted, their SHA-256 checksums and runtime payload/configuration were checked, and each extracted executable successfully initialized WPF/the native image policy and exported its PNG/ICO brand assets. Portable measured about 90.2 MiB and Lite 20.1 MiB locally. Both retain the same native image library. Conversion behavior is unchanged from the 94-test run below; these startup checks are not a repeat of every conversion or GUI scenario. Workflow syntax is checked separately before distribution.

Release 0.3.0 was compiled on Windows x64 using .NET SDK 10.0.402 with warnings treated as errors (zero warnings/errors). The 20 October 2026 local run passed **94 tests, with zero failures and no skipped engine suites**, including FFmpeg, LibreOffice and Pandoc. The XML and summary are included in `validation/`. These are locally executed tests; a GitHub-hosted run has not yet occurred.

The runner initializes the image policy **before any Magick image is created**, matching application startup. GUI testing exposed that the old initialization order could hide policy failures for color canvases. The policy now permits the internal color canvas used for collage; restricted SVG uses its explicit built-in renderer, while application image reads select a decoder from the accepted extension. External delegates remain disabled.

The final local benchmark used a 6000 × 4000 (24 MP) synthetic source, decoded once into a preview capped at 1100 pixels per edge. Eight exposure previews had a median of approximately 112 ms on this machine; they wrote no intermediate files. This is a local diagnostic, not a portable timing requirement or a guarantee for all photo operations. Save still processes original resolution.

The earlier 0.2.0 self-contained WPF app was exercised through its Windows UI using synthetic local files:

- Workspace video → GIF: opened the visual editor, selected Save new copy, observed palette/conversion feedback, then Done / 100% and output buttons. The saved GIF decoded as 640 × 360 with an 8.00-second duration.
- Video crop: dragged a corner handle and moved the rectangle, saved, and independently probed a 426 × 240 output.
- Video split: moved the default marker from 4 to 2 seconds, moved the playhead to 6 seconds and added another marker. Save produced three independently probed 2-, 4- and 2-second clips.
- The rounded clickable wheel and rendered GIF sample preview were inspected. This is not evidence that every global Explorer drag scenario works.

Photo and PDF output behavior is covered by engine tests; complete end-to-end GUI coverage for every editor is still pending. Tests do not establish that all codecs or every arbitrary user file will work.

The 0.3.0 GUI pass additionally inspected the branded custom header, live light/dark theme switching, original logo, Lucide icon rendering and filtering the tool list. Collage was explicitly reproduced in the GUI under the real image policy before its fix, then reopened successfully with two different images, showing a 103 ms layout preview. Save created a 1328 × 672 PNG; the custom Activity window showed Done / 100% and output buttons. The Join window displayed two numbered cards side by side, selecting a card changed the preview, and Move earlier updated their order. Saving the reordered clips produced a probed 1280 × 720 MP4 lasting approximately 4.02 seconds. The media metadata window showed 71 properties for the first selected file; filtering for codec returned the expected H.264 entries. Both themes were inspected after correcting text contrast. Sequence playback was observed advancing from the first clip to the second and stopping at its end. This also exposed an end-of-file scrub issue; the final frame preview/export fallback is covered by the regression suite. Output-level regressions verify collage pixel order and geometry, cached preview agreement, image/media metadata removal and the first frame of joined clips in the requested order.

## Still required before calling a release stable

- [ ] Explorer → Shift drag → wheel → drop → verify source remains and output opens.
- [ ] Alt tools / Ctrl actions, Tab pagination, Escape, drop outside wedges, and release after showing the wheel.
- [ ] Native drop handlers with Ctrl/Shift held never signal Move for conversion.
- [x] Conventional workspace end-to-end GIF, visual crop and visual split with decoded output checks.
- [x] Clickable wheel appearance and rendered GIF output preview inspected.
- [ ] Explorer drag with repeated same modifier while the mouse stays held; verify the original drag continues.
- [ ] Every image/PDF editor end-to-end interaction, manual invalid field recovery and accessibility.
- [ ] Tray close/reopen/quit, pause/resume, cancel then queue a new job.
- [ ] Shelf drag into Explorer and a normal application; original remains.
- [ ] Copy/move errors, permissions, collisions, folders and cross-volume behavior.
- [ ] Two monitors, negative monitor coordinates, mixed 100/150/200% DPI, and taskbars on different edges.
- [ ] Screen readers, high-contrast mode, keyboard-only workflow and Windows display scaling.
- [ ] First launch on a clean supported Windows x64 machine without a preinstalled .NET runtime.
- [ ] Full GitHub-hosted workflow after pushing to the user’s repository.
- [ ] Engine installation button on a clean machine; cancellation/elevation and failure feedback. Existing installed engines were used in local tests.
- [ ] HEIC/HEIF decoding using representative camera/phone files; complex Office documents, unusual codecs and corrupted/large files.

## Commands

```powershell
dotnet restore HandyLime.slnx --locked-mode --configfile NuGet.Config
dotnet build HandyLime.slnx -c Release --no-restore -warnaserror
dotnet run --project tests/HandyLime.Tests -c Release --no-build -- --require-engines
```

If you want fixtures for inspection, append `--keep-fixtures`. Test files are generated in a uniquely named temporary directory. `--report=PATH` changes the report directory. Test-generated fixtures contain no user files.

GitHub Actions performs the same build and integration test sequence before packaging. Hosting a workflow file is not evidence that the hosted workflow has already run; the initial hosted run occurs after you push the project.

For focused development checks, `--only=metadata --report=TestResults/metadata` selects matching tests plus required media fixtures. Release validation uses the full suite without `--only`.
