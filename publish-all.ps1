$ErrorActionPreference = "Stop"

Write-Host "Publishing console build (single-file, self-contained, win-x64)..." -ForegroundColor Cyan
dotnet publish .\ProjetDelta.csproj -c Release -r win-x64 `
    -p:PublishSingleFile=true `
    -p:SelfContained=true `
    -p:IncludeAllContentForSelfExtract=true `
    -o .\publish

Write-Host "Publishing MAUI/Blazor build (Windows)..." -ForegroundColor Cyan
dotnet publish .\ProjetDelta.BlazorHybrid\ProjetDelta.BlazorHybrid.csproj `
    -c Release `
    -f net8.0-windows10.0.19041.0 `
    -r win-x64 `
    -p:WindowsPackageType=None `
    -p:SelfContained=true `
    -p:WindowsAppSDKSelfContained=true `
    -p:WindowsAppSDKGenerateAppxPackageOnBuild=false `
    -p:WindowsAppSDKGenerateTestArtifacts=false `
    -p:WindowsAppSDKVersion=1.4.230822000 `
    -o .\publish-maui

Write-Host "Publishing MAUI/Blazor MSIX (runtime inclus)..." -ForegroundColor Cyan
dotnet publish .\ProjetDelta.BlazorHybrid\ProjetDelta.BlazorHybrid.csproj `
    -c Release `
    -f net8.0-windows10.0.19041.0 `
    -r win-x64 `
    -p:WindowsPackageType=MSIX `
    -p:SelfContained=true `
    -p:WindowsAppSDKSelfContained=true `
    -p:WindowsAppSDKVersion=1.5.0 `
    -p:AppxPackageSigningEnabled=false `
    -p:GenerateAppInstallerFile=false `
    -o .\publish-maui-msix

# Regroupe dans un dossier unique (bundle) pour distribution
$bundle = Join-Path $PWD "bundle"
Write-Host "Assemblage du bundle (console + UI MAUI) dans $bundle" -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $bundle | Out-Null
Copy-Item .\publish\* $bundle -Recurse -Force
$uiBundle = Join-Path $bundle "ui-maui"
New-Item -ItemType Directory -Force -Path $uiBundle | Out-Null
Copy-Item .\publish-maui\* $uiBundle -Recurse -Force
$msix = Get-ChildItem .\publish-maui-msix\*.msix -ErrorAction SilentlyContinue | Select-Object -First 1
if ($msix) { Copy-Item $msix.FullName $bundle -Force }

Write-Host "Done. Console in ./publish, MAUI in ./publish-maui, MSIX in ./publish-maui-msix, bundle complet dans ./bundle." -ForegroundColor Green
