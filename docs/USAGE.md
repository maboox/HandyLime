# Using HandyLime

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
- **Video adjustments:** choose **Adjust video** for the same 14 controls. Sliders update a cached paused frame; scrub the timeline to review other positions and use **Show original** to compare. **Play original** plays the source, while **Play edited** renders up to five seconds from the playhead using your current settings. **Back to editor** or Escape returns to the controls. Save applies the settings to the whole video at its original resolution and writes a new H.264 MP4 with AAC audio when present. Odd dimensions are padded by one pixel for codec compatibility. Color processing is 8-bit SDR; it is not an HDR grading pipeline. The LUT interpolation, re-encoding and video sharpening can cause small differences from a still-frame preview; use edited playback to review the rendered result.
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
You can opt in to automatic sign-in startup with **Settings → Start HandyLime with Windows**. It starts in the tray and applies only to your Windows account. The checkbox is off by default; disabling it removes HandyLime's registration. If you move the portable folder, open the new copy and use **Use this copy for Windows startup**. Lite still needs its Desktop Runtime and video/audio features still need their optional engines.

## Known limitations

- Available conversion routes and engine requirements are listed in [Supported formats](FORMATS.md).
- No Apple Pages/RTFD, AirDrop, RAR, OCR, QR reader, PDF-to-editable-Office reconstruction, exact-byte-size compression, automatic face/text detection or arbitrary metadata editing.
- HEIC/HEIF input depends on the bundled decoder; **HEIC output is intentionally not offered**. Animated/multipage image input uses the first frame/page. Q8 output does not preserve HDR/16-bit image data. JPEG backgrounds are flattened to white.
- PDF text export preserves text content, not original layout. Scanned PDFs without selectable text fail with a useful message. Merging/reordering PDFs may discard bookmarks, signatures, forms or other document-level features.
- CSV/TSV exports use the first sheet. Locale, encodings and complex Office formatting can affect fidelity. Markdown/EPUB routes use Pandoc and do not promise pixel-perfect typography.
- Media operations process the first video/audio stream unless noted. Metadata removal does not guarantee anonymity or erase visible/watermarked information. Compression is quality-based; a smaller output is not guaranteed.
- The cover tools burn one solid rectangle; video cover applies during one time interval. The timeline supports single-source trim/split/cover operations, not a multitrack video editor. No automated tracking or multi-region video cover.
- Photo adjustments use 8-bit processing. PDF thumbnail editing currently supports up to 300 pages per input document. Frame-step buttons advance by 1/30 second; variable-frame-rate files are not indexed frame by frame.
- Dragging from an elevated app into a normal app is restricted by Windows. Run both at the same normal privilege level. Virtual cloud/Outlook attachments without real file paths are unsupported; save them locally first.
- Global drag behavior, multiple monitors/mixed DPI, different Windows themes and assistive technologies still require broader hands-on validation. The conventional workspace is the fallback. The wheel is pointer-first; the workspace provides standard keyboard-focusable controls.
- Copy/move actions are sequential and can partially complete before a later item fails; completed items are not rolled back. Directory moves across volumes can fail. Existing destinations are never overwritten.


