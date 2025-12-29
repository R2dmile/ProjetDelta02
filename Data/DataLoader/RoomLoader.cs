using ProjetDelta.Data.DataClasses.Rooms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ProjetDelta.Donjon.Monstres;
using ProjetDelta.Donjon.Rooms;
using ProjetDelta.Data.DataLoader;

namespace ProjetDelta.Data.DataLoader
{
    public class RoomLoader
    {
        private readonly MonstreBuilder _builder = new();

        /// <summary>
        /// Charge les salles. Si randomize = true, construit une progression lineaire: start (GoldenVault) -> salles melangees -> boss (FinalFight)
        /// en ne gardant que les types lineaires (Combat, Shop, Treasure, SkillCheck) pour l'intermediaire.
        /// </summary>
        public Dictionary<string, Room> GetRooms(bool randomize = false, int etage = 1)
        {
            string roomsFilePath = ResolvePath();
            string json = File.ReadAllText(roomsFilePath);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            RawRoomList rawRoomList = JsonSerializer.Deserialize<RawRoomList>(json, options) ?? new RawRoomList { rooms = new List<RawRoom>() };

            return randomize
                ? BuildRandomizedRooms(rawRoomList.rooms, etage)
                : BuildRooms(rawRoomList.rooms, etage);
        }

        private string ResolvePath()
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "DataStorage", "rooms", "rooms.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "Data", "DataStorage", "rooms", "rooms.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "Data", "DataStorage", "rooms", "rooms.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Data", "DataStorage", "rooms", "rooms.json"),
                Path.Combine(AppContext.BaseDirectory, "net8.0", "Data", "DataStorage", "rooms", "rooms.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "DataStorage", "rooms", "rooms.json"),
                Path.Combine("..", "..", "..", "Data", "DataStorage", "rooms", "rooms.json"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProjetDelta", "rooms.json")
            }
            .Select(p => Path.GetFullPath(p))
            .ToList();
            var found = candidates.FirstOrDefault(File.Exists);
            return found ?? Path.Combine("..", "..", "..", "Data", "DataStorage", "rooms", "rooms.json");
        }

        private Dictionary<string, Room> BuildRooms(IEnumerable<RawRoom> raws, int etage)
        {
            Dictionary<string, Room> createdRooms = new();
            // Identifier un boss final potentiel pour fallback
            var bossName = raws.FirstOrDefault(r => string.Equals(r.name, "FinalFight", StringComparison.OrdinalIgnoreCase))?.name;

            foreach (RawRoom raw in raws)
            {
                var room = CreateRoom(raw, etage);
                room.SetNextRoomName(raw.next);
                createdRooms[raw.name] = room;
            }

            foreach (var room in createdRooms.Values)
            {
                room.ResolveLinks(createdRooms);
            }
            return createdRooms;
        }

