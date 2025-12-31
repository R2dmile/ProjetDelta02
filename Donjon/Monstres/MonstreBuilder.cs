using ProjetDelta.Data.DataClasses;
using ProjetDelta.Data.DataLoader;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProjetDelta.Artifacts;

namespace ProjetDelta.Donjon.Monstres
{


    public class MonstreData
    {
        [JsonPropertyName("monstres")]
        public List<Monstre> Monstres { get; set; }
    }

    public record MonstreTemplate(string Nom, string Couleur, AttributsDto Attrs, Stats Stats, int Tier);
        public record DifficultyModifier(string Nom, float HpMult, float AtkMult, float SpdMult, int Level)
        {
            // Mode facile davantage amplifie (stats a 70%, vitesse aussi reduite)
            public static DifficultyModifier Facile => new("Facile", 0.7f, 0.7f, 0.7f, 0);
            public static DifficultyModifier Normal => new("Normal", 1f, 1f, 1f, 1);
            public static DifficultyModifier Difficile => new("Difficile", 1.2f, 1.2f, 1.05f, 2);

        public static DifficultyModifier FromName(string nom)
        {
            var n = (nom ?? "").Trim().ToLowerInvariant();
            return n switch
            {
                "facile" => Facile,
                "difficile" => Difficile,
                "normal" => Normal,
                "brutal" => FromLevel(3),
                "infernal" => FromLevel(4),
                "cauchemar" => FromLevel(5),
                "apocalypse" => FromLevel(6),
                "omega" => FromLevel(7),
                "ludicrous" => FromLevel(8),
                "cosmique" => FromLevel(9),
                "absurde" => FromLevel(10),
                _ => ParseDynamic(n)
            };
        }

        public static DifficultyModifier FromLevel(int level)
        {
            // Noms uniques pour les 11 paliers (0..10)
            string[] noms = new[]
            {
                "Facile",
                "Normal",
                "Difficile",
                "Brutal",
                "Infernal",
                "Cauchemar",
                "Apocalypse",
                "Omega",
                "Ludicrous",
                "Cosmique",
                "Absurde"
            };
            var idx = Math.Clamp(level, 0, noms.Length - 1);

            if (level <= 0) return new DifficultyModifier(noms[idx], 0.7f, 0.7f, 0.7f, 0);
            if (level == 1) return new DifficultyModifier(noms[idx], 1f, 1f, 1f, 1);
            if (level == 2) return new DifficultyModifier(noms[idx], 1.2f, 1.2f, 1.05f, 2);

            // A partir du niveau 3, on augmente progressivement
            float hpAtkMult = 1f + 0.1f * level; // +10% par palier
            float spdMult = 1f + 0.02f * level;  // +2% par palier
            return new DifficultyModifier(noms[idx], hpAtkMult, hpAtkMult, spdMult, level);
        }

