param([ValidateSet('All','Media','Office','Documents')][string]$Set = 'All')
$ErrorActionPreference = 'Stop'
if (-not (Get-Command winget -ErrorAction SilentlyContinue)) { throw 'Install Windows App Installer (winget) from Microsoft Store first. See docs/ENGINES.md for manual downloads.' }
$packages = switch ($Set) {
    'Media' { @('Gyan.FFmpeg') }
    'Office' { @('TheDocumentFoundation.LibreOffice') }
    'Documents' { @('JohnMacFarlane.Pandoc') }
    'All' { @('Gyan.FFmpeg','TheDocumentFoundation.LibreOffice','JohnMacFarlane.Pandoc') }
}
Write-Host 'This installs optional third-party conversion engines using Windows Package Manager.'
Write-Host ('Packages: ' + ($packages -join ', '))
Write-Host 'Review the publisher and package agreements shown by winget. Internet access is needed for installation only.'
foreach ($package in $packages) {
    winget install --id $package --exact --source winget
    if ($LASTEXITCODE -ne 0) { throw "winget did not finish installing $package (exit $LASTEXITCODE)." }
}
Write-Host 'Restart HandyLime. If an engine is not detected, use Settings > Locate .exe.'
