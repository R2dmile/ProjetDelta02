using ProjetDelta.Data.DataClasses;
using ProjetDelta.Player;
using System.Text.Json;

namespace ProjetDelta.Data.DataLoader
{
    public class FinalBossState
    {
        public JoueurData? Current { get; set; }
        public List<JoueurData> History { get; set; } = new();
    }

    public static class FinalBossLoader
    {
        private static string ResolveUserPath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProjetDelta");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "finalboss.json");
        }

        private static string ResolvePath()
        {
            var userPath = ResolveUserPath();
            if (File.Exists(userPath)) return userPath;

            var packaged = Path.Combine(AppContext.BaseDirectory, "Data", "DataStorage", "finalboss.json");
            if (File.Exists(packaged))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(userPath) ?? ".");
                File.Copy(packaged, userPath, overwrite: false);
                return userPath;
            }

            var dev = Path.Combine("..", "..", "..", "Data", "DataStorage", "finalboss.json");
            if (File.Exists(dev))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(userPath) ?? ".");
                File.Copy(dev, userPath, overwrite: false);
            }
            return userPath;
        }

        private static FinalBossState? _cache;

        private static FinalBossState LoadState()
        {
            if (_cache != null) return _cache;
            var filePath = ResolvePath();
            if (!File.Exists(filePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
                var empty = new FinalBossState();
                SaveState(empty);
                _cache = empty;
                return empty;
            }

            var json = File.ReadAllText(filePath);
            FinalBossState? state = null;
            try
            {
                state = JsonSerializer.Deserialize<FinalBossState>(json);
            }
            catch
            {
                // ignore, try legacy
            }
            if (state == null || (state.Current == null && (state.History == null || state.History.Count == 0)))
            {
                try
                {
                    var legacy = JsonSerializer.Deserialize<JoueurData>(json);
                    // N'accepter une ancienne sauvegarde que si elle est valide (nom present)
                    if (legacy != null && !string.IsNullOrWhiteSpace(legacy.Nom))
                    {
                        state = new FinalBossState { Current = legacy, History = new List<JoueurData>() };
                    }
                }
                catch
                {
                    state = new FinalBossState();
                }
            }
            state ??= new FinalBossState();
            state.History ??= new List<JoueurData>();
            // Si aucun boss valide, forcer un etat vide et sauvegarder pour eviter les placeholders
            if (state.Current == null && state.History.Count == 0)
            {
                SaveState(state);
            }
            _cache = state;
            return state;
        }

        private static void SaveState(FinalBossState state)
        {
            var filePath = ResolvePath();
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
            _cache = state;
        }

        public static JoueurData? Load()
        {
            return LoadState().Current;
        }

        public static FinalBossState LoadFull() => LoadState();

        public static void Save(Joueur joueur)
        {
            var state = LoadState();
            state.History ??= new List<JoueurData>();
            if (state.Current != null)
            {
                state.History.Insert(0, state.Current);
                if (state.History.Count > 5) state.History = state.History.Take(5).ToList();
            }

            var data = joueur.ToData();
            // S'assurer que le boss sauvegardé est full PV
            data.Stats = new Stats(data.Stats.VieMax, data.Stats.VieMax, data.Stats.Attaque, data.Stats.Vitesse);
            state.Current = data;
            SaveState(state);
        }

        public static bool Exists()
        {
            return Load() != null;
        }

        public static string? GetName()
        {
            return Load()?.Nom;
        }
    }
}
