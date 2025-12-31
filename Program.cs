using System;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using ProjetDelta.UI;
using ProjetDelta.Data.DataLoader;

try
{
    // Make sure relative file loads (Data/DataStorage/*) work both in dev and when published.
    var originalDir = Directory.GetCurrentDirectory();
    Directory.SetCurrentDirectory(AppContext.BaseDirectory);
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    // Chargement profil (permet de forcer le dev meta via arg)
    var tempUi = new SpectreUI();
    var profile = ProfileLoader.Load();
    var devArgs = Environment.GetCommandLineArgs();
    bool devMeta = devArgs.Any(a => a.Equals("--dev-meta", StringComparison.OrdinalIgnoreCase) ||
                                    a.Equals("--unlock-meta", StringComparison.OrdinalIgnoreCase));
    bool forceMaui = devArgs.Any(a => a.Equals("--force-maui", StringComparison.OrdinalIgnoreCase));
    bool hasWinAppRuntime = HasWinAppRuntime15();
    string FindBlazorCsproj()
    {
        var candidates = new List<string>();

        string CombineRoot(string root) =>
            Path.GetFullPath(Path.Combine(root, "ProjetDelta.BlazorHybrid", "ProjetDelta.BlazorHybrid.csproj"));

        // Racine initiale
        candidates.Add(CombineRoot(originalDir));
        // Dossier courant (peut etre bin/Debug/...)
        candidates.Add(CombineRoot(Directory.GetCurrentDirectory()));
        // BaseDirectory du binaire
        candidates.Add(CombineRoot(AppContext.BaseDirectory));
        // remonter de bin/Debug/net8.0
        var up3 = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        candidates.Add(CombineRoot(up3));

        return candidates.FirstOrDefault(File.Exists) ?? "";
    }
    string FindBlazorExe()
    {
        var baseDir = AppContext.BaseDirectory;
        var execDir = Path.GetDirectoryName(Environment.ProcessPath ?? "") ?? baseDir;
        var roots = new[]
        {
            baseDir,
            execDir,
            Path.GetFullPath(Path.Combine(baseDir, "..")),      // au cas où extraction dans sous-dossier
            Path.GetFullPath(Path.Combine(execDir, ".."))
        }.Distinct();

        var candidates = new List<string>();
        foreach (var root in roots)
        {
            if (string.IsNullOrWhiteSpace(root)) continue;
            candidates.Add(Path.Combine(root, "ProjetDelta.BlazorHybrid.exe"));
            candidates.Add(Path.Combine(root, "ui-maui", "ProjetDelta.BlazorHybrid.exe"));
            candidates.Add(Path.Combine(root, "publish-maui", "ProjetDelta.BlazorHybrid.exe"));
            candidates.Add(Path.Combine(root, "ProjetDelta.BlazorHybrid", "ProjetDelta.BlazorHybrid.exe"));
        }
        return candidates.FirstOrDefault(File.Exists) ?? "";
    }
    var csprojPath = FindBlazorCsproj();
    var mauiExePath = FindBlazorExe();

    if (devMeta)
    {
        profile.MetaUnlocked = true;
        profile.MetaCurrency = Math.Max(profile.MetaCurrency, 1000);
        profile.HighestDifficultyUnlocked = Math.Max(profile.HighestDifficultyUnlocked, 4);
        profile.MauiUnlocked = true;
        profile.UseBlazorUI = true;
        ProfileLoader.Save(profile);
        Console.WriteLine("Dev meta activee : meta currency 1000, UI MAUI debloquee.");
    }

    // Choix du UI via Spectre pour rester homogène, option MAUI dispo si debloquee
    bool blazorUnlocked = profile.MauiUnlocked;
    bool blazorAvailable = blazorUnlocked && (!string.IsNullOrWhiteSpace(mauiExePath) || !string.IsNullOrWhiteSpace(csprojPath));
    var optionsUi = new Dictionary<string, string>
    {
        { "1", "Spectre" },
        { "2", "Console simple" }
    };
    if (blazorAvailable) optionsUi["3"] = "MAUI / Blazor";

    var choixUi = tempUi.ChoisirOption("Choisis l'interface :", optionsUi).Item1;
    var modeUi = choixUi?.Trim() switch
    {
        "2" or "console" or "text" => "text",
        "3" or "maui" or "blazor" => blazorAvailable ? "maui" : "spectre",
        _ => "spectre"
    };

    if (modeUi == "maui")
    {
        try
        {
            if (!hasWinAppRuntime)
            {
                var previousColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(forceMaui
                    ? "Avertissement : Windows App Runtime 1.5 non detecte, lancement force (--force-maui). L'UI peut echouer ou rester blanche."
                    : "Avertissement : Windows App Runtime 1.5 non detecte. On tente quand meme; installe https://aka.ms/windowsappsdk/1.5/desktop-x64 pour eliminer ce warning.");
                Console.ForegroundColor = previousColor;
            }

            if (!string.IsNullOrWhiteSpace(mauiExePath) && File.Exists(mauiExePath))
            {
                var psi = new ProcessStartInfo(mauiExePath)
                {
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(mauiExePath) ?? originalDir
                };
                Console.WriteLine("Lancement de l'UI MAUI/Blazor (exe)... ");
                Process.Start(psi);
                return;
            }

            if (!string.IsNullOrWhiteSpace(csprojPath) && File.Exists(csprojPath))
            {
                var psi = new ProcessStartInfo("dotnet", $"run -f net8.0-windows10.0.19041.0 --project \"{csprojPath}\"")
                {
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(csprojPath) ?? originalDir
                };
                Console.WriteLine("Lancement de l'UI MAUI/Blazor...");
                Process.Start(psi);
                return;
            }

            Console.WriteLine("Impossible de trouver l'exe ou le csproj MAUI/Blazor. MAUI indisponible dans cette build/publish.");
            Console.WriteLine("Retour au mode Spectre.");
            modeUi = "spectre";
        }
        catch (Exception ex)
        {
            Console.WriteLine("Impossible de lancer l'UI MAUI/Blazor : " + ex.Message);
            Console.WriteLine("Retour au mode Spectre.");
            modeUi = "spectre";
        }
    }

    var app = new GameApp(new SwitchableUI(modeUi));
    app.Run(Environment.GetCommandLineArgs());
}
catch (Exception ex)
{
    Console.WriteLine("Une erreur critique s'est produite :");
    Console.WriteLine(ex.ToString());
    Console.WriteLine("Appuie sur une touche pour fermer.");
    Console.ReadKey();
    Environment.Exit(1);
}

static bool HasWinAppRuntime15()
{
#if !WINDOWS
    return false;
#else
    try
    {
        // Vérifie la présence du runtime 1.5 dans la base de registre (HKLM/HKCU)
        string[] hives = { "HKEY_LOCAL_MACHINE", "HKEY_CURRENT_USER" };
        foreach (var hive in hives)
        {
            var baseKey = RegistryKey.OpenBaseKey(
                hive == "HKEY_LOCAL_MACHINE" ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
                RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\WindowsAppRuntime");
            if (key == null) continue;
            foreach (var sub in key.GetSubKeyNames())
            {
                if (sub.Contains("1.5", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
    }
    catch
    {
        // Ignore et considère non trouvé
    }
    return false;
#endif
}
