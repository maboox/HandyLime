# Changelog

## 0.4.0 — 2026-10-21

- Added Adjust video to the workspace and drag wheel, with the same 14 light/color/detail controls as the photo editor, original comparison, individual/all resets, a cached frame preview, timeline scrubbing and an edited playback sample.
- Added full-resolution H.264 MP4 export with audio when present. The color LUT samples the photo transform; spatial sharpening uses FFmpeg. Temporary LUTs are isolated and removed after success, failure or cancellation.
- Added optional Start HandyLime with Windows in Settings. It registers only the current user's HandyLime entry, starts in the tray after sign-in and can update the registered location after moving the portable folder. Disabled by default.
- Avoided flashing the main window on tray startup and suppressed duplicate-instance messages for automatic tray launches.
- Added an explicit Back to editor button to rendered previews and visible playback-failure feedback. The sidebar version now follows the built assembly.
- Added eight regression tests for edited-video color agreement, dimensions/duration/audio, silent clips, unusual paths, cancellation and isolated Windows startup registration.

## 0.3.1 — 2026-10-20

- Separated app downloads from the native library source companion in Actions and public Releases.
- Added a smaller Lite edition using the installed .NET Desktop Runtime, with the same app features as Portable.
- Added checksum, extracted-payload and startup verification for both release editions. Kept native dependencies replaceable and preserved license/source availability.
- Added public release publishing from a version tag or the manual workflow's publish option, with English/Persian download guidance.
- Simplified the public README and moved build/release details to the development guide.

## 0.3.0 — 2026-10-20 · HandyLime

- Renamed the app to HandyLime; introduced an original vector lime mark, multi-size application icon and lime palette.
- Added persistent Light, Dark and System appearance modes and custom headers on app, editor, activity, shelf and message windows. UI icons use the vendored Lucide 1.59.0 SVG set and upstream license notices.
- Replaced repeated full-size image decoding, encoding and temporary files with a bounded decoded preview session. Slider changes are coalesced; save still uses original inputs. Tone work is parallelized and avoids unnecessary gamma/power operations.
- Added collage selection validation and cached layout previews; tested output order, mixed image dimensions and single-image failure feedback.
- Added searchable/copyable image/media metadata tables and a clearly named Save clean copy action.
- Added numbered video thumbnail cards with per-clip selection and scrubbing, earlier/later ordering and sequence playback; aligned transport button sizes and refreshed PDF merge previews after reordering.
- Fixed policy initialization for collage color canvases, selected explicit safe image decoders, preserved original EXIF properties during metadata inspection, handled end-of-video frame preview/export, and fixed automatic sequence playback startup. Released editor caches on close and coalesced Activity refreshes for batches.
- Expanded real-engine regressions with image/media metadata removal, collage pixel order and a 24-megapixel source preview benchmark.

## 0.2.0 — 2026-10-20

- Persistent job feedback for wheel and workspace conversions: stages, percentages when available, per-job cancellation, visible errors and output actions. Missing FFmpeg/FFprobe no longer leaves the user without an explanation.
- Visual GIF interval/fps/width controls and rendered sample preview; bounded palette processing; improved installed-engine discovery and optional in-app winget installation.
- Filmstrip/waveform timelines, draggable trim handles and split markers, playback/frame controls, visual crop/cover handles and aspect presets.
- Live photo light/color adjustments, original comparison and reset controls; arrow, line, shape, text and freehand annotation with move/undo/delete.
- PDF thumbnails, per-page rotation, reorder/duplicate/remove, and reorderable merge/collage inputs.
- Rounded petal wheel with original line icons, file thumbnail/size center and same-modifier toggle without injecting Escape into a drag.
- Expanded real-engine regressions, documented GUI checks, and updated English/Persian guides. This remains an independent preview with the limitations documented in README.

## 0.1.0 — 2026-10-19

Initial preview: native Windows workspace, radial drag target, tray, floating shelf, batch queue and cancellation; image/media/document/PDF/subtitle/archive operations; optional engine discovery; safe output naming; Windows x64 self-contained packaging; regression/integration tests; English and Persian documentation; upstream notices and corresponding-source packaging.

This preview intentionally documents missing Tangerine functionality and unverified GUI scenarios. It is not presented as a feature-complete official port.
