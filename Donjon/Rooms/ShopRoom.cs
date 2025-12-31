using ProjetDelta.Artifacts;
using ProjetDelta.Player;
using ProjetDelta.UI;
using ProjetDelta.Data.DataLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjetDelta.Data.DataClasses;

namespace ProjetDelta.Donjon.Rooms
{
    public class ShopRoom : Room
    {
        private string _name;
        private string _description;
        private List<string> _items;
        private Room _nextRoom;

        public override string Name => _name;

        public ShopRoom(string name, string description, List<string> items)
        {
            _name = name;
            _description = description;
            _items = items;
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
            ui.Afficher($"Salle Boutique : {_name}");
            ui.Afficher(_description);

            var rand = new Random();
            var dtos = ArtefactLoader.GetDtos();
            var commonsPool = dtos.Where(d => string.IsNullOrWhiteSpace(d.Rarity) || d.Rarity.Equals("common", StringComparison.OrdinalIgnoreCase)).ToList();
            var raresPool = dtos.Where(d => d.Rarity != null && d.Rarity.Equals("rare", StringComparison.OrdinalIgnoreCase)).ToList();
            var epicsPool = dtos.Where(d => d.Rarity != null && d.Rarity.Equals("epic", StringComparison.OrdinalIgnoreCase)).ToList();

            // Comptes par étage (ex: etage1 = 4 communs + 1 rare ; etage5 = 4 rares + 1 épic)
            (int commons, int rares, int epics) GetCounts(int etage) => etage switch
            {
                <= 1 => (4, 1, 0),
                2 => (3, 2, 0),
                3 => (2, 2, 1),
                4 => (1, 3, 1),
                _ => (0, 4, 1)
            };
            var counts = GetCounts(joueur.Etage);

            List<ArtefactDto> Pick(List<ArtefactDto> pool, int n)
            {
                return pool.OrderBy(_ => rand.Next()).Take(Math.Min(n, pool.Count)).ToList();
            }

            var selectedCommons = Pick(commonsPool, counts.commons);
            var selectedRares = Pick(raresPool, counts.rares);
            var selectedEpics = Pick(epicsPool, counts.epics);

            var options = new Dictionary<string, (string label, int price, string idOrNom, ArtefactDto dto)>();
            int idx = 1;
            const int prixRandomBase = 10;
            float priceMult = 1f + 0.05f * Math.Max(0, joueur.Etage - 1); // +5% par etage
            int Scale(int p) => (int)Math.Ceiling(p * priceMult);
            int PriceCommon() => (int)Math.Ceiling(20 * priceMult);
            int PriceRare() => (int)Math.Ceiling(90 * priceMult);
            int PriceEpic() => (int)Math.Ceiling(220 * priceMult);

            void AddOption(ArtefactDto dto, int price)
            {
                var desc = string.IsNullOrWhiteSpace(dto.Description) ? "" : $" ({dto.Description})";
                options[idx.ToString().ToUpperInvariant()] = ($"{dto.Nom ?? dto.Id} - {price} or{desc}", price, dto.Id ?? dto.Nom ?? string.Empty, dto);
                idx++;
            }

            foreach (var dto in selectedCommons) AddOption(dto, PriceCommon());
            foreach (var dto in selectedRares) AddOption(dto, PriceRare());
            foreach (var dto in selectedEpics) AddOption(dto, PriceEpic());

            // Stock aléatoire pour chaque option
            Dictionary<string, int> stock = new Dictionary<string, int>();

            // Option d'achat aléatoire commun
            var randomKey = "R";
            int prixRandom = Scale(prixRandomBase);
            var randomLabel = $"Artefact aleatoire commun - {prixRandom} or";
            options[randomKey.ToUpperInvariant()] = (randomLabel, prixRandom, "__RANDOM__", new ArtefactDto { Nom = randomLabel });

            // Packs communs (2 ou 3 artefacts) uniquement a partir de l'etage 2/3
            void AjouterPack(int count, string key)
            {
                if (commonsPool.Count < count) return;
                var baseDto = commonsPool[rand.Next(commonsPool.Count)];
                var baseName = baseDto.Nom ?? baseDto.Id ?? "artefact";
                int basePrice = Scale(25);
                int price = (int)Math.Ceiling(basePrice * (count == 2 ? 2.2f : 3.2f));
                var label = $"Pack de {count} {baseName} - {price} or";
                options[key.ToUpperInvariant()] = (label, price, $"__PACK{count}__:{baseDto.Id ?? baseDto.Nom}", baseDto);
            }
            if (joueur.Etage >= 2) AjouterPack(2, "P2");
            if (joueur.Etage >= 3) AjouterPack(3, "P3");

            // Slots rares aléatoires selon l'étage
            int rareSlots = Math.Min(3, Math.Max(0, joueur.Etage - 1)); // plus on monte, plus de slots rares (0 au 1er etage)
            int prixRareRandom = Scale(90);
            for (int i = 0; i < rareSlots; i++)
            {
                var key = $"RR{i + 1}";
                var label = $"Artefact rare aleatoire - {prixRareRandom} or";
                options[key.ToUpperInvariant()] = (label, prixRareRandom, "__RARE__", new ArtefactDto { Nom = label });
            }

            // Option de soin instantané à l'achat
            int healPrice = Scale(15);
            const float healAmount = 40f;
            options["H"] = ($"Potion de soin (+{healAmount} PV immediat) - {healPrice} or", healPrice, "__HEAL__", new ArtefactDto { Nom = "Potion de soin" });

            if (options.Count == 0)
            {
                ui.Afficher("La boutique est vide.");
                return;
            }

            // Limiter les options pour les mini-shops
            if (_name.StartsWith("MiniShop", StringComparison.OrdinalIgnoreCase) && options.Count > 3)
            {
                options = options.Take(3).ToDictionary(k => k.Key, v => v.Value);
            }

            // Initialiser le stock
            foreach (var key in options.Keys.ToList())
            {
                // mini-shop -> stock 1
                stock[key] = _name.StartsWith("MiniShop", StringComparison.OrdinalIgnoreCase) ? 1 : rand.Next(1, 4); // 1 a 3
            }

            string BuildLabel(string key, (string label, int price, string idOrNom, ArtefactDto dto) v)
            {
                var label = $"{v.label} (stock:{stock.GetValueOrDefault(key, 0)})";
                if (v.price > joueur.Argent) label += " (pas assez d'or)";
                return label;
            }

            bool continuer = true;
            while (continuer)
            {
                ui.Afficher($"Ton argent: {joueur.Argent}");
                // option acheter tout si possible (hors soin)
                var purchasables = options
                    .Where(kv => stock.GetValueOrDefault(kv.Key, 0) > 0 && kv.Value.idOrNom != "__HEAL__")
                    .ToList();
                int coutTotal = purchasables.Sum(kv => kv.Value.price * stock.GetValueOrDefault(kv.Key, 0));
                var availableOptions = options
                    .Where(kv => stock.GetValueOrDefault(kv.Key, 0) > 0)
                    .ToDictionary(k => k.Key, v => BuildLabel(v.Key, v.Value));
                if (coutTotal > 0 && joueur.Argent >= coutTotal)
                {
                    availableOptions["ALL"] = $"Tout acheter ({coutTotal} or) (stocks inclus)";
                }
                availableOptions.Add("0", "Quitter la boutique");
                var nonSelectable = options
                    .Where(o => o.Value.price > joueur.Argent || stock.GetValueOrDefault(o.Key, 0) <= 0)
                    .Select(o => o.Key)
                    .ToList();
                var choix = ui.ChoisirOption("Que veux-tu acheter ?", availableOptions, nonSelectable).Item1.ToUpperInvariant();
                if (choix == "0" || choix == "BACKSPACE") return;
                if (choix == "ALL")
                {
                    if (joueur.Argent < coutTotal)
                    {
                        ui.Afficher("Pas assez d'or pour tout acheter.");
                        continue;
                    }
                    var gains = new List<string>();
                    foreach (var kv in purchasables)
                    {
                        var selectionAll = kv.Value;
                        int qty = stock.GetValueOrDefault(kv.Key, 0);
                        for (int i = 0; i < qty; i++)
                        {
                            if (!joueur.DepenserArgent(selectionAll.price)) { break; }
                            TraiterAchat(ui, joueur, selectionAll, dtos, commonsPool, raresPool, rand, healAmount, silent: true, gains: gains);
                            stock[kv.Key]--;
                        }
                    }
                    if (gains.Count > 0)
                    {
                        ui.Afficher($"Achat groupé : {gains.Count} artefacts obtenus ({string.Join(", ", gains.Distinct())})");
                    }
                    continue;
                }

                if (options.TryGetValue(choix, out var selection))
                {
                    if (stock.GetValueOrDefault(choix, 0) <= 0)
                    {
                        ui.Afficher("Plus de stock pour cet article.");
                        continue;
                    }
                    if (!joueur.DepenserArgent(selection.price))
                    {
                        ui.Afficher("Pas assez d'or pour cet achat.");
                        continue;
                    }
                    if (!TraiterAchat(ui, joueur, selection, dtos, commonsPool, raresPool, rand, healAmount))
                        continue;
                    stock[choix]--;
                }
            }
        }

