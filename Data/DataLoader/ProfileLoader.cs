using ProjetDelta.Data.DataClasses;
using System.Text.Json;

namespace ProjetDelta.Data.DataLoader
{
    public static class ProfileLoader
    {
        private static string ResolveUserPath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProjetDelta");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "profile.json");
        }

        private static string ResolvePath()
        {
            var userPath = ResolveUserPath();
            if (File.Exists(userPath)) return userPath;

            // Préfère le dossier Data/DataStorage à côté de l'exécutable (publish) pour la copie initiale
            var packaged = Path.Combine(AppContext.BaseDirectory, "Data", "DataStorage", "profile.json");
            if (File.Exists(packaged))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(userPath) ?? ".");
                File.Copy(packaged, userPath, overwrite: false);
                return userPath;
            }

            // Fallback mode dev : copie vers le user path pour ne plus dépendre du binaire
            var dev = Path.Combine("..", "..", "..", "Data", "DataStorage", "profile.json");
            if (File.Exists(dev))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(userPath) ?? ".");
                File.Copy(dev, userPath, overwrite: false);
            }
            return userPath;
        }

        public static ProfileData Load()
        {
            var filePath = ResolvePath();
            if (!File.Exists(filePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
                var def = new ProfileData();
                Save(def);
                return def;
            }

            var json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<ProfileData>(json) ?? new ProfileData();
            data.MetaArtefacts ??= new List<string>();
            // Mise a jour des champs ajoutes apres coup pour compatibilite ancienne sauvegarde
            if (data.HighestDifficultyUnlocked < 0) data.HighestDifficultyUnlocked = 0;
            if (data.BossTestLevelUnlocked < 0) data.BossTestLevelUnlocked = 0;
            if (data.BossTestBestLevel < 0) data.BossTestBestLevel = 0;
            return data;
        }

        public static void Save(ProfileData profile)
        {
            var filePath = ResolvePath();
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }
}
