using ProjetDelta.Systemes;

namespace ProjetDelta.UI
{
    public interface IUI
    {
        public void Afficher(string text);
        public void AffichageBasicInteractif(string text, string couleur);
        public void AfficherNonSelectionnable(string text);
        public void Degats(string text);
        public void Effet(string text);
        public (string, string) ChoisirOption(string titre, Dictionary<string, string> options);
        public (string, string) ChoisirOption(string titre, Dictionary<string, string> options, List<string> nonSelectableKeys);
        public string ChoisirOptionAvecConfirmation(string titre, Dictionary<string, string> options);
        public string QuestionReponse(string question = "");
        public T ChoisirOption<T>(string titre, List<T> options, Func<T, string> optionToString);
        public T ChoisirOptionAvecConfirmation<T>(string titre, List<T> options, Func<T, string> optionToString);
        // public string QuestionReponseBans(string question = "", List<string> bans = new List<string>);
        public void TerminerLigne();
    }
}
