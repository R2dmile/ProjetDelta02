using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Threading.Tasks;

namespace ProjetDelta.BlazorHybrid;

public partial class MainPage : ContentPage
{
    private bool _fallbackShown;
    private bool _initialized;
    private bool _webViewDetected;
    private string? _webViewVersion;
    private readonly string _logPath = Path.Combine(FileSystem.AppDataDirectory, "blazor-debug.log");

    public MainPage()
    {
        InitializeComponent();
        AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
        // Exceptions globales -> overlay pour ne pas rester sur un écran gris
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            ShowError("Exception .NET non interceptée", e.ExceptionObject?.ToString());
            AppendLog("UnhandledException", e.ExceptionObject?.ToString());
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            ShowError("Exception tâche asynchrone", e.Exception?.ToString());
            e.SetObserved();
            AppendLog("UnobservedTaskException", e.Exception?.ToString());
        };
        // Vérifie WebView2 sur le thread UI pour éviter l'erreur COM 0x80010106
        MainThread.BeginInvokeOnMainThread(TryCheckWebView);
    }

    private void OnFirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
    {
        AppendLog("FirstChance", e.Exception.ToString());
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadingOverlay.IsVisible = true;
        StartFallbackTimer();
        _ = ProbeWebViewEnvironment();
    }

    private void OnWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
        _initialized = true;
        LoadingOverlay.IsVisible = false;

#if WINDOWS
        try
        {
            var core = e?.WebView?.CoreWebView2;
            if (core != null)
            {
                core.NavigationStarting += (s, args) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        WebViewInfoLabel.Text = "Navigation en cours...";
                        AppendLog("NavigationStarting", args.Uri);
                    });
                };

                core.NavigationCompleted += (s, args) =>
                {
                    if (!args.IsSuccess)
                    {
                        ShowError("Navigation WebView échouée", args.WebErrorStatus.ToString());
                        AppendLog("NavigationFailed", args.WebErrorStatus.ToString());
                    }
                    else
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            WebViewInfoLabel.Text = "UI MAUI chargée.";
                            AppendLog("NavigationCompleted", "Success");
                        });
                    }
                };

                core.ProcessFailed += (s, args) =>
                {
                    ShowError("WebView2 a planté", $"ProcessFailedKind: {args.ProcessFailedKind}");
                    AppendLog("ProcessFailed", args.ProcessFailedKind.ToString());
                };
            }
        }
        catch
        {
            // Ignore : si cela échoue, le fallback timer prendra le relais
        }
#endif
    }

    private void OnWebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e)
    {
#if WINDOWS
        // Laisse l'initialisation par defaut; si besoin de debug, on activera le port plus tard
#endif
    }

    private void TryCheckWebView()
    {
        try
        {
#if WINDOWS
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var version = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString();
                    if (string.IsNullOrWhiteSpace(version))
                    {
                        ShowWebViewMissing();
                        AppendLog("WebView2", "Missing");
                    }
                    else
                    {
                        _webViewDetected = true;
                        _webViewVersion = version;
                        WebViewInfoLabel.Text = $"WebView2 detecte ({version})";
                        AppendLog("WebView2", $"Detected {version}");
                    }
                }
                catch (Exception ex)
                {
                    ShowError("Erreur WebView2 (détection)", ex.Message);
                    AppendLog("WebView2Error", ex.ToString());
                }
            });
#endif
        }
        catch
        {
            ShowWebViewMissing();
        }
    }

    private void StartFallbackTimer()
    {
        _fallbackShown = false;
        Task.Delay(TimeSpan.FromSeconds(12)).ContinueWith(_ =>
        {
            if (LoadingOverlay.IsVisible && !_fallbackShown && !_initialized)
            {
                _fallbackShown = true;
                if (!_webViewDetected)
                {
                    ShowWebViewMissing();
                    AppendLog("Fallback", "WebView2 non detecte");
                }
                else
                {
                    ShowError("Le chargement prend du temps...",
                        $"WebView2 detecte ({_webViewVersion ?? "version inconnue"}). Si l'ecran reste blanc, relance l'application.");
                    AppendLog("Fallback", "WebView2 detecte mais pas init");
                }
            }
        });
    }

    private void ShowWebViewMissing()
    {
        LoadingOverlay.IsVisible = true;
        LoadingOverlay.BackgroundColor = Color.FromArgb("#111827");
        _webViewDetected = false;
        _webViewVersion = null;

        LoadingOverlay.Children.Clear();
        LoadingOverlay.Children.Add(new VerticalStackLayout
        {
            Padding = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new Label
                {
                    Text = "WebView2 runtime manquant",
                    FontSize = 20,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    BackgroundColor = Color.FromArgb("#0f172a"),
                    Padding = new Thickness(12),
                },
                new Label
                {
                    Text = "Installe le runtime Microsoft Edge WebView2 pour lancer l'UI MAUI.",
                    TextColor = Colors.White,
                },
                new Label
                {
                    Text = "Lien: https://go.microsoft.com/fwlink/p/?LinkId=2124703",
                    TextDecorations = TextDecorations.Underline,
                    TextColor = Colors.SkyBlue
                }
            }
        });
    }

    private void ShowError(string title, string? detail)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LoadingOverlay.IsVisible = true;
            LoadingOverlay.BackgroundColor = Color.FromArgb("#111827");
            LoadingOverlay.Children.Clear();
            LoadingOverlay.Children.Add(new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 10,
                Children =
                {
                    new Label
                    {
                        Text = title,
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White
                    },
                    new Label
                    {
                        Text = string.IsNullOrWhiteSpace(detail) ? "Relance l'application après avoir vérifié WebView2." : detail,
                        TextColor = Colors.White,
                        FontSize = 13
                    }
                }
            });
        });
        AppendLog("ShowError", $"{title} | {detail}");
    }

    private async Task ProbeWebViewEnvironment()
    {
#if WINDOWS
        try
        {
            var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync();
            AppendLog("EnvCreate", env.BrowserVersionString);
        }
        catch (Exception ex)
        {
            AppendLog("EnvCreateError", ex.ToString());
        }
#endif
    }

    private void AppendLog(string tag, string? message)
    {
        try
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {tag}: {message}";
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
        catch
        {
            // ignore
        }
    }
}
