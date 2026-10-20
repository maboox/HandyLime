# Supported operations

This file describes HandyLime 0.3.0, not the full capability set of its engines. The app exposes only the routes in `Catalog.cs`. Batch selections show the intersection of compatible operations. All outputs are new copies, except the separately confirmed Move file action.

## Conversions

| Input | Output | Requirement / qualifications |
|---|---|---|
| JPG/JPEG, PNG, WebP, HEIC/HEIF, TIFF, AVIF, BMP, GIF, ICO, restricted SVG | PNG, JPEG, WebP, AVIF, TIFF, BMP, GIF, ICO, PDF | Bundled Magick.NET; first image/frame only, Q8 color. HEIC input codec dependent. ICO <=256 px. SVG cannot reference external resources. No SVG or HEIC export. |
| MP3, M4A, WAV, FLAC, OGG, Opus, AIFF, WMA, AAC | MP3, M4A, WAV, FLAC, OGG, Opus, AIFF, WMA | FFmpeg + FFprobe; first audio track |
| MP4, MOV, MKV, WebM, AVI, WMV, M4V | MP4, MOV, MKV, WebM, AVI, WMV, GIF, MP3, WAV | FFmpeg + FFprobe; GIF has visual interval, 1–30 fps and 64–1920 px width controls; defaults 15 fps / up to 640 px. Audio export requires an audio track |
| PDF | PNG, JPEG, TXT, MD | Native Windows PDF rasterizer at 300 DPI for all pages; PdfPig text extraction. MD is extracted text, not recovered headings/tables. |
| DOC/DOCX, ODT, RTF, TXT | PDF, DOCX, ODT, RTF, TXT, HTML | LibreOffice. HTML export is a folder with `input.html` and any image resources. |
| Markdown, EPUB, HTML | DOCX, HTML, Markdown, TXT, EPUB | Pandoc with sandbox enabled; resource limitations may apply |
| PPT/PPTX, ODP | PDF, PPTX, ODP, PNG, JPEG | LibreOffice; image exports go through PDF and produce a page folder |
| XLS/XLSX, ODS, CSV, TSV | XLSX, ODS, CSV, TSV, PDF | LibreOffice; CSV/TSV use UTF-8 and the first sheet; comma/tab separators respectively |
| SRT, VTT | SRT, VTT, TXT | Built in; basic cue timing/text, no CSS/region/layout preservation |

The same input/output extension is omitted from the format menu. JPEG/JPG and TIF/TIFF aliases may still appear as equivalent encodings. Unknown extensions still have archive and checksum tools. Unsupported, encrypted, malformed or unusually complex files can fail with an engine error in Activity.

## File tools

| Category | Tools | Details |
|---|---|---|
| Images | Compress, resize, crop, rotate, mirror, adjust, annotate, background, cover area, collage, remove metadata | Compression produces WebP. Resize fits an aspect-preserving box. Crop/cover have draggable handles. Adjust has live light/color/detail sliders. Annotation supports movable arrows, lines, rectangles, ellipses, text and pen strokes. Cover burns a black rectangle into PNG. Collage is a fixed-cell grid with preserved aspect ratio. |
| Video | Compress, trim, crop, speed, frame, split, join, cover area, remove metadata | Filmstrip/playhead and visual time ranges; split at movable markers or equal intervals. Mostly H.264/AAC MP4. Odd crop dimensions pad up to even dimensions. Speed supports 0.25×–4× and silent video. Split re-encodes intervals. Join normalizes to 1280×720 / 30 fps / stereo; silent clips get a silent audio track. Metadata removal copies audio/video streams into MKV. |
| Audio | Trim, normalize, channels, bleep, visualizer, remove metadata | Most tools output PCM WAV; visualizer outputs MP4; metadata removal outputs FLAC. Loudness is single-pass −16 LUFS / −1.5 dBTP. Bleep replaces one interval with a 1 kHz tone. |
| PDF | Merge, arrange pages, split | Thumbnail editor supports moving, duplicating, removing and rotating individual pages by 0/90/180/270 degrees. Merge file order can be changed before saving. The core also accepts numeric page ranges and duplicates. |
| Archives | Create ZIP, create TAR, extract | ZIP/TAR/TAR.GZ/TGZ/GZIP extraction. No encrypted archives, RAR or symlinks. No GZIP creation in this version. |
| All regular files | SHA-256 | Writes a `.sha256` text file |

Numeric parameters use a dot as the decimal separator. Coordinate values are pixels; image coordinates refer to the auto-oriented image. Video coordinates refer to the decoded frame; files carrying unusual rotation metadata need careful checking. Time values are seconds. Always review covered/redacted output before sharing sensitive material.

## File actions

Float shelf, clipboard copy, copy to directory, confirmed move to directory, copy paths, reveal first item in Explorer, and open PowerShell in its containing directory. There is no Windows AirDrop equivalent included. Float uses Copy drag effects so dragging from the shelf does not remove originals.

## Limits

- Main workspace: up to 1,000 items.
- Collage: up to 100 images, 100 megapixels total output.
- PDF render: up to 1,000 pages and 100 megapixels per rendered page.
- Visual PDF editor: up to 300 pages per input document; loaded thumbnails use memory.
- Annotations: up to 500 marks; color and source-coordinate geometry are validated.
- Video join: 2–100 clips. Split: up to 1,000 clips.
- Archives: at most 10,000 entries; extraction expands at most 4 GiB per job.
- Images: source file <=512 MB and native memory/disk/dimension limits.
- A serial queue avoids running several expensive media jobs simultaneously.

Native processing can take time even after Cancel is clicked. Cancellation prevents the unfinished output from being committed; already finished jobs remain available.