        private Dictionary<string, Room> BuildRandomizedRooms(IEnumerable<RawRoom> raws, int etage)
        {
            var list = raws.ToList();
            var start = list.FirstOrDefault(r => string.Equals(r.name, "GoldenVault", StringComparison.OrdinalIgnoreCase))
                        ?? list.FirstOrDefault();
            var boss = list.FirstOrDefault(r => string.Equals(r.name, "FinalFight", StringComparison.OrdinalIgnoreCase))
                       ?? list.LastOrDefault();

            var rng = new Random();

            // Pools par type (on reutilise descriptions, pas les liens)
            List<RawRoom> FilterByTier(IEnumerable<RawRoom> pool)
            {
                var filtered = pool.Where(r => r.tier == 0 || r.tier == etage).ToList();
                if (filtered.Count == 0)
                {
                    filtered = pool.ToList();
                }
                return filtered;
            }

            var combatTemplates = FilterByTier(list.Where(r => string.Equals(r.type, "CombatRoom", StringComparison.OrdinalIgnoreCase) && !r.boss && !string.Equals(r.name, "FinalFight", StringComparison.OrdinalIgnoreCase)));
            if (combatTemplates.Count == 0 && list.Any(r => string.Equals(r.type, "CombatRoom", StringComparison.OrdinalIgnoreCase)))
                combatTemplates.Add(list.First(r => string.Equals(r.type, "CombatRoom", StringComparison.OrdinalIgnoreCase)));
            if (combatTemplates.Count == 0)
                combatTemplates.Add(new RawRoom { type = "CombatRoom", description = $"Un combat d'etage {etage} t'attend." });

            var treasureTemplates = FilterByTier(list.Where(r => string.Equals(r.type, "TreasureRoom", StringComparison.OrdinalIgnoreCase)));
            if (treasureTemplates.Count == 0)
                treasureTemplates.Add(new RawRoom { type = "TreasureRoom", description = $"Un petit tresor t'attend a l'etage {etage}.", treasure = "Petite bourse d'or" });

            var intersectionTemplates = FilterByTier(list.Where(r => string.Equals(r.type, "IntersectionRoom", StringComparison.OrdinalIgnoreCase)));
            if (intersectionTemplates.Count == 0)
                intersectionTemplates.Add(new RawRoom { type = "IntersectionRoom", description = $"Un carrefour de l'etage {etage}." });
            var miniBranchTemplates = FilterByTier(list.Where(r => string.Equals(r.type, "MiniBranchRoom", StringComparison.OrdinalIgnoreCase)));
            var shopTemplate = list.FirstOrDefault(r => string.Equals(r.type, "ShopRoom", StringComparison.OrdinalIgnoreCase));

            // Cibles : entre 4 et 10 combats avant le shop final
            int targetCombats = rng.Next(4, 11);
            int combatCount = 0; // inclut combats standards + elites eventuels
            int safeGuard = 0;

            var mainPath = new List<RawRoom>();
            var extraRooms = new List<RawRoom>();

            // Cloner start/boss pour ne pas polluer les datas sources
            RawRoom Clone(RawRoom src, string newName)
            {
                return new RawRoom
                {
                    type = src.type,
                    name = newName,
                    description = src.description,
                    treasure = src.treasure,
                    items = src.items?.ToList() ?? new List<string>(),
                    monsters = src.monsters?.ToList() ?? new List<string>(),
                    boss = src.boss,
                    elite = src.elite
                };
            }

            // Le debut est désormais un combat simple a la place du coffre
            RawRoom startRoom = new RawRoom
            {
                type = "CombatRoom",
                name = start?.name ?? "GoldenVault",
                description = string.IsNullOrWhiteSpace(start?.description)
                    ? "Un premier adversaire garde l'entree du donjon."
                    : start!.description,
                tier = etage
            };
            RawRoom bossRoom = boss != null ? Clone(boss, "FinalFight") : new RawRoom { type = "CombatRoom", name = "FinalFight", description = "La piece finale, un trone silencieux t'attend.", boss = true };

            mainPath.Add(startRoom);
            if (startRoom.type.Equals("CombatRoom", StringComparison.OrdinalIgnoreCase))
            {
                combatCount++;
            }

            int combatIdx = 1;
            int interIdx = 1;
            int treasureIdx = 1;

            RawRoom NewCombat(bool elite = false)
            {
                var template = combatTemplates.Count > 0 ? combatTemplates[rng.Next(combatTemplates.Count)] : new RawRoom { description = "Un combat t'attend." };
                return new RawRoom
                {
                    type = "CombatRoom",
                    name = elite ? $"CombatElite{combatIdx++}" : $"Combat{combatIdx++}",
                    description = template.description,
                    elite = elite
                };
            }

            RawRoom NewTreasure()
            {
                var template = treasureTemplates.Count > 0 ? treasureTemplates[rng.Next(treasureTemplates.Count)] : new RawRoom { description = "Un petit tresor repose ici.", treasure = "Petite bourse d'or" };
                return new RawRoom
                {
                    type = "TreasureRoom",
                    name = $"Treasure{treasureIdx++}",
                    description = template.description,
                    treasure = template.treasure
                };
            }

            RawRoom NewIntersection(out RawRoom eliteChild, out RawRoom treasureChild)
            {
                var template = intersectionTemplates.Count > 0 ? intersectionTemplates[rng.Next(intersectionTemplates.Count)] : new RawRoom { description = "Un carrefour obscur." };
                eliteChild = NewCombat(elite: true);
                treasureChild = NewTreasure();
                return new RawRoom
                {
                    type = "IntersectionRoom",
                    name = $"Intersection{interIdx++}",
                    description = template.description,
                    choices = new Dictionary<string, string>
                    {
                        { "Combattre un elite", eliteChild.name },
                        { "Explorer un tresor", treasureChild.name }
                    }
                };
            }

            RawRoom NewMiniBranch(out RawRoom miniShop, out RawRoom lootRoom)
            {
                var template = miniBranchTemplates.Count > 0 ? miniBranchTemplates[rng.Next(miniBranchTemplates.Count)] : new RawRoom { description = "Un embranchement discret propose un choix simple." };
                miniShop = new RawRoom
                {
                    type = "ShopRoom",
                    name = $"MiniShop{interIdx}",
                    description = "Une echoppe modeste propose un seul article.",
                    items = new List<string> { "random_common" }
                };
                lootRoom = new RawRoom
                {
                    type = "TreasureRoom",
                    name = $"MiniLoot{interIdx}",
                    description = "Une petite cache contient un lot modeste.",
                    treasure = "Petite bourse d'or"
                };
                return new RawRoom
                {
                    type = "IntersectionRoom",
                    name = $"MiniBranch{interIdx++}",
                    description = template.description,
                    choices = new Dictionary<string, string>
                    {
                        { "Mini-shop", miniShop.name },
                        { "Butin rapide", lootRoom.name }
                    }
                };
            }

            while (combatCount < targetCombats && safeGuard < 30)
            {
                safeGuard++;
                double roll = rng.NextDouble();
                if (combatCount >= 2 && roll < 0.15)
                {
                    // Petite branche apres 2 combats : mini shop ou loot commun/cash
                    var mini = NewMiniBranch(out var miniShop, out var lootRoom);
                    mainPath.Add(mini);
                    extraRooms.Add(miniShop);
                    extraRooms.Add(lootRoom);
                    continue;
                }
                if (roll < 0.45 && intersectionTemplates.Count > 0)
                {
                    var inter = NewIntersection(out var eliteChild, out var treasureChild);
                    mainPath.Add(inter);
                    extraRooms.Add(eliteChild);
                    extraRooms.Add(treasureChild);
                    // Comptabiliser un combat potentiel via l'elite
                    combatCount++;
                    continue;
                }
                // Combat standard
                var combat = NewCombat();
                mainPath.Add(combat);
                combatCount++;
            }

            // Shop obligatoire juste avant le boss
            RawRoom shopRoom = shopTemplate != null
                ? Clone(shopTemplate, "ShopBeforeBoss")
                : new RawRoom { type = "ShopRoom", name = "ShopBeforeBoss", description = "Un marchand improvise son etal.", items = new List<string>() };

            mainPath.Add(shopRoom);
            mainPath.Add(bossRoom);

            // Lier la progression principale
            for (int i = 0; i < mainPath.Count - 1; i++)
            {
                mainPath[i].next = mainPath[i + 1].name;
            }

            // Les embranchements reviennent toujours a la salle suivante du chemin principal
            for (int i = 0; i < mainPath.Count; i++)
            {
                if (mainPath[i].type.Equals("IntersectionRoom", StringComparison.OrdinalIgnoreCase))
                {
                    var nextName = (i + 1 < mainPath.Count) ? mainPath[i + 1].name : bossRoom.name;
                    foreach (var child in extraRooms.Where(r => mainPath[i].choices.Values.Contains(r.name)))
                    {
                        child.next = nextName;
                    }
                }
            }

            var ordered = new List<RawRoom>();
            ordered.AddRange(mainPath);
            ordered.AddRange(extraRooms);

            return BuildRooms(ordered, etage);
        }

