using ProjetDelta.Player;
using ProjetDelta.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using ProjetDelta.Donjon.Monstres;

namespace ProjetDelta.Systemes
{
    public static class CommandHandler
    {
        private static Func<IEnumerable<Combattant>>? _currentOpponentsProvider;

        public static void SetOpponentsProvider(Func<IEnumerable<Combattant>> provider)
        {
            _currentOpponentsProvider = provider;
        }

        public static void ClearOpponentsProvider()
        {
            _currentOpponentsProvider = null;
        }

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
                        var color = !string.IsNullOrWhiteSpace(dto?.Color)
                            ? dto!.Color!.Trim().ToLowerInvariant()
                            : (!string.IsNullOrWhiteSpace(a.Color) ? a.Color : "white");
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
                    ui.Afficher("- inspect : affiche les ennemis en combat (stats, artefacts).");
                    ui.Afficher("- help : affiche cette aide.");
                    return true;
                case "stats":
                    ui.Afficher($"Attributs : INT {joueur.Intelligence}, FOR {joueur.Force}, CON {joueur.Constitution}, DEX {joueur.Dexterite}");
                    ui.Afficher($"Stats : PV {joueur.PV}/{joueur.PVMax}, ATK {joueur.Attaque:0.##}, SPD {joueur.SPD:0.##}");
                    ui.Afficher($"Bonus loot : {joueur.LootDropBonusPct*100:0.#}%, Bonus or : {joueur.GoldBonusPct*100:0.#}%, XP bonus : {joueur.XpBonusPct*100:0.#}%");
                    ui.Afficher($"Or : {joueur.Argent}");
                    return true;
                case "inspect":
                case "ennemi":
                case "enemy":
                case "ins":
                    var opps = _currentOpponentsProvider?.Invoke()?.Where(o => o != null).ToList();
                    if (opps == null || opps.Count == 0)
                    {
                        ui.Afficher("Aucun ennemi a inspecter pour le moment.");
                        return true;
                    }
                    ui.Afficher("Ennemis :");
                    foreach (var o in opps)
                    {
                        string tierTxt = o is Monstre m ? $"Tier {m.Tier}" : "";
                        ui.Afficher($"- {o.Nom} {tierTxt} | PV {o.PV:0.##}/{o.PVMax:0.##} | ATK {o.Attaque:0.##} | SPD {o.SPD:0.##}");
                        ui.Afficher($"  Stats : FOR {o.Force}, DEX {o.Dexterite}, CON {o.Constitution}, INT {o.Intelligence}");
                        if (o.Artefacts.Count > 0)
                        {
                            ui.Afficher("  Artefacts : " + string.Join(", ", o.Artefacts.Select(a => a.Nom)));
                        }
                        else
                        {
                            ui.Afficher("  Aucun artefact");
                        }
                    }
                    return true;
                case "levelup":
                case "lvl":
                case "points":
                    if (joueur.PointsNonDepenses <= 0)
                    {
                        ui.Afficher("Aucun point a distribuer pour le moment.");
                        return true;
                    }
                    ui.Afficher($"Distribution des points (restants: {joueur.PointsNonDepenses})");
                    joueur.DistribuerPoints(ui);
                    return true;
                default:
                    return false;
            }
        }
    }
}
