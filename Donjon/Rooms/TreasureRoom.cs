using ProjetDelta.Player;
using ProjetDelta.UI;
using ProjetDelta.Data.DataLoader;
using ProjetDelta.Data.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetDelta.Donjon.Rooms
{
    public class TreasureRoom : Room
    {
        private string _name;
        private string _description;
        private string _treasure;
        private Room _nextRoom;

        public override string Name => _name;

        public TreasureRoom(string name, string description, string treasure)
        {
            _name = name;
            _description = description;
            _treasure = treasure;
        }

        public override void SetNextRoomName(string name)
        {
            base.SetNextRoomName(name);
        }

        public override void ResolveLinks(Dictionary<string, Room> allRooms)
        {
            if (_nextRoomName != null && allRooms.TryGetValue(_nextRoomName, out var resolved))
                _nextRoom = resolved;
        }

        public override Room GetNextRoom() => _nextRoom;

        public override void ResolveRoom(IUI ui, Joueur joueur)
        {
            ui.Afficher($"Salle Tresor : {_name}");
            ui.Afficher(_description);
            // Artefact cible explicite ?
            var dto = ArtefactLoader.GetDtos()
                .FirstOrDefault(d =>
                    string.Equals(d.Id, _treasure, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(d.Nom, _treasure, StringComparison.OrdinalIgnoreCase));
            bool artifactRolled = false;
            if (dto != null)
            {
                ui.Afficher($"Tu trouves : {dto.Nom ?? _treasure}");
                ui.Afficher($"Description : {dto.Description}");
                var artefact = ArtefactLoader.CreerInstance(dto.Id);
                if (artefact != null)
                {
                    joueur.EquipArtefact(artefact);
                    ui.Afficher($"Tu as equipe : {artefact.Nom} !");
                    artifactRolled = true; // deja un artefact obtenu
                }
            }
            else
            {
                ui.Afficher("Tu fouilles le coffre et recuperes un butin varie...");
            }

            // Variante de tresor : or, soin, artefact random, ou fiole d'XP
            var rand = new Random();

            int iterations = 1 + Math.Max(0, joueur.TreasureOverloadStacks);

            for (int i = 0; i < iterations; i++)
            {
                // Gold (parse premier nombre trouve)
                var digits = new string(_treasure.Where(char.IsDigit).ToArray());
                int gold = 0;
                if (!string.IsNullOrWhiteSpace(digits))
                {
                    int.TryParse(digits, out gold);
                }
                if (gold == 0) gold = 10;
                gold = (int)Math.Ceiling(gold * 10 * (1 + joueur.GoldBonusPct)); // buff x10 sur le gain d'or

                // Soin
                var heal = MathF.Ceiling(joueur.PVMax * 0.5f);

                // Artefact random avec table : 65% common, 20% rare, 5% epic, 10% jackpot (+2 relances)
                var dtosAll = ArtefactLoader.GetDtos();
                void DonnerArtefactParRarity(string rarity)
                {
                    List<ArtefactDto> pool = rarity switch
                    {
                        "epic" => dtosAll.Where(d => d.Rarity != null && d.Rarity.Equals("epic", StringComparison.OrdinalIgnoreCase)).ToList(),
                        "rare" => dtosAll.Where(d => d.Rarity != null && d.Rarity.Equals("rare", StringComparison.OrdinalIgnoreCase)).ToList(),
                        _ => dtosAll.Where(d => string.IsNullOrWhiteSpace(d.Rarity) || d.Rarity.Equals("common", StringComparison.OrdinalIgnoreCase)).ToList()
                    };
                    if (pool.Count == 0) return;
                    var pick = pool[rand.Next(pool.Count)];
                    var art = ArtefactLoader.CreerInstance(pick.Id ?? pick.Nom ?? "");
                    if (art != null)
                    {
                        artifactRolled = true;
                        var rarityLabel = pick.Rarity ?? "common";
                        var color = rarityLabel.Equals("epic", StringComparison.OrdinalIgnoreCase) ? "magenta"
                                    : rarityLabel.Equals("rare", StringComparison.OrdinalIgnoreCase) ? "blue"
                                    : rarityLabel.Equals("special", StringComparison.OrdinalIgnoreCase) ? "cyan"
                                    : "white";
                        ui.AffichageBasicInteractif("Coffre mystique : ", "white");
                        ui.AffichageBasicInteractif($"[{rarityLabel}] ", color);
                        ui.AffichageBasicInteractif($"{art.Nom}", color);
                        ui.TerminerLigne();
                        if (!string.IsNullOrWhiteSpace(pick.Description))
                            ui.Afficher($"Description : {pick.Description}");
                    }
                }

                void RollAndGive()
                {
                    double jackpotBonus = Math.Max(0, joueur.LootDropBonusPct) + 0.01; // +1% de base, plus loot bonus
                    double jackpotChance = 0.10 + jackpotBonus;
                    double commonChance = 0.65 - jackpotBonus - joueur.RareFindBonusPct;
                    double rareChance = 0.20 + joueur.RareFindBonusPct;
                    double epicChance = 0.05;

                    // Securite : clamp
                    if (commonChance < 0.1) commonChance = 0.1;
                    if (jackpotChance > 0.5) jackpotChance = 0.5;

                    double roll = rand.NextDouble();
                    if (roll < commonChance)
                    {
                        DonnerArtefactParRarity("common");
                    }
                    else if (roll < commonChance + rareChance)
                    {
                        DonnerArtefactParRarity("rare");
                    }
                    else if (roll < commonChance + rareChance + epicChance)
                    {
                        DonnerArtefactParRarity("epic");
                    }
                    else
                    {
                        // +2 rolls (recursive)
                        RollAndGive();
                        RollAndGive();
                    }
                }

                // XP potion
                int xp = (int)Math.Ceiling(20.0 + 10.0 * Math.Max(1, joueur.Niveau));

                // Choisir UNE recompense par iteration (sauf overload qui rajoute des iterations)
                var pickReward = rand.Next(4);
                // Option spéciale mini-loot : forcer un choix petit (or ou common)
                bool miniLoot = _name.StartsWith("MiniLoot", StringComparison.OrdinalIgnoreCase);
                if (miniLoot)
                {
                    pickReward = rand.Next(2) == 0 ? 0 : 2; // or ou artefact commun
                }
                switch (pickReward)
                {
                    case 0:
                        joueur.AjouterArgent(gold);
                        ui.Afficher($"Tu empoches {gold} pieces d'or.");
                        break;
                    case 1:
                        var avant = joueur.PV;
                        joueur.Heal(heal);
                        var apres = joueur.PV;
                        ui.Afficher($"Une fontaine mysterieuse te soigne : +{heal} PV ({avant}->{apres}).");
                        break;
                    case 2:
                        if (miniLoot)
                        {
                            // Forcer un commun pour le mini-loot
                            DonnerArtefactParRarity("common");
                            artifactRolled = true;
                        }
                        else
                        {
                            RollAndGive();
                        }
                        break;
                    case 3:
                        joueur.GagnerExperience(xp, ui);
                        ui.Afficher($"Une fiole d'XP emplit ton esprit : +{xp} XP.");
                        break;
                }
            }

            // S'assurer qu'au moins un roll d'artefact a eu lieu si aucune iteration ne l'a fait
            if (!artifactRolled)
            {
                // Cas epic garanti ? On peut refaire un roll commun par defaut (utilise la table definie plus haut)
                void DonnerArtefactParRarityFallback()
                {
                    var dtosAll = ArtefactLoader.GetDtos();
                    var pool = dtosAll.Where(d => string.IsNullOrWhiteSpace(d.Rarity) || d.Rarity.Equals("common", StringComparison.OrdinalIgnoreCase)).ToList();
                    if (pool.Count == 0) return;
                    var pick = pool[rand.Next(pool.Count)];
                    var art = ArtefactLoader.CreerInstance(pick.Id ?? pick.Nom ?? "");
                    if (art != null)
                    {
                        artifactRolled = true;
                        var rarityLabel = pick.Rarity ?? "common";
                        var color = rarityLabel.Equals("epic", StringComparison.OrdinalIgnoreCase) ? "magenta"
                                    : rarityLabel.Equals("rare", StringComparison.OrdinalIgnoreCase) ? "blue"
                                    : rarityLabel.Equals("special", StringComparison.OrdinalIgnoreCase) ? "cyan"
                                    : "white";
                        ui.AffichageBasicInteractif("Coffre mystique (fallback): ", "white");
                        ui.AffichageBasicInteractif($"[{rarityLabel}] {art.Nom}", color);
                        ui.TerminerLigne();
                        if (!string.IsNullOrWhiteSpace(pick.Description))
                            ui.Afficher($"Description : {pick.Description}");
                    }
                }
                DonnerArtefactParRarityFallback();
            }

            return;
        }
    }
}