        private Room CreateRoom(RawRoom raw, int etage)
        {
            return raw.type switch
            {
                "CombatRoom" => BuildCombatRoom(raw, etage),
                "MiniBranchRoom" => new IntersectionRoom(
                    raw.name,
                    raw.description,
                    raw.choices ?? new Dictionary<string, string>()
                ),
                "ShopRoom" => new ShopRoom(
                    raw.name,
                    raw.description,
                    raw.items ?? new List<string>()
                ),
                "TreasureRoom" => MaybeTreasureFight(raw, etage),
                "IntersectionRoom" => new IntersectionRoom(
                    raw.name,
                    raw.description,
                    raw.choices ?? new Dictionary<string, string>()
                ),
                "FountainRoom" => new FountainRoom(
                    raw.name,
                    raw.description
                ),
                "SkillCheckRoom" => new ShrineRoom(
                    raw.name,
                    raw.description
                ),
                "ShrineRoom" => new ShrineRoom(
                    raw.name,
                    raw.description
                ),
                _ => throw new Exception($"Type inconnu : {raw.type}")
            };
        }

        private CombatRoom BuildCombatRoom(RawRoom raw, int etage)
        {
            var isFinal = string.Equals(raw.name, "FinalFight", StringComparison.OrdinalIgnoreCase);
            var (desc, displayName) = isFinal ? FinalRoomTexts(etage) : (raw.description, raw.name);
            bool isElite = raw.elite || raw.name.Contains("elite", StringComparison.OrdinalIgnoreCase);
            return new CombatRoom(
                raw.name,
                desc,
                BuildCombatants(raw, etage, isElite),
                raw.boss || isFinal,
                displayName,
                isElite
            );
        }

