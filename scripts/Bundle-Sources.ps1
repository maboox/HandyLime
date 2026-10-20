param([string]$Destination = (Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts'))
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
New-Item -ItemType Directory -Force -Path $Destination | Out-Null
$cache = Join-Path $Destination 'corresponding-source'
New-Item -ItemType Directory -Force -Path $cache | Out-Null
$manifest = Get-Content (Join-Path $root 'licenses/source-manifest.json') -Raw | ConvertFrom-Json
foreach ($item in $manifest) {
    if ($item.repository -notmatch '^(dlemstra|ImageMagick)/[A-Za-z0-9.-]+$' -or $item.commit -notmatch '^[A-Za-z0-9.]+$') { throw 'Invalid source manifest entry' }
    $archive = Join-Path $cache ($item.name + '-' + $item.commit + '.tar.gz')
    if (-not (Test-Path -LiteralPath $archive)) {
        Write-Host "Fetching corresponding source: $($item.repository)"
        Invoke-WebRequest -Uri "https://codeload.github.com/$($item.repository)/tar.gz/$($item.commit)" -OutFile ($archive + '.partial') -MaximumRetryCount 3 -RetryIntervalSec 3
        Move-Item -LiteralPath ($archive + '.partial') -Destination $archive
    }
    if ((Get-Item -LiteralPath $archive).Length -lt 100) { throw "Invalid archive: $archive" }
}
Copy-Item (Join-Path $root 'licenses/source-manifest.json') -Destination $cache
Copy-Item (Join-Path $root 'docs/REBUILD-NATIVE.md') -Destination $cache
Get-ChildItem -LiteralPath $cache -Filter '*.tar.gz' | Sort-Object Name | ForEach-Object { ((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $_.Name) } | Set-Content -Encoding utf8 (Join-Path $cache 'SHA256SUMS.txt')
$zip = Join-Path $Destination 'HandyLime-corresponding-source.zip'
if (-not (Test-Path -LiteralPath $zip)) { [System.IO.Compression.ZipFile]::CreateFromDirectory($cache,$zip,[System.IO.Compression.CompressionLevel]::NoCompression,$false) }
$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  HandyLime-corresponding-source.zip" | Set-Content -Encoding utf8 "$zip.sha256"
Write-Host "Corresponding sources: $zip"
