using System;
using System.Collections.Generic;
using System.Linq;
using ProjetDelta.UI;

namespace ProjetDelta.BlazorHybrid.Services
{
    /// <summary>
    /// Adaptateur IUI qui pousse les messages/choix vers l'etat Blazor.
    /// Interactions avancées (choix utilisateur) restent à implémenter côté page Razor.
    /// </summary>
    public class BlazorUIAdapter : IUI, IUILogTarget
    {
        private readonly UIStateService _state;
        private readonly List<UIStateService.MessageSegment> _lineBuffer = new();
        public BlazorUIAdapter(UIStateService state) { _state = state; }

        private static string NormalizeColor(string color) =>
            string.IsNullOrWhiteSpace(color) ? "white" : color.ToLowerInvariant();

        private void FlushBuffer()
        {
            if (_lineBuffer.Count == 0) return;
            _state.AddLine(_lineBuffer.ToList());
            _lineBuffer.Clear();
        }

        public void Afficher(string text)
        {
            FlushBuffer();
            _state.AddMessage(text, "white");
        }

        public void AffichageBasicInteractif(string text, string couleur)
        {
            _lineBuffer.Add(new UIStateService.MessageSegment(text, NormalizeColor(couleur)));
        }

        public void AfficherNonSelectionnable(string text)
        {
            FlushBuffer();
            _state.AddMessage(text, "red");
        }

        public void Degats(string text)
        {
            FlushBuffer();
            _state.AddMessage(text, "red");
        }

        public void Effet(string text)
        {
            FlushBuffer();
            _state.AddMessage(text, "cyan");
        }

        public void TerminerLigne()
        {
            FlushBuffer();
        }

        public void AddLogLine(IEnumerable<UILogSegment> segments)
        {
            var mapped = segments.Select(s => new UIStateService.MessageSegment(s.Text, NormalizeColor(s.Color), s.Artefact, s.Owner)).ToList();
            _state.AddLine(mapped);
        }

        private string Choisir(string titre, Dictionary<string, string> options, List<string>? nonSelectableKeys = null)
        {
            FlushBuffer();
            _state.BeginChoice(titre, options, nonSelectableKeys);
            return _state.WaitForChoice();
        }

        public (string, string) ChoisirOption(string titre, Dictionary<string, string> options)
        {
            var key = Choisir(titre, options);
            return (key, options.TryGetValue(key, out var v) ? v : "");
        }

        public (string, string) ChoisirOption(string titre, Dictionary<string, string> options, List<string> nonSelectableKeys)
        {
            var key = Choisir(titre, options, nonSelectableKeys);
            return (key, options.TryGetValue(key, out var v) ? v : "");
        }

        public string ChoisirOptionAvecConfirmation(string titre, Dictionary<string, string> options)
        {
            return Choisir(titre, options);
        }

        public string QuestionReponse(string question = "")
        {
            FlushBuffer();
            _state.BeginText(question, allowBack: true);
            return _state.WaitForChoice();
        }

        public void GoNext(string prompt = "Appuie sur Entree pour continuer.")
        {
            FlushBuffer();
            var message = string.IsNullOrWhiteSpace(prompt) ? "Appuie sur Entree pour continuer." : prompt;
            _state.BeginNext(message, "Continuer");
            _state.WaitForChoice();
        }

        public void UpdatePlayerState(UIPlayerState state)
        {
            _state.SetPlayerState(state);
        }

        public T ChoisirOption<T>(string titre, List<T> options, Func<T, string> optionToString)
        {
            var dict = options.Select((o, idx) => (o, idx))
                              .ToDictionary(t => (t.idx + 1).ToString(), t => optionToString(t.o));
            var key = Choisir(titre, dict);
            if (int.TryParse(key, out int parsed) && parsed >= 1 && parsed <= options.Count)
                return options[parsed - 1];
            return options.First();
        }

        public T ChoisirOptionAvecConfirmation<T>(string titre, List<T> options, Func<T, string> optionToString)
            => ChoisirOption(titre, options, optionToString);
    }
}