        private Room MaybeTreasureFight(RawRoom raw, int etage)
        {
            // 50% de chance de transformer en salle risk/reward
            var rand = new Random();
            bool risk = rand.NextDouble() < 0.5;
            if (risk)
            {
                return new TreasureFightRoom(
                    raw.name,
                    raw.description,
                    raw.treasure ?? "Tresor inconnu",
                    _builder,
                    etage
                );
            }
            return new TreasureRoom(
                raw.name,
                raw.description,
                raw.treasure ?? "Tresor inconnu"
            );
        }

        private (string desc, string display) FinalRoomTexts(int etage)
        {
            string[] noms =
            {
                "Champion de l'etage 1",
                "Seigneur de l'etage 2",
                "Gardien de l'etage 3",
                "Tyran de l'etage 4",
                "Boss ultime"
            };
            string[] descs =
            {
                "Un champion impose sa loi sur cet etage.",
                "Un seigneur guerrier t'attend, l'aura lourde.",
                "Un gardien silencieux protege ce palier.",
                "Un tyran surveille les marches superieures.",
                "La salle du trone semble respirer, prete a defier."
            };
            int idx = Math.Clamp(etage - 1, 0, noms.Length - 1);
            string display = noms[idx];
            string desc = descs[idx];

            if (etage >= 5)
            {
                var fb = FinalBossLoader.Load();
                if (fb != null)
                {
                    desc = $"Sur le trone, une figure familiere se dresse : {fb.Nom}";
                    display = $"Champion final : {fb.Nom}";
                }
                else
                {
                    desc = "Un trone vide t'attend dans une salle aux murs fissures. Une presence tapie dans l'ombre se dresse soudain pour defendre ce lieu.";
                    display = "Boss ultime";
                }
            }

            return (desc, display);
        }

        private List<Monstre> BuildCombatants(RawRoom raw, int etage, bool isElite)
        {
            var result = new List<Monstre>();
            // Autoriser les noms explicites uniquement pour les boss/salles speciales
            bool explicitAllowed = raw.boss || string.Equals(raw.name, "FinalFight", StringComparison.OrdinalIgnoreCase);
            if (explicitAllowed && raw.monsters != null && raw.monsters.Count > 0 && raw.monsters.All(m => !m.StartsWith("__random", StringComparison.OrdinalIgnoreCase)))
            {
                result.AddRange(raw.monsters.Select(name => _builder.CreerMonstre(name)));
                // Si un placeholder "slime" a ete genere, fallback sur generation par tiers
                if (result.Any(m => m.Nom.Equals("slime", StringComparison.OrdinalIgnoreCase)))
                {
                    result.Clear();
                }
                else
                {
                    return result;
                }
            }

            // Combat generé selon l'etage
            int minTier = Math.Max(1, etage - 1);
            int maxTier = Math.Max(minTier, Math.Min(etage, 7));
            int bossTier = Math.Min(7, etage + 2);
            var tiersPool = new[] { minTier, maxTier }.Distinct().ToList();

            // Combat "elite" explicite : tiers +1 et drop d'artefact garanti apres le combat
            if (isElite)
            {
                tiersPool = new[] { Math.Min(7, maxTier + 1) }.ToList();
                var elite = _builder.CreerMonstreParTiers(tiersPool);
                return new List<Monstre> { elite };
            }
            // Boss special
            if (raw.boss || string.Equals(raw.name, "FinalFight", StringComparison.OrdinalIgnoreCase))
            {
                if (etage >= 5)
                {
                    result.Add(_builder.CreerBossFinal());
                    return result;
                }
                result.Add(_builder.CreerMonstreParTier(bossTier));
                return result;
            }

            // Combats standards : 1 monstre par defaut (le double combat est gere via TreasureFightRoom uniquement)
            result.Add(_builder.CreerMonstreParTiers(tiersPool));
            return result;
        }
    }
}
