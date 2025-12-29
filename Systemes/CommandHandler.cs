using ProjetDelta.Player;
using ProjetDelta.UI;
using System;
using System.Linq;

namespace ProjetDelta.Systemes
{
    public static class CommandHandler
    {
        public static bool TryHandle(string input, Joueur joueur, IUI ui)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            var cmd = input.Trim().ToLowerInvariant();

            var dtos = ProjetDelta.Data.DataLoader.ArtefactLoader.GetDtos();

            switch (cmd)
            {
                case "artifacts":
                case "artefacts":
                case "art":
                case "inventaire":
                case "inv":
                    if (joueur.Artefacts.Count == 0)
                    {
                        ui.Afficher("Aucun artefact equipe.");
                        return true;
                    }
                    ui.Afficher($"PV: {joueur.PV}/{joueur.PVMax} | Or: {joueur.Argent}");
                    ui.Afficher("Artefacts equipes :");
                    foreach (var a in joueur.Artefacts)
                    {
                        var dto = dtos.FirstOrDefault(d =>
                            string.Equals(d.Nom, a.Nom, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(d.Id, a.Nom, StringComparison.OrdinalIgnoreCase));
                        var rarity = dto?.Rarity ?? "common";
                        var desc = string.IsNullOrWhiteSpace(a.Description) ? "" : $" - {a.Description}";
                    var color = rarity.Equals("epic", StringComparison.OrdinalIgnoreCase) ? "magenta"
                                : rarity.Equals("rare", StringComparison.OrdinalIgnoreCase) ? "blue"
                                : rarity.Equals("special", StringComparison.OrdinalIgnoreCase) ? "cyan"
                                : "white";
                        ui.AffichageBasicInteractif("- ", "white");
                        ui.AffichageBasicInteractif($"[{rarity}] ", color);
                        ui.AffichageBasicInteractif($"{a.Nom}", color);
                        if (!string.IsNullOrWhiteSpace(desc))
                            ui.AffichageBasicInteractif($"{desc}", "white");
                        ui.TerminerLigne();
                    }
                    return true;
                case "help":
                case "aide":
                    ui.Afficher("Commandes disponibles :");
                    ui.Afficher("- inventaire : liste tes artefacts et l'or.");
                    ui.Afficher("- stats : affiche tes attributs et tes stats.");
                    ui.Afficher("- help : affiche cette aide.");
                    return true;
                case "stats":
                    ui.Afficher($"Attributs : INT {joueur.Intelligence}, FOR {joueur.Force}, CON {joueur.Constitution}, DEX {joueur.Dexterite}");
                    ui.Afficher($"Stats : PV {joueur.PV}/{joueur.PVMax}, ATK {joueur.Attaque:0.##}, SPD {joueur.SPD:0.##}");
                    ui.Afficher($"Bonus loot : {joueur.LootDropBonusPct*100:0.#}%, Bonus or : {joueur.GoldBonusPct*100:0.#}%, XP bonus : {joueur.XpBonusPct*100:0.#}%");
                    ui.Afficher($"Or : {joueur.Argent}");
                    return true;
                default:
                    return false;
            }
        }
    }
}
