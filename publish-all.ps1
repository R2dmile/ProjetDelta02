$ErrorActionPreference = "Stop"

# Dossier racine du bundle unique
$bundle = Join-Path $PWD "bundle"
$bundleArtefact = Join-Path $bundle "Artefact"

New-Item -ItemType Directory -Force -Path $bundle, $bundleArtefact | Out-Null

Write-Host "Publishing console build (single-file, self-contained, win-x64)..." -ForegroundColor Cyan
dotnet publish .\ProjetDelta.csproj -c Release -r win-x64 `
    -p:PublishSingleFile=true `
    -p:SelfContained=true `
    -p:IncludeAllContentForSelfExtract=true `
    -o $bundleArtefact

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
    -o (Join-Path $bundleArtefact "ProjetDelta.BlazorHybrid")

Write-Host "Done. Build console et MAUI publiées dans ./bundle/Artefact." -ForegroundColor Green
