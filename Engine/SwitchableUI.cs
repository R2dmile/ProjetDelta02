using System;
using System.Collections.Generic;

namespace ProjetDelta.UI
{
    /// <summary>
    /// Permet de basculer rapidement entre plusieurs implementations de IUI
    /// pour tester l'affichage (Spectre, UIText2, etc.) sans changer le reste du code.
    /// </summary>
    public class SwitchableUI : IUI
    {
        private readonly IUI _spectre = new SpectreUI();
        private readonly IUI _text = new UIText2();
        private IUI _active;

        public SwitchableUI(string mode = "spectre")
        {
            _active = Resolve(mode);
        }

        public void SetMode(string mode)
        {
            _active = Resolve(mode);
        }

        private IUI Resolve(string mode)
        {
            return mode?.ToLowerInvariant() switch
            {
                "text" => _text,
                "console" => _text,
                "spectre" => _spectre,
                _ => _spectre
            };
        }

        public void Afficher(string text) => _active.Afficher(text);
        public void AffichageBasicInteractif(string text, string couleur) => _active.AffichageBasicInteractif(text, couleur);
        public void AfficherNonSelectionnable(string text) => _active.AfficherNonSelectionnable(text);
        public void Degats(string text) => _active.Degats(text);
        public void Effet(string text) => _active.Effet(text);
        public (string, string) ChoisirOption(string titre, Dictionary<string, string> options) => _active.ChoisirOption(titre, options);
        public (string, string) ChoisirOption(string titre, Dictionary<string, string> options, List<string> nonSelectableKeys) => _active.ChoisirOption(titre, options, nonSelectableKeys);
        public string ChoisirOptionAvecConfirmation(string titre, Dictionary<string, string> options) => _active.ChoisirOptionAvecConfirmation(titre, options);
        public string QuestionReponse(string question = "") => _active.QuestionReponse(question);
        public T ChoisirOption<T>(string titre, List<T> options, Func<T, string> optionToString) => _active.ChoisirOption(titre, options, optionToString);
        public T ChoisirOptionAvecConfirmation<T>(string titre, List<T> options, Func<T, string> optionToString) => _active.ChoisirOptionAvecConfirmation(titre, options, optionToString);
        public void GoNext(string prompt = "Appuie sur Entree pour continuer.") => _active.GoNext(prompt);
        public void UpdatePlayerState(UIPlayerState state) => _active.UpdatePlayerState(state);
        public void TerminerLigne() => _active.TerminerLigne();
    }
}
