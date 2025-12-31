using System;
using System.Threading.Tasks;
using ProjetDelta.UI;

namespace ProjetDelta.BlazorHybrid.Services
{
    /// <summary>
    /// Démarre le moteur de jeu dans un thread séparé avec l'IUI Blazor.
    /// </summary>
    public class GameRunner
    {
        private bool _started;
        private Task? _runTask;

        public void EnsureStarted(UIStateService state)
        {
            if (_started) return;
            _started = true;
            state.SetLoading(true);

            _runTask = Task.Run(() =>
            {
                try
                {
                    // S'assurer que les chemins relatifs pour les données fonctionnent.
                    Directory.SetCurrentDirectory(AppContext.BaseDirectory);
                    var ui = new BlazorUIAdapter(state);
                    var app = new GameApp(ui);
                    app.Run(Array.Empty<string>());
                }
                catch (Exception ex)
                {
                    state.SetLoading(false);
                    state.AddMessage($"Erreur MAUI/Blazor : {ex.Message}");
                }
            }).ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null)
                {
                    state.SetLoading(false);
                    state.AddMessage($"Erreur thread UI MAUI : {t.Exception.InnerException?.Message ?? t.Exception.Message}");
                }
            });
        }
    }
}
