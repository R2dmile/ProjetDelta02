using ProjetDelta.Player;
using ProjetDelta.UI;
using ProjetDelta.Data.DataLoader;
using ProjetDelta.Data.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;

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

            var rand = new Random();
            int goldRolls = 1 + Math.Max(0, joueur.TreasureOverloadStacks);
            int artifactRolls = 1 + Math.Max(0, joueur.TreasureOverloadStacks);
            bool miniLoot = _name.StartsWith("MiniLoot", StringComparison.OrdinalIgnoreCase);

            int BaseGold()
            {
                var digits = new string(_treasure.Where(char.IsDigit).ToArray());
                if (!string.IsNullOrWhiteSpace(digits) && int.TryParse(digits, out int parsed) && parsed > 0)
                    return parsed;
                return rand.Next(20, 41); // 20-40 or
            }

            float etageMult = Math.Max(1, joueur.Etage);
            int totalGold = 0;
            for (int i = 0; i < goldRolls; i++)
            {
                int gold = (int)Math.Ceiling(BaseGold() * etageMult);
                gold = (int)Math.Ceiling(gold * (1 + joueur.GoldBonusPct));
                totalGold += gold;
            }
            // 1% de jackpot x10
            if (rand.NextDouble() <= 0.01)
            {
                totalGold *= 10;
                ui.Afficher("Jackpot ! Le coffre deborde d'or (x10).");
            }
            joueur.AjouterArgent(totalGold);
            ui.Afficher($"Tu empoches {totalGold} pieces d'or (bonus compris).");

            var dtosAll = ArtefactLoader.GetDtos().ToList();
            if (dtosAll.Count == 0)
            {
                ui.Afficher("Coffre vide (aucun artefact defini).");
                return;
            }

            bool GiveOneArtefact(string rarityWanted)
            {
                // limitation d'essais pour garantir un drop
                for (int tries = 0; tries < 5; tries++)
                {
                    List<ArtefactDto> pool = rarityWanted switch
                    {
                        "epic" => dtosAll.Where(d => d.Rarity != null && d.Rarity.Equals("epic", StringComparison.OrdinalIgnoreCase)).ToList(),
                        "rare" => dtosAll.Where(d => d.Rarity != null && d.Rarity.Equals("rare", StringComparison.OrdinalIgnoreCase)).ToList(),
                        _ => dtosAll.Where(d => string.IsNullOrWhiteSpace(d.Rarity) || d.Rarity.Equals("common", StringComparison.OrdinalIgnoreCase)).ToList()
                    };
                    if (pool.Count == 0) pool = dtosAll;
                    if (pool.Count == 0) return false;

                    var pick = pool[rand.Next(pool.Count)];
                    var art = ArtefactLoader.CreerInstance(pick.Id ?? pick.Nom ?? "");
                    if (art == null) continue;
                    joueur.EquipArtefact(art);
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
                    return true;
                }
                return false;
            }

            for (int i = 0; i < artifactRolls; i++)
            {
                string rarity;
                if (miniLoot)
                {
                    rarity = "common";
                }
                else
                {
                    double roll = rand.NextDouble();
                    rarity = roll < 0.333 ? "common" : (roll < 0.666 ? "rare" : "epic");
                }
                GiveOneArtefact(rarity);
            }
        }
    }
}
