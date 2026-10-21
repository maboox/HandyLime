<div align="center">

<img src="docs/assets/handylime.png" width="92" alt="HandyLime" />

# HandyLime

### New format. Same flow.

Convert and edit files without leaving your workflow.

**Windows x64 · Local processing · Light & dark themes · Open source**

[![Windows build and tests](https://github.com/maboox/HandyLime/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/maboox/HandyLime/actions/workflows/build.yml?query=branch%3Amain)
[![Latest release](https://img.shields.io/github/v/release/maboox/HandyLime)](https://github.com/maboox/HandyLime/releases/latest)
[![MIT license](https://img.shields.io/github/license/maboox/HandyLime)](LICENSE)

[Download](https://github.com/maboox/HandyLime/releases/latest) · [User guide](docs/USAGE.md) · [Supported formats](docs/FORMATS.md) · [فارسی](docs/README.fa.md)

</div>

Drag a file, tap **Shift**, and choose a format around your pointer. Use **Alt** for tools or **Ctrl** for file actions. Or add files to the main window and follow progress in Activity.

## Download

Get the latest version from **[Releases](https://github.com/maboox/HandyLime/releases/latest)**. Choose one app ZIP:

| Edition | Best for | .NET installation |
|---|---|---|
| **[Portable](https://github.com/maboox/HandyLime/releases/latest/download/HandyLime-windows-x64.zip)** | Extract and run on a Windows x64 PC | Included |
| **[Lite](https://github.com/maboox/HandyLime/releases/latest/download/HandyLime-windows-x64-lite.zip)** | Smaller downloads, especially if .NET is already installed | Install **[.NET Desktop Runtime 10 for Windows x64](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)** first |

Both editions provide the same features. Lite uses the shared runtime; it does not reduce conversion quality. The plain .NET Runtime and ASP.NET Runtime do not replace the Desktop Runtime.

The **corresponding-source** download contains third-party library sources for redistribution and development. **You do not need it to run the app.** `.sha256` files are optional integrity checks.

## Get started

1. Extract the entire app ZIP and run **HandyLime.exe**. Keep its companion files together.
2. Add files by dragging them into the window, using **Add files**, or pressing **Ctrl+O**. **Ctrl+V** also accepts files copied in Explorer.
3. Choose a format or tool, review the preview when available, and save a new copy. Activity shows progress, cancellation, errors and output buttons.
4. For video/audio and Office documents, open **Settings → Conversion engines** to install or locate the optional tools. [Engine setup](docs/ENGINES.md) explains the choices.

Use Windows 11 x64 or a supported Windows 10 edition/build. The app is currently distributed as an unsigned preview; Windows may display a publisher/reputation prompt.

## Features

- **Images:** convert, resize, crop, rotate, adjust light and color, annotate, add backgrounds, cover areas, inspect/remove metadata and create collages.
- **Video & audio:** adjust video light/color/detail with live frame previews and edited playback samples; convert, make GIFs, trim, split, crop, reorder/join clips, change speed, export frames and inspect/remove metadata. Requires FFmpeg and FFprobe.
- **PDFs & documents:** merge, split, reorder and rotate PDF pages; export pages or text. Office conversions use LibreOffice; Markdown/EPUB routes use Pandoc.
- **File tools:** create/extract ZIP and TAR archives, calculate checksums, copy paths and keep files on a floating shelf.
- **Workspace:** batch queue, visual previews, saved destinations, Light/Dark/System appearance and a tray menu.

See [supported formats](docs/FORMATS.md) for exact routes, requirements and limits.

## Drag wheel

Start dragging a file from Explorer, move a little, then tap a key:

| Key | Opens |
|---|---|
| **Shift** | Convert formats |
| **Alt** | Editing tools |
| **Ctrl** | File actions |
| **Tab** | Next page of options |
| **Same modifier again** | Hide the wheel while keeping the drag |

Release the modifier, keep dragging, then drop on the option. The center cancels. The **Open wheel** button also works with files already in the workspace. [Full user guide](docs/USAGE.md).

## Good to know

Processing happens locally. HandyLime does not upload conversion inputs or require an account. Installing optional engines requires internet access.

Conversions create new files beside the originals by default; repeated names receive a numeric suffix. The separate **Move to…** action changes the source location after confirmation.

Closing the main window keeps the app in the tray by default. Double-click the tray icon to reopen it, or choose **Quit** to exit. Change this behavior in Settings.

Want it ready when Windows starts? Enable **Settings → Start HandyLime with Windows**. It starts minimized in the tray after you sign in; the option is off by default. Keep the app in a permanent folder. After moving it, use **Use this copy for Windows startup** in Settings to update the location, or turn the option off to remove it.

This is a preview release. Complex documents, unusual codecs and large files can have format-specific limitations; review outputs before deleting originals. [Detailed limitations](docs/USAGE.md#known-limitations).

## Help & contributing

[Report a bug](https://github.com/maboox/HandyLime/issues) · [Build & release guide](docs/BUILDING.md) · [Contributing](CONTRIBUTING.md) · [Security](SECURITY.md)

HandyLime is independent software inspired by the drag-wheel workflow of Tangerine for Mac. It is not affiliated with Tangerine.

HandyLime's source and original lime mark are [MIT licensed](LICENSE). Interface icons are from [Lucide](https://lucide.dev/). Other libraries retain their licenses; see [third-party notices](THIRD-PARTY-NOTICES.md).

## فارسی

**HandyLime ابزاری متن‌باز برای تبدیل و ویرایش فایل‌ها در ویندوز است.** عکس، ویدیو، صدا، PDF و اسناد را با چرخ انتخاب کنار نشانگر یا پنجرهٔ اصلی پردازش کنید. ویرایشگرهای بصری، صف تبدیل و تم روشن و تیره دارد. پردازش فایل‌ها روی کامپیوتر خودتان انجام می‌شود و حساب کاربری لازم نیست.

- **[دانلود نسخهٔ آماده](https://github.com/maboox/HandyLime/releases/latest):** فقط یکی از دو فایل برنامه را بگیرید. **Portable** دات‌نت را همراه دارد؛ **Lite** کم‌حجم‌تر است و به **[.NET Desktop Runtime 10 x64](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)** نیاز دارد. امکانات هر دو یکسان است.
- ZIP را کامل استخراج و **HandyLime.exe** را اجرا کنید. فایل **corresponding-source** برای اجرای برنامه لازم نیست.
- فایل را بکشید و **Shift** را برای تبدیل، **Alt** را برای ویرایش یا **Ctrl** را برای عملیات فایل بزنید. زدن دوبارهٔ همان کلید چرخ را می‌بندد.
- تبدیل ویدیو و صدا به FFmpeg/FFprobe و برخی تبدیل‌های سند به LibreOffice یا Pandoc نیاز دارد؛ از تنظیمات برنامه آن‌ها را معرفی کنید.
- این نسخه فعلاً پیش‌نمایش و بدون امضای تجاری است. خروجی‌های مهم را پیش از حذف فایل اصلی بررسی کنید.

**[راهنمای کامل فارسی](docs/README.fa.md)** · [گزارش مشکل یا پیشنهاد](https://github.com/maboox/HandyLime/issues/new/choose) · [گزارش خصوصی مشکل امنیتی](https://github.com/maboox/HandyLime/security/advisories/new)
