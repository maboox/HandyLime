# Contributing

Use Windows and the .NET 10 SDK. Read README and docs/FORMATS.md before adding routes.

1. Keep changes focused. Add a real conversion test that checks output content/dimensions/timing, not only file existence.
2. Add operations to the shared catalog so the workspace and wheel agree.
3. Never overwrite a source during a conversion. Use OutputTransaction and honor cancellation before committing.
4. Build child process arguments with ArgumentList. Do not introduce shell-command interpolation of filenames.
5. Avoid network behavior in converters. Any new external resources or telemetry require an explicit product decision and documentation.
6. Update format/engine documentation and third-party notices when dependencies change. PDFsharp is intentionally pinned to 6.2.4 for its MIT license.
7. Run `dotnet build -c Release` and `dotnet run --project tests/HandyLime.Tests -c Release`. With all engines installed, add `-- --require-engines`.
8. Include manual checks for actual Explorer drag/drop, cancellation, mixed DPI and keyboard operation when editing the wheel or UI.

Do not commit private documents, generated binaries, credentials, user settings, temporary files or logs containing personal paths. Use minimal synthetic fixtures. Your contributions are provided under the repository’s MIT license unless a file explicitly identifies a compatible third-party license.
