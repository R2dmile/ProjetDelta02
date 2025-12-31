# ProjetDelta – Publication rapide

## Prérequis
- Windows avec .NET SDK 8+ installé (`dotnet --list-sdks`).
- Workloads MAUI Windows installés (si besoin de l’UI MAUI) :
  - `dotnet workload install maui-windows`

## Publier les deux builds (console + MAUI)
Depuis la racine du repo :
```powershell
powershell -ExecutionPolicy Bypass -File .\publish-all.ps1
```
Résultat :
- `publish\` : jeu console en exécutable unique (win-x64, self-contained).
- `publish-maui\` : UI MAUI/Blazor pour Windows (net8.0-windows).
- `publish-maui-msix\` : package MSIX self-contained (WinAppSDK inclus, pas d’installation supplémentaire côté utilisateur). Installer le .msix puis lancer l’appli.
- `bundle\` : dossier prêt à distribuer contenant l’exe console + un sous-dossier `ui-maui` avec l’UI MAUI et une copie du MSIX. Lancer `ProjetDelta.exe` (option 3 démarre l’UI si présente).
  - La partie MAUI en zip peut encore demander le runtime sur certaines machines; utiliser le MSIX pour éviter toute install manuelle.

## Publier manuellement (au cas où)
- Console seul :
  ```powershell
  dotnet publish ProjetDelta.csproj -c Release -r win-x64 `
      -p:PublishSingleFile=true -p:SelfContained=true `
      -p:IncludeAllContentForSelfExtract=true -o publish
  ```
- MAUI/Blazor seul (Windows) :
  ```powershell
  dotnet publish ProjetDelta.BlazorHybrid/ProjetDelta.BlazorHybrid.csproj `
      -c Release -f net8.0-windows10.0.19041.0 -r win-x64 `
      -p:WindowsPackageType=None -o publish-maui
  ```

## Notes
- Les données JSON sont intégrées dans l’exécutable console (single-file).
- L’UI MAUI ne peut pas être single-file ; le dossier `publish-maui` (ou `bundle/ui-maui`) est à distribuer tel quel.
- Pour un install simple : zipe `bundle\`, distribue-le, puis lance `ProjetDelta.exe` à la racine. Pour être 100% autonome, installe d’abord le MSIX (`install-maui-ui.ps1`) puis l’option 3 fonctionnera sans runtime externe.
