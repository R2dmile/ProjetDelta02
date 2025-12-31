using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

namespace ProjetDelta.BlazorHybrid;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<ProjetDelta.BlazorHybrid.Services.UIStateService>();
        builder.Services.AddSingleton<ProjetDelta.BlazorHybrid.Services.GameRunner>();

#if DEBUG
        // Force l'exposition du port DevTools pour la WebView2 (debug console)
        Environment.SetEnvironmentVariable(
            "WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS",
            "--remote-debugging-port=9222 --user-data-dir=" + Path.Combine(Path.GetTempPath(), "wv2debug"));
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

#if WINDOWS
        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddWindows(win =>
            {
                win.OnClosed((window, args) =>
                {
                    try { Environment.Exit(0); } catch { }
                });
            });
        });
#endif

        return builder.Build();
    }
}
