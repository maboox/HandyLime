param([ValidateSet('All', 'Portable', 'Lite')][string]$Edition = 'All')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$artifactRoot = Join-Path $root 'artifacts'
$editions = if ($Edition -eq 'All') { @('Portable', 'Lite') } else { @($Edition) }
foreach ($item in $editions) {
    $selfContained = ($item -eq 'Portable')
    $name = if ($selfContained) { 'HandyLime-windows-x64' } else { 'HandyLime-windows-x64-lite' }
    $zip = Join-Path $artifactRoot ($name + '.zip')
    $actual = (Get-FileHash -Algorithm SHA256 -LiteralPath $zip).Hash.ToLowerInvariant()
    $expected = ((Get-Content -LiteralPath "$zip.sha256" -Raw).Trim() -split '\s+')[0]
    if ($actual -ne $expected) { throw "$item checksum mismatch" }
    $check = Join-Path $artifactRoot ('package-check-' + [guid]::NewGuid().ToString('N'))
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zip, $check)
    $app = Join-Path $check 'HandyLime'
    foreach ($required in @('HandyLime.exe', 'HandyLime.dll', 'HandyLime.Core.dll', 'HandyLime.runtimeconfig.json', 'Magick.Native-Q8-x64.dll', 'LICENSE', 'THIRD-PARTY-NOTICES.md', 'START-HERE.txt', 'licenses/Magick.NET-NOTICE.txt')) {
        if (-not (Test-Path -LiteralPath (Join-Path $app $required))) { throw "$item missing $required" }
    }
    $config = (Get-Content -LiteralPath (Join-Path $app 'HandyLime.runtimeconfig.json') -Raw | ConvertFrom-Json).runtimeOptions
    if ((Test-Path -LiteralPath (Join-Path $app 'coreclr.dll')) -ne $selfContained) { throw "$item runtime payload is incorrect" }
    $frameworks = if ($selfContained) { $config.includedFrameworks } else { $config.frameworks }
    if ('Microsoft.WindowsDesktop.App' -notin $frameworks.name) { throw "$item does not declare Windows Desktop runtime" }
    $brandOutput = Join-Path $check 'startup check'
    # Initialize WPF and the native image policy, then render the original brand
    # from the extracted package, rather than the development build directory.
    $process = Start-Process -FilePath (Join-Path $app 'HandyLime.exe') -ArgumentList @('--export-brand', ('"' + $brandOutput + '"')) -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(30000)) { $process.Kill(); throw "$item startup check timed out" }
    if ($process.ExitCode -ne 0) { throw "$item startup check failed: $($process.ExitCode)" }
    foreach ($asset in @('handylime.ico', 'handylime.png')) {
        $assetPath = Join-Path $brandOutput $asset
        if (-not (Test-Path -LiteralPath $assetPath) -or (Get-Item -LiteralPath $assetPath).Length -lt 100) { throw "$item did not render $asset" }
    }
    Write-Output "$item package: checksum, extracted files, runtime mode and WPF/native startup passed."
}