        private static DifficultyModifier ParseDynamic(string n)
        {
            // chercher un entier dans le nom
            var digits = new string(n.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out int lvl))
            {
                return FromLevel(lvl);
            }
            return Normal;
        }
    }
    public class MonstreBuilder
    {
        private readonly List<MonstreTemplate> _templates;
        private readonly Dictionary<int, List<MonstreTemplate>> _parTier = new();
        public static DifficultyModifier CurrentDifficulty { get; set; } = DifficultyModifier.Normal;
        private readonly List<ArtefactDto> _commons;
        private readonly Random _rand = new();
        public MonstreBuilder()
        {
            var charges = ChargerTousLesFichiers();
            if (charges.Count == 0)
            {
                // Fallback explicite pour eviter les slimes placeholder si les fichiers ne sont pas trouves
                charges = BuildDefaultTier1();
                Console.WriteLine("[WARN] Aucun monstre charge depuis les fichiers, utilisation des monstres T1 par defaut.");
            }
            _templates = charges;
            foreach (var t in _templates)
            {
                if (!_parTier.ContainsKey(t.Tier)) _parTier[t.Tier] = new List<MonstreTemplate>();
                _parTier[t.Tier].Add(t);
            }
            // File d'assure : si aucune entree par tier, on ajoute les T1 par defaut
            if (_parTier.Count == 0)
            {
                var def = BuildDefaultTier1();
                foreach (var t in def)
                {
                    if (!_parTier.ContainsKey(t.Tier)) _parTier[t.Tier] = new List<MonstreTemplate>();
                    _parTier[t.Tier].Add(t);
                    _templates.Add(t);
                }
                Console.WriteLine("[WARN] Aucun monstre valide, fallback T1 force.");
            }
            _commons = ArtefactLoader.GetDtos()
                .Where(d => string.IsNullOrWhiteSpace(d.Rarity) || d.Rarity.Equals("common", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private List<MonstreTemplate> BuildDefaultTier1()
        {
            return new List<MonstreTemplate>
            {
                new MonstreTemplate(
                    "rat geant",
                    "brun",
                    new AttributsDto { Intel = 2, Force = 6, Constitution = 7, Dex = 10, Darkness = 1 },
                    new Stats(15, 2, 12),
                    1),
                new MonstreTemplate(
                    "araignee de cave",
                    "noir",
                    new AttributsDto { Intel = 1, Force = 5, Constitution = 6, Dex = 12, Darkness = 3 },
                    new Stats(10, 1, 8),
                    1),
                new MonstreTemplate(
                    "squelette faible",
                    "blanc",
                    new AttributsDto { Intel = 3, Force = 8, Constitution = 15, Dex = 6, Darkness = 2 },
                    new Stats(20, 4, 6),
                    1),
                new MonstreTemplate(
                    "zombie titubant",
                    "vert pale",
                    new AttributsDto { Intel = 1, Force = 12, Constitution = 10, Dex = 2, Darkness = 4 },
                    new Stats(25, 5, 4),
                    1),
                new MonstreTemplate(
                    "chauve-souris geante",
                    "noir",
                    new AttributsDto { Intel = 2, Force = 4, Constitution = 5, Dex = 14, Darkness = 2 },
                    new Stats(12, 3, 15),
                    1)
            };
        }

        private List<MonstreTemplate> ChargerTousLesFichiers()
        {
            var dossiers = new[]
            {
                "monstres.json",
                "monstresT1.json",
                "monstresT2.json",
                "monstresT3.json",
                "monstresT4.json",
                "monstresT5.json",
                "monstresT6.json",
                "monstresT7.json"
            };
            var result = new List<MonstreTemplate>();
            foreach (var fichier in dossiers)
            {
                try
                {
                    var json = DataFileHelper.ReadText(Path.Combine("Data", "DataStorage", fichier));
                    result.AddRange(ChargerMonstres(json, fichier));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARN] Lecture/parse echouee pour {fichier} : {ex.Message}");
                }
            }
            return result;
        }

        private List<MonstreTemplate> ChargerMonstres(string json, string fichierNom)
        {
            var monstres = new List<MonstreTemplate>();
            int tierParDefaut = 1;
            var matchTier = System.Text.RegularExpressions.Regex.Match(fichierNom, @"T(\d+)");
            if (matchTier.Success && int.TryParse(matchTier.Groups[1].Value, out int tnum))
            {
                tierParDefaut = tnum;
            }

            using (JsonDocument document = JsonDocument.Parse(json))
            {
                JsonElement root = document.RootElement;
                JsonElement monstresArray = root.GetProperty("monstres");

                foreach (JsonElement monstreElement in monstresArray.EnumerateArray())
                {
                    string nom = monstreElement.GetProperty("nom").GetString();
                    string couleur = monstreElement.GetProperty("couleur").GetString() ?? "white";
                    JsonElement attributsElement = monstreElement.GetProperty("attributs");
                    AttributsDto attributDto = new AttributsDto(
                        intel: attributsElement.GetProperty("intel").GetSingle(),
                        force: attributsElement.GetProperty("force").GetSingle(),
                        constitution: attributsElement.GetProperty("constitution").GetSingle(),
                        dex: attributsElement.GetProperty("dex").GetSingle(),
                        darkness: attributsElement.GetProperty("darkness").GetInt32()
                    );

                    JsonElement statsElement = monstreElement.GetProperty("stats");
                    var vie = statsElement.GetProperty("vie").GetInt32();
                    var att = statsElement.GetProperty("attaque").GetInt32();
                    var spd = statsElement.GetProperty("vitesse").GetInt32();
                    Stats stats = new Stats(vie, vie, att, spd);
                    int tier = tierParDefaut;
                    if (monstreElement.TryGetProperty("tier", out var tierElement) && tierElement.ValueKind == JsonValueKind.Number)
                    {
                        tier = tierElement.GetInt32();
                    }
                    monstres.Add(new MonstreTemplate(nom, couleur, attributDto, stats, tier));
                }
            }

            return monstres;
        }
        public Monstre CreerMonstre(string nom)
        {
            var monstreTrouve = _templates.Find(m => m.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase));

            if (monstreTrouve != null)
                return FromTemplate(monstreTrouve);

            // Si le monstre n'est pas trouve, retourne un monstre par defaut ("slime")
            return new Monstre("slime", "vertPale" ,new Attributs(), new Stats(10, 10, 2, 1));
        }

        public Monstre CreerMonstreParTier(int tier)
        {
            if (_parTier.TryGetValue(tier, out var liste) && liste.Count > 0)
            {
                var rand = new Random();
                return FromTemplate(liste[rand.Next(liste.Count)]);
            }
            // fallback tiers inferieurs
            var any = _parTier.Values.SelectMany(x => x).ToList();
            if (any.Count > 0)
            {
                var rand = new Random();
                return FromTemplate(any[rand.Next(any.Count)]);
            }
            return CreerMonstre("slime");
        }

        public Monstre CreerMonstreParTiers(IEnumerable<int> tiers)
        {
            var pool = new List<MonstreTemplate>();
            foreach (var t in tiers.Select(x => Math.Max(1, x)))
            {
                if (_parTier.TryGetValue(t, out var liste) && liste.Count > 0)
                {
                    pool.AddRange(liste);
                }
            }
            if (pool.Count == 0)
            {
                var fallbackTier = Math.Max(1, tiers.FirstOrDefault());
                return CreerMonstreParTier(fallbackTier);
            }
            var rand = new Random();
            return FromTemplate(pool[rand.Next(pool.Count)]);
        }

        public Monstre CreerBossFinal()
        {
            var saved = FinalBossLoader.Load();
            if (saved != null)
            {
                var atts = new Attributs().AddAttributes(saved.Attributs);
                var stats = saved.Stats;
                var monstre = new Monstre(saved.Nom, "crimson", atts, new Stats(stats.VieMax, stats.VieMax, stats.Attaque, stats.Vitesse), tier: 7);
                // Equiper les artefacts sauvegardés
                if (saved.Artefacts != null)
                {
                    foreach (var nom in saved.Artefacts)
                    {
                        var art = ArtefactLoader.CreerInstance(nom);
                        if (art != null) monstre.EquipArtefact(art);
                    }
                }
                return monstre;
            }

            // Boss placeholder special etage 5
            var attsDefault = new Attributs();
            attsDefault.AddAttributes(new AttributsDto
            {
                Intel = 5,
                Force = 25,
                Constitution = 25,
                Dex = 5,
                Darkness = 5
            });
            var statsDefault = new Stats(200, 25, 10);
            return new Monstre("Ombre Primordiale", "darkmagenta", attsDefault, statsDefault, tier: 7);
        }

        private Monstre FromTemplate(MonstreTemplate t)
        {
            var diff = CurrentDifficulty;
            var stats = new Stats(
                t.Stats.VieMax * diff.HpMult,
                t.Stats.VieMax * diff.HpMult,
                t.Stats.Attaque * diff.AtkMult,
                t.Stats.Vitesse * diff.SpdMult
            );
            var att = new Attributs();
            att.AddAttributes(t.Attrs);
            var monstre = new Monstre(t.Nom, t.Couleur, att, stats, t.Tier);

            // Donner des artefacts aux monstres selon la difficulte (triangulaire)
            int Tri(int n) => n * (n + 1) / 2;
            int artefactsToGive = Tri(diff.Level);
            for (int i = 0; i < artefactsToGive && _commons.Count > 0; i++)
            {
                var dto = _commons[_rand.Next(_commons.Count)];
                var art = ArtefactLoader.CreerInstance(dto.Id ?? dto.Nom ?? "");
                if (art != null) monstre.EquipArtefact(art);
            }

            // A partir de diff >=4, donner des rares en plus (triangulaire depuis niveau 4)
            if (diff.Level >= 4)
            {
                var rares = ArtefactLoader.GetDtos().Where(d => d.Rarity != null && d.Rarity.Equals("rare", StringComparison.OrdinalIgnoreCase)).ToList();
                int raresToGive = Tri(diff.Level - 3); // niveau4 -> Tri(1)=1, niveau5->Tri(2)=3...
                for (int i = 0; i < raresToGive && rares.Count > 0; i++)
                {
                    var dto = rares[_rand.Next(rares.Count)];
                    var art = ArtefactLoader.CreerInstance(dto.Id ?? dto.Nom ?? "");
                    if (art != null) monstre.EquipArtefact(art);
                }
            }
            // A partir de diff >=7, donner des epics en plus (triangulaire depuis niveau 7)
            if (diff.Level >= 7)
            {
                var epics = ArtefactLoader.GetDtos().Where(d => d.Rarity != null && d.Rarity.Equals("epic", StringComparison.OrdinalIgnoreCase)).ToList();
                int epicsToGive = Tri(diff.Level - 6); // niveau7 -> Tri(1)=1
                for (int i = 0; i < epicsToGive && epics.Count > 0; i++)
                {
                    var dto = epics[_rand.Next(epics.Count)];
                    var art = ArtefactLoader.CreerInstance(dto.Id ?? dto.Nom ?? "");
                    if (art != null) monstre.EquipArtefact(art);
                }
            }

            return monstre;
        }
    }
}
