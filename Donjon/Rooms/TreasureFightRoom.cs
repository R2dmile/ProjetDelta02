using ProjetDelta.Player;
using ProjetDelta.UI;
using ProjetDelta.Donjon.Monstres;
using ProjetDelta.Jeu;
using ProjetDelta.Data.DataLoader;
using ProjetDelta.Artifacts;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjetDelta.Systemes.events;
using ProjetDelta.Data.DataClasses;

namespace ProjetDelta.Donjon.Rooms
{
    public class TreasureFightRoom : Room
    {
        private readonly string _name;
        private readonly string _description;
        private readonly string _treasure;
        private Room _nextRoom;
        private readonly MonstreBuilder _builder;
        private readonly int _etage;

        public override string Name => _name;

        public TreasureFightRoom(string name, string description, string treasure, MonstreBuilder builder, int etage)
        {
            _name = name;
            _description = description;
            _treasure = treasure;
            _builder = builder;
            _etage = etage;
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
            ui.Afficher($"Tu apercois : {_treasure}");

            var choix = ui.ChoisirOption("Choisis :", new Dictionary<string, string>
            {
                {"1","Prendre le tresor sans risque"},
                {"2","Combattre 2 monstres pour des recompenses plus fortes"}
            }).Item1;

            if (choix == "1")
            {
                DonnerTresor(ui, joueur);
                return;
            }

            // Combat risque/recompense
            // Pour l'etage 1, rester strictement sur le tier 1
            var tiersPool = (_etage <= 1)
                ? new[] { 1 }
                : new[] { Math.Max(1, _etage), Math.Max(1, Math.Min(7, _etage + 1)) };
            var monstres = new List<Monstre>
            {
                _builder.CreerMonstreParTiers(tiersPool),
                _builder.CreerMonstreParTiers(tiersPool)
            };
            ui.Afficher("Tu defies deux monstres !");
            var combat = new Combat(joueur, monstres, ui);
            combat.Demarrer();
            if (!joueur.EstVivant())
            {
                throw new GameOverException("Le joueur est mort.");
            }

            // Recompenses bonus
            int goldBase = 30 + 20 * _etage;
            int gold = (int)Math.Ceiling(goldBase * (1 + joueur.GoldBonusPct));
            joueur.AjouterArgent(gold);
            ui.Afficher($"Victoire ! Recompense : {gold} or (bonus inclus). Argent total: {joueur.Argent}");

            // XP bonus pour ce combat double
            int xpGain = monstres.Sum(m => 10 + 5 * Math.Max(0, m.Tier - 1));
            joueur.GagnerExperience(xpGain, ui);

            // Recompenses bonus : 2 artefacts communs + 1 rare (garantis)
            var rand = new Random();
            var dtos = ArtefactLoader.GetDtos();
            GiveArtifact(ui, joueur, dtos, "common");
            GiveArtifact(ui, joueur, dtos, "common");
            GiveArtifact(ui, joueur, dtos, "rare");

            // Recompense supplementaire pour le combat double (gold et XP deja donnes)
            int goldBonus = (int)Math.Ceiling((15 + 10 * _etage) * (1 + joueur.GoldBonusPct));
            joueur.AjouterArgent(goldBonus);
            ui.Afficher($"Bonus de difficulte : +{goldBonus} or supplementaires (argent total: {joueur.Argent}).");
        }

        private void GiveArtifact(IUI ui, Joueur joueur, IReadOnlyList<ArtefactDto> dtos, string rarity)
        {
            var pool = dtos.Where(d =>
                rarity == "common"
                    ? (string.IsNullOrWhiteSpace(d.Rarity) || d.Rarity.Equals("common", StringComparison.OrdinalIgnoreCase))
                    : d.Rarity != null && d.Rarity.Equals(rarity, StringComparison.OrdinalIgnoreCase)).ToList();
            if (pool.Count == 0) return;
            var rand = new Random();
            var dto = pool[rand.Next(pool.Count)];
            var artefact = ArtefactLoader.CreerInstance(dto.Id ?? dto.Nom ?? "");
            if (artefact == null) return;
            joueur.EquipArtefact(artefact);
            var color = rarity.Equals("epic", StringComparison.OrdinalIgnoreCase) ? "magenta"
                        : rarity.Equals("rare", StringComparison.OrdinalIgnoreCase) ? "blue"
                        : rarity.Equals("special", StringComparison.OrdinalIgnoreCase) ? "cyan"
                        : "white";
            ui.AffichageBasicInteractif("Butin : ", "white");
            ui.AffichageBasicInteractif($"[{rarity}] ", color);
            ui.AffichageBasicInteractif($"{artefact.Nom}", color);
            ui.TerminerLigne();
            if (!string.IsNullOrWhiteSpace(dto.Description))
                ui.Afficher($"Description : {dto.Description}");
        }

        private void DonnerTresor(IUI ui, Joueur joueur)
        {
            var dto = ArtefactLoader.GetDtos()
                .FirstOrDefault(d =>
                    string.Equals(d.Id, _treasure, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(d.Nom, _treasure, StringComparison.OrdinalIgnoreCase));
            if (dto != null)
            {
                ui.Afficher($"Description : {dto.Description}");
                var artefact = ArtefactLoader.CreerInstance(dto.Id);
                if (artefact != null)
                {
                    joueur.EquipArtefact(artefact);
                    var rarity = dto.Rarity ?? "common";
                    var color = rarity.Equals("epic", StringComparison.OrdinalIgnoreCase) ? "magenta"
                                : rarity.Equals("rare", StringComparison.OrdinalIgnoreCase) ? "blue"
                                : rarity.Equals("special", StringComparison.OrdinalIgnoreCase) ? "cyan"
                                : "white";
                    ui.AffichageBasicInteractif("Tu as equipe : ", "white");
                    ui.AffichageBasicInteractif($"[{rarity}] {artefact.Nom} !", color);
                    ui.TerminerLigne();
                }
                return;
            }

            var digits = new string(_treasure.Where(char.IsDigit).ToArray());
            int gold = 0;
            if (!string.IsNullOrWhiteSpace(digits))
            {
                int.TryParse(digits, out gold);
            }
            if (gold == 0) gold = 10;
            gold = (int)Math.Ceiling(gold * (1 + joueur.GoldBonusPct));
            joueur.AjouterArgent(gold);
            ui.Afficher($"Tu empoches {gold} pieces d'or.");
        }
    }
}
