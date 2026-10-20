# Security

HandyLime processes local file formats using native and managed libraries. It is not a sandbox for adversarial documents. Do not disable OS security features to use it.

No upload or telemetry service is implemented. Image delegates are restricted; FFmpeg gets local protocol restrictions; Pandoc uses sandbox mode; LibreOffice gets an isolated profile with macros/link updates disabled. Native engine bugs remain possible. Update dependencies and external tools promptly.

Supported version: the latest preview/release from this repository. Before publishing a fork, enable GitHub **Settings → Code security → Private vulnerability reporting**. Report security vulnerabilities through the repository’s private **Security → Report a vulnerability** flow when enabled. Do not post exploit documents containing private information in public issues. If private reporting is unavailable, ask the maintainer for a private reporting channel without disclosing the exploit publicly.

Reports should include version, OS, engine version, minimal non-private reproduction and expected/actual behavior. Review path traversal, archive bombs, command-line arguments, external-resource access, temporary-output handling and clipboard behavior when changing relevant code.
