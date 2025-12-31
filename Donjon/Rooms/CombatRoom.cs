using ProjetDelta.Donjon.Monstres;
using ProjetDelta.Jeu;
using ProjetDelta.Player;
using ProjetDelta.Systemes.events;
using ProjetDelta.UI;
using ProjetDelta.Artifacts;
using ProjetDelta.Data.DataLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetDelta.Donjon.Rooms
{
    public class CombatRoom : Room
    {
        private string _name;
        private string _description;
        private List<Monstre> _monstres;
        private Room _nextRoom;
        private bool _isBoss;
        private string? _displayName;
        private readonly bool _isElite;

        public override string Name => _name;
        public bool IsBoss => _isBoss;
        public bool IsElite => _isElite;

        public CombatRoom(string name, string description, List<Monstre> monstres, bool isBoss = false, string? displayName = null, bool isElite = false)
        {
            _name = name;
            _description = description;
            _monstres = monstres;
            _isBoss = isBoss;
            _displayName = displayName;
            _isElite = isElite;
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
            var nomAffiche = _displayName ?? _name;
            bool isFinalBossRoom = _isBoss && string.Equals(_name, "FinalFight", StringComparison.OrdinalIgnoreCase);
            bool noBossYet = isFinalBossRoom && FinalBossLoader.Load() == null;

            ui.Afficher($"Salle Combat : {nomAffiche}");
            ui.Afficher(_description);
            if (noBossYet)
            {
                ui.Afficher("Le trone est encore vide. Aucun adversaire ne se presente pour l'instant.");
                return;
            }
            ui.Afficher("Monstres presents : " + string.Join(", ", _monstres.Select(m => m.Nom)));
            ui.TerminerLigne();

            // Pause avant combat
            ui.GoNext("Appuie sur Entree pour commencer le combat");

            var combat = new Combat(joueur, _monstres, ui);
            combat.Demarrer();
            if (!joueur.EstVivant())
            {
                throw new GameOverException("Le joueur est mort.");
            }
            Console.WriteLine("Combat termine");

            ui.GoNext("Appuie sur Entree pour voir les recompenses");

            // Recompenses
            int Triangulaire(int t)
            {
                // n-th triangular number: n*(n+1)/2
                return t * (t + 1) / 2;
            }

            var goldBase = _monstres.Sum(m =>
            {
                var tier = Math.Max(1, m.Tier);
                return Triangulaire(tier) * 10;
            });
            // Recompense plus genereuse : +50% de base avant bonus d'or
            float eliteNerf = _isElite ? 0.85f : 1f; // nerf leger des elites
            var gold = (int)Math.Ceiling(goldBase * 1.5f * eliteNerf * (1 + joueur.GoldBonusPct));
            joueur.AjouterArgent(gold);
            ui.Afficher($"Victoire ! Recompense : {gold} or (bonus: x{MathF.Round(1 + joueur.GoldBonusPct, 2)}) (argent total: {joueur.Argent})");

            // XP gagnee
            int xpGain = _monstres.Sum(m => 10 + 5 * Math.Max(0, m.Tier - 1));
            xpGain = (int)Math.Ceiling(xpGain * 1.5f); // +50% XP
            joueur.GagnerExperience(xpGain, ui);

            // Artefact bonus eventuel (autorise doublons)
            var rand = new Random();
            var baseChanceCommon = 0.01 + joueur.LootDropBonusPct;
            var luckMult = 1 + joueur.LootDropBonusPct;
            var baseChanceRare = (0.005 * luckMult) + joueur.RareFindBonusPct;
            var baseChanceEpic = (0.001 * luckMult) + (joueur.RareFindBonusPct * 0.5f);

            bool TryOne(ref double ce, ref double cr, ref double cc)
            {
                // Guaranteed portions: each full point = one auto-drop
                if ((int)ce > 0)
                {
                    DonnerArtefact(ui, joueur, "epic");
                    ce -= 1;
                    return true;
                }
                if ((int)cr > 0)
                {
                    DonnerArtefact(ui, joueur, "rare");
                    cr -= 1;
                    return true;
                }
                if ((int)cc > 0)
                {
                    DonnerArtefact(ui, joueur, "common");
                    cc -= 1;
                    return true;
                }

                // Fractional rolls
                if (rand.NextDouble() <= ce)
                {
                    DonnerArtefact(ui, joueur, "epic");
                    return true;
                }
                if (rand.NextDouble() <= cr)
                {
                    DonnerArtefact(ui, joueur, "rare");
                    return true;
                }
                if (rand.NextDouble() <= cc)
                {
                    DonnerArtefact(ui, joueur, "common");
                    return true;
                }
                return false;
            }

            // Tentative unique (nerf : plus de reroll en chaine) + une relance bonus si un loot tombe
            double ceRoll = baseChanceEpic;
            double crRoll = baseChanceRare;
            double ccRoll = baseChanceCommon;
            bool drop = TryOne(ref ceRoll, ref crRoll, ref ccRoll);
            if (drop)
            {
                // Relance supplementaire
                TryOne(ref ceRoll, ref crRoll, ref ccRoll);
            }
            if (_isElite)
            {
                var dtosAll = ArtefactLoader.GetDtos().ToList();
                if (dtosAll.Count > 0)
                {
                    // Leger biais vers le rare
                    var rares = dtosAll.Where(d => d.Rarity != null && d.Rarity.Equals("rare", StringComparison.OrdinalIgnoreCase)).ToList();
                    var commons = dtosAll.Where(d => string.IsNullOrWhiteSpace(d.Rarity) || d.Rarity.Equals("common", StringComparison.OrdinalIgnoreCase)).ToList();
                    var pool = (rand.NextDouble() < 0.3 && rares.Count > 0) ? rares : commons;
                    if (pool.Count == 0) pool = dtosAll;
                    var dto = pool[rand.Next(pool.Count)];
                    var loot = ArtefactLoader.CreerInstance(dto.Id ?? dto.Nom ?? "");
                    if (loot != null)
                    {
                        joueur.EquipArtefact(loot);
                        var desc = dto.Description ?? loot.Description;
                        var rarity = dto.Rarity ?? "common";
                    var color = rarity.Equals("epic", StringComparison.OrdinalIgnoreCase) ? "magenta"
                                : rarity.Equals("rare", StringComparison.OrdinalIgnoreCase) ? "blue"
                                : rarity.Equals("special", StringComparison.OrdinalIgnoreCase) ? "cyan"
                                : "white";
                        ui.AffichageBasicInteractif("Butin elite : ", "white");
                        ui.AffichageBasicInteractif($"[{rarity}] {loot.Nom}", color);
                        ui.TerminerLigne();
                        if (!string.IsNullOrWhiteSpace(desc))
                        {
                            ui.Afficher($"Description : {desc}");
                        }
                    }
                }
            }
        }

        private void DonnerArtefact(IUI ui, Joueur joueur, string rarity)
        {
            var rand = new Random();
            var pool = ArtefactLoader.GetDtos()
                .Where(d => rarity.Equals("epic", StringComparison.OrdinalIgnoreCase) ? d.Rarity != null && d.Rarity.Equals("epic", StringComparison.OrdinalIgnoreCase)
                            : rarity.Equals("rare", StringComparison.OrdinalIgnoreCase) ? d.Rarity != null && d.Rarity.Equals("rare", StringComparison.OrdinalIgnoreCase)
                            : string.IsNullOrWhiteSpace(d.Rarity) || d.Rarity.Equals("common", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (pool.Count == 0) return;
            var dto = pool[rand.Next(pool.Count)];
            var loot = ArtefactLoader.CreerInstance(dto.Id ?? dto.Nom ?? "");
            if (loot == null) return;
            joueur.EquipArtefact(loot);
            var desc = dto.Description ?? loot.Description;
            var rarityLabel = dto.Rarity ?? "common";
            var color = rarityLabel.Equals("epic", StringComparison.OrdinalIgnoreCase) ? "magenta"
                        : rarityLabel.Equals("rare", StringComparison.OrdinalIgnoreCase) ? "blue"
                        : rarityLabel.Equals("special", StringComparison.OrdinalIgnoreCase) ? "cyan"
                        : "white";
            ui.AffichageBasicInteractif("Tu trouves : ", "white");
            ui.AffichageBasicInteractif($"[{rarityLabel}] ", color);
            ui.AffichageBasicInteractif($"{loot.Nom}", color);
            ui.TerminerLigne();
            if (!string.IsNullOrWhiteSpace(desc))
            {
                ui.Afficher($"Description : {desc}");
            }
        }
    }
}
