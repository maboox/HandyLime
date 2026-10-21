# Security

HandyLime processes local file formats using native and managed libraries. It is not a sandbox for adversarial documents. Do not disable OS security features to use it.

No upload or telemetry service is implemented. Image delegates are restricted; FFmpeg gets local protocol restrictions; Pandoc uses sandbox mode; LibreOffice gets an isolated profile with macros/link updates disabled. Native engine bugs remain possible. Update dependencies and external tools promptly.

Supported version: the [latest release](https://github.com/maboox/HandyLime/releases/latest) from this repository. Use **[Report a vulnerability](https://github.com/maboox/HandyLime/security/advisories/new)** to contact the maintainer privately. Do not post exploit details, credentials or documents containing private information in public issues. Ordinary bugs and feature requests belong in [Issues](https://github.com/maboox/HandyLime/issues/new/choose).

Reports should include version, OS, engine version, minimal non-private reproduction and expected/actual behavior. Review path traversal, archive bombs, command-line arguments, external-resource access, temporary-output handling and clipboard behavior when changing relevant code.

## گزارش امنیتی به فارسی

مشکل‌های امنیتی را از **[فرم گزارش خصوصی آسیب‌پذیری](https://github.com/maboox/HandyLime/security/advisories/new)** ارسال کنید. جزئیات سوءاستفاده، کلید، رمز، فایل شخصی و مسیرهای خصوصی را در Issue عمومی قرار ندهید. نسخهٔ برنامه، نسخهٔ ویندوز و موتور تبدیل، مراحل بازتولید و یک نمونهٔ ساختگی و غیرخصوصی به بررسی کمک می‌کند.

آخرین نسخهٔ منتشرشده پشتیبانی می‌شود. HandyLime فایل‌ها را محلی پردازش می‌کند، اما محیطی ایزوله برای بازکردن فایل‌های مخرب نیست. برنامه و موتورهای جانبی را به‌روز نگه دارید.
