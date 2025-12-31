$ErrorActionPreference = "Stop"

# Recherche du MSIX dans publish-maui-msix ou bundle
$candidates = @(
    Join-Path $PSScriptRoot "publish-maui-msix"
    Join-Path $PSScriptRoot "bundle"
)

$msix = $null
foreach ($c in $candidates) {
    if (Test-Path $c) {
        $found = Get-ChildItem $c -Filter *.msix -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) { $msix = $found.FullName; break }
    }
}

if (-not $msix) {
    Write-Host "MSIX introuvable. Lance d'abord publish-all.ps1" -ForegroundColor Red
    exit 1
}

Write-Host "Installation du package $msix ..." -ForegroundColor Cyan
Add-AppxPackage -ForceApplicationShutdown -ForceUpdateFromAnyVersion -Path $msix
Write-Host "Installation terminee. Lance l'application MAUI via le menu démarrer ou option 3 du launcher." -ForegroundColor Green
