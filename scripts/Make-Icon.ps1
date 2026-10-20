$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$assets = Join-Path $root 'src/HandyLime.App/Assets'
Push-Location $root
try {
    dotnet run --project src/HandyLime.App -c Release -- --export-brand $assets
    if ($LASTEXITCODE -ne 0) { throw 'Brand asset generation failed' }
    Copy-Item -LiteralPath (Join-Path $assets 'handylime.png') -Destination (Join-Path $root 'docs/assets/handylime.png')
    Write-Output 'Generated the original HandyLime PNG and seven-size ICO. Rebuild to embed the updated icon.'
} finally { Pop-Location }
