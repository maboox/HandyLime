param([string]$Destination = (Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'HandyLime-source.zip'))
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$target = [System.IO.Path]::GetFullPath($Destination)
if (Test-Path -LiteralPath $target) { throw "Output already exists: $target" }
$excluded = @('bin', 'obj', 'artifacts', 'TestResults', '.git', '.vs', '.idea', 'tools')
$files = Get-ChildItem -LiteralPath $root -Recurse -File -Force | Where-Object {
    $relative = [System.IO.Path]::GetRelativePath($root, $_.FullName)
    $parts = $relative.Split([char[]]@('\', '/'))
    -not ($parts | Where-Object { $_ -in $excluded }) -and $_.Extension -notin @('.user', '.suo') -and $_.FullName -ne $target
} | Sort-Object FullName
$archive = [System.IO.Compression.ZipFile]::Open($target, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in $files) {
        $relative = [System.IO.Path]::GetRelativePath($root, $file.FullName).Replace('\', '/')
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, ('HandyLime/' + $relative), [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally { $archive.Dispose() }
$hash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([System.IO.Path]::GetFileName($target))" | Set-Content -Encoding utf8 "$target.sha256"
Write-Output "Packed $($files.Count) source files: $target"
