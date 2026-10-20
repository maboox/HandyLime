param([ValidateSet('win-x64')][string]$Runtime = 'win-x64')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    $artifactRoot = Join-Path $root 'artifacts'
    New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null
    $stage = Join-Path $artifactRoot ('stage-' + [guid]::NewGuid().ToString('N'))
    $output = Join-Path $stage 'HandyLime'
    dotnet publish src/HandyLime.App/HandyLime.App.csproj -c Release -r $Runtime --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -p:RestoreLockedMode=true -o $output
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
    Copy-Item LICENSE,THIRD-PARTY-NOTICES.md,README.md -Destination $output
    Copy-Item docs,licenses -Destination $output -Recurse
    Copy-Item scripts/Install-Engines.ps1 -Destination $output
    & (Join-Path $PSScriptRoot 'Bundle-Sources.ps1') -Destination $artifactRoot
    $zip = Join-Path $artifactRoot 'HandyLime-windows-x64.zip'
    if (Test-Path $zip) { throw "Output already exists: $zip. Move it elsewhere before publishing again." }
    [System.IO.Compression.ZipFile]::CreateFromDirectory($stage,$zip,[System.IO.Compression.CompressionLevel]::Optimal,$false)
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $zip).Hash.ToLowerInvariant()
    "$hash  HandyLime-windows-x64.zip" | Set-Content -Encoding utf8 -Path "$zip.sha256"
    Write-Output "Built $zip"
} finally { Pop-Location }
