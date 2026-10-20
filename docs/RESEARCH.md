# Reference research and implementation decisions

Research date: 18–19 October 2026; implementation comparison updated 20 October 2026 using the user's supplied wheel/photo/annotation/video-crop references. These are observations of public material, not claims that the proprietary Mac binary was inspected or tested.

## Sources inspected

1. [Tangerine official website](https://tangerineformac.com/): drag-triggered format/tool/action wheels, local processing, format categories and advertised aggregate counts. Its interactive website explicitly says operations are simulated.
2. [Public tool catalog](https://www.tangerineformac.com/tools): descriptions and screenshots of individual editing windows. The public detail page is more useful for individual behavior than the aggregate tool count.
3. [Press kit](https://www.tangerineformac.com/press): the cursor-centered workflow, saving new copies and historical product/version information.
4. [App Store listing](https://apps.apple.com/us/app/tangerine-file-converter/id6813034065?mt=12): version 2.0.1 at review time; recent actions/floating-file functionality and additional document categories. Older shortcut wording differs from the current website.
5. [User-supplied LinkedIn post](https://www.linkedin.com/posts/jung-hoon-son_found-this-wonderful-tangerine-app-on-my-activity-7510425254276616192-a-eC/): public text emphasizes the desktop workflow and elegant interface. The embedded video reported “Unable to play media”; it was not treated as verified footage.
6. [Privacy policy](https://tangerineformac.com/privacy): consulted for public privacy claims. HandyLime has its own implementation and data handling described in README; no claim is made that these are identical.

We did not download/decompile the Mac app, bypass its trial, copy its source, reuse its visual assets, or imply affiliation. Website screenshots were reviewed as references and are not bundled as product assets.

## Mapping the experience to Windows

| Reference behavior | HandyLime implementation |
|---|---|
| Conversion at the pointer during a drag | Topmost WPF radial drop target; Shift selects formats |
| Relevant editing tools | Alt tool wheel using the same operation catalog as the main window |
| Action wheel | Ctrl for copy, move, path, reveal, terminal and a floating shelf |
| Multiple compatible files | Intersection of supported operations; sequential job queue |
| New outputs beside originals | Unique names and a staging/commit process; configurable output directory |
| Local processing | Bundled image/PDF libraries and local optional engines |
| Menu bar residence | Windows system tray |
| Rich visual tool editors | Draggable crop/cover, filmstrip and waveform timelines, split markers, live photo controls, drawn annotations and PDF thumbnails |
| AirDrop / Pages | No equivalent advertised; no Apple-only implementation |

## Deliberate differences and gaps

HandyLime does not claim the reference’s 275 paths or every advanced tool. It provides a practical subset with tested core behavior. Missing areas include exact-size compression, QR scanning, editable PDF reconstruction, OCR, automatic redaction detection/tracking, image-in-image annotations, arbitrary metadata editing, RAR and Apple-specific formats.

The visual editors are independently authored and not pixel-identical copies. Photo controls use our own tone/color algorithm with live output previews. Annotation supports arrows, lines, rectangles, ellipses, text and freehand strokes; it does not reproduce every reference annotation tool. Media editing uses one source timeline, not multitrack editing. A solid rectangle is used for privacy cover tools; it is burned into exported pixels. Audio normalization is single-pass. Images use 8-bit channels and the first frame. Tool behavior and limits are documented next to controls and in FORMATS.md.

## Technology and source references

- [WPF and .NET 10](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/whats-new/): native Windows windowing, shell integration and standard controls.
- [Magick.NET](https://github.com/dlemstra/Magick.NET): image codecs and transformations. Actual tests showed HEIC encoding unavailable in the bundled build, so the UI does not offer it.
- [PDFsharp 6.2.4](https://www.nuget.org/packages/PDFsharp/6.2.4): pinned to its MIT-licensed version; later releases must not be adopted without a license review.
- [PdfPig](https://github.com/UglyToad/PdfPig): selectable text extraction.
- [Windows PDF API](https://learn.microsoft.com/en-us/uwp/api/windows.data.pdf.pdfdocument): rasterization without Ghostscript.
- [FFmpeg documentation](https://ffmpeg.org/documentation.html), [license](https://ffmpeg.org/legal.html): media processing as a separately installed executable.
- [LibreOffice command-line parameters](https://help.libreoffice.org/latest/en-US/text/shared/guide/start_parameters.html): headless conversion with an isolated profile.
- [Pandoc manual](https://pandoc.org/MANUAL.html): document routes and sandbox mode.

The project's lime identity, fruit mark, wording and layouts are independently authored. Interface icons are vendored from Lucide 1.59.0 with the upstream ISC/MIT license text and source manifest. Fonts are supplied by Windows. HandyLime 0.3.0 adds light/dark/system palettes and custom window frames.
