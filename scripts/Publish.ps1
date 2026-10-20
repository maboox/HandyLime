param(
    [ValidateSet('win-x64')][string]$Runtime = 'win-x64',
    [ValidateSet('All', 'Portable', 'Lite')][string]$Edition = 'All'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    $artifactRoot = Join-Path $root 'artifacts'
    New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null
    $editions = if ($Edition -eq 'All') { @('Portable', 'Lite') } else { @($Edition) }
    foreach ($item in $editions) {
        $name = if ($item -eq 'Lite') { 'HandyLime-windows-x64-lite' } else { 'HandyLime-windows-x64' }
        $zip = Join-Path $artifactRoot ($name + '.zip')
        if (Test-Path -LiteralPath $zip) { throw "Output already exists: $zip. Move it elsewhere before publishing again." }
    }
    foreach ($item in $editions) {
        $selfContained = ($item -eq 'Portable')
        $name = if ($selfContained) { 'HandyLime-windows-x64' } else { 'HandyLime-windows-x64-lite' }
        $stage = Join-Path $artifactRoot ('stage-' + [guid]::NewGuid().ToString('N'))
        $output = Join-Path $stage 'HandyLime'
        $deployment = if ($selfContained) { 'true' } else { 'false' }
        dotnet publish src/HandyLime.App/HandyLime.App.csproj -c Release -r $Runtime --self-contained $deployment -p:PublishSingleFile=false -p:PublishTrimmed=false -p:DebugSymbols=false -p:DebugType=None -p:RestoreLockedMode=true -o $output
        if ($LASTEXITCODE -ne 0) { throw "$item publish failed" }
        Copy-Item LICENSE,THIRD-PARTY-NOTICES.md,README.md -Destination $output
        Copy-Item docs,licenses -Destination $output -Recurse
        Copy-Item scripts/Install-Engines.ps1 -Destination $output
        $requirement = if ($selfContained) {
            'Portable edition: the .NET runtime is included. No separate .NET installation is required.'
        } else {
            'Lite edition: install Microsoft .NET Desktop Runtime 10 for Windows x64 first (the latest 10.0 servicing version). The plain .NET Runtime is not sufficient. Download: https://dotnet.microsoft.com/en-us/download/dotnet/10.0'
        }
        @"
HandyLime - $item edition

$requirement

Extract this entire folder and run HandyLime.exe. Keep all files together.
Both editions have the same features. Optional video/office engines are configured in Settings.
The corresponding-source ZIP is for redistribution and library development; it is not needed to run HandyLime.

Downloads and help: https://github.com/maboox/HandyLime/releases/latest
"@ | Set-Content -LiteralPath (Join-Path $output 'START-HERE.txt') -Encoding utf8
        $zip = Join-Path $artifactRoot ($name + '.zip')
        [System.IO.Compression.ZipFile]::CreateFromDirectory($stage,$zip,[System.IO.Compression.CompressionLevel]::SmallestSize,$false)
        $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $zip).Hash.ToLowerInvariant()
        "$hash  $name.zip" | Set-Content -Encoding utf8 -LiteralPath "$zip.sha256"
        Write-Output ("Built {0}: {1:N1} MiB" -f $zip, ((Get-Item -LiteralPath $zip).Length / 1MB))
    }
    & (Join-Path $PSScriptRoot 'Bundle-Sources.ps1') -Destination $artifactRoot
} finally { Pop-Location }
