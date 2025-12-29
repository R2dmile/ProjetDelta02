namespace ProjetDelta.Choix
{
    public class Choice
    {
        public string Text { get; set; } = string.Empty;
        public string? NextSection { get; set; } // Pour les choix simples
        public bool IsCombat { get; set; } = false;
        public string? WinSection { get; set; } // Section en cas de victoire
        public string? LoseSection { get; set; } // Section en cas de defaite

        public Choice(string text, string? nextSection = null, bool isCombat = false, string? winSection = null, string? loseSection = null)
        {
            Text = text;
            NextSection = nextSection;
            IsCombat = isCombat;
            WinSection = winSection;
            LoseSection = loseSection;

        }
    }
}