        private bool TraiterAchat(IUI ui, Joueur joueur, (string label, int price, string idOrNom, ArtefactDto dto) selection, IEnumerable<ArtefactDto> dtos, IEnumerable<ArtefactDto> commonsPool, IEnumerable<ArtefactDto> raresPool, Random rand, float healAmount, bool silent = false, List<string>? gains = null)
        {
            var dtosList = dtos.ToList();
            var commonsList = commonsPool.ToList();
            var raresList = raresPool.ToList();
            IArtefact? artefact;
            ArtefactDto? descDto = selection.dto;
            if (selection.idOrNom == "__RANDOM__")
            {
                var pool = dtosList
                    .Where(d => string.IsNullOrWhiteSpace(d.Rarity) || d.Rarity.Equals("common", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (pool.Count == 0)
                {
                    ui.Afficher("Pas d'artefact disponible.");
                    return false;
                }
                descDto = pool[rand.Next(pool.Count)];
                artefact = ArtefactLoader.CreerInstance(descDto.Id ?? descDto.Nom ?? "");
            }
            else if (selection.idOrNom == "__RARE__")
            {
                var pool = raresList;
                if (pool.Count == 0)
                {
                    ui.Afficher("Pas d'artefact rare disponible.");
                    return false;
                }
                descDto = pool[rand.Next(pool.Count)];
                artefact = ArtefactLoader.CreerInstance(descDto.Id ?? descDto.Nom ?? "");
            }
            else if (selection.idOrNom != null && (selection.idOrNom.StartsWith("__PACK2__") || selection.idOrNom.StartsWith("__PACK3__")))
            {
                int count = selection.idOrNom.Contains("PACK2") ? 2 : 3;
                var parts = selection.idOrNom.Split(':');
                var baseId = parts.Length > 1 ? parts[1] : "";
                var pool = commonsList.ToList();
                if (pool.Count < count)
                {
                    ui.Afficher("Pas assez d'artefacts pour un pack.");
                    return false;
                }
                if (!silent) ui.Afficher($"Tu ouvres un pack de {count} artefacts :");
                for (int i = 0; i < count; i++)
                {
                    var pick = pool[rand.Next(pool.Count)];
                    if (!string.IsNullOrWhiteSpace(baseId))
                    {
                        var forced = pool.FirstOrDefault(p => string.Equals(p.Id, baseId, StringComparison.OrdinalIgnoreCase) || string.Equals(p.Nom, baseId, StringComparison.OrdinalIgnoreCase));
                        if (forced != null) pick = forced;
                    }
                    var a = ArtefactLoader.CreerInstance(pick.Id ?? pick.Nom ?? "");
                    if (a != null)
                    {
                        joueur.EquipArtefact(a);
                        var d = pick.Description ?? a.Description;
                        if (!silent) ui.Afficher($"- {a.Nom} equipe. {d}");
                        gains?.Add(a.Nom);
                    }
                }
                if (!silent) ui.GoNext("Appuie sur Entree pour continuer.");
                return true;
            }
            else if (selection.idOrNom == "__HEAL__")
            {
                joueur.Heal(healAmount);
                if (!silent) ui.Afficher($"Tu te soignes instantanement de {healAmount} PV. PV actuels: {joueur.PV}/{joueur.PVMax}. Il te reste {joueur.Argent} or.");
                return true;
            }
            else
            {
                artefact = ArtefactLoader.CreerInstance(selection.idOrNom);
            }

            if (artefact == null)
            {
                ui.Afficher("Impossible de generer cet artefact.");
                return false;
            }
            joueur.EquipArtefact(artefact);
            var desc = descDto?.Description ?? artefact.Description;
            gains?.Add(artefact.Nom);
            if (!silent)
            {
                ui.Afficher($"Tu achetes et equipes : {artefact.Nom}. Il te reste {joueur.Argent} or.");
                if (!string.IsNullOrWhiteSpace(desc))
                {
                    ui.Afficher($"Description : {desc}");
                }
                if (selection.idOrNom == "__RANDOM__" || selection.idOrNom == "__RARE__")
                {
                    ui.GoNext("Appuie sur Entree pour continuer.");
                }
            }
            return true;
        }
    }
}
