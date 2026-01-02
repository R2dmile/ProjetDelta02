using ProjetDelta.Artifacts;
using ProjetDelta.Data.DataClasses;
using ProjetDelta.Data.DataLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ProjetDelta.Player
{
    namespace ProjetDelta.PlayerLoader
    {
        public class PlayerLoadeur
        {
            private static string ResolveUserDataPath(string fileName)
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProjetDelta");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, fileName);
            }

            private static string ResolvePath()
            {
                // Chemin de sauvegarde utilisateur
                var userPath = ResolveUserDataPath("players.json");
                if (File.Exists(userPath)) return userPath;

                // Si pas encore existant, copier depuis les datas packagées si dispo
                var packaged = Path.Combine(AppContext.BaseDirectory, "Data", "DataStorage", "players.json");
                if (File.Exists(packaged))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(userPath) ?? ".");
                    File.Copy(packaged, userPath, overwrite: false);
                    return userPath;
                }

                // fallback dev
                var dev = Path.Combine("..", "..", "..", "Data", "DataStorage", "players.json");
                if (File.Exists(dev))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(userPath) ?? ".");
                    File.Copy(dev, userPath, overwrite: false);
                }
                return userPath;
            }

            // Sauvegarde le joueur dans un fichier JSON
            public static void Sauvegarder(Joueur joueur, string cheminFichier)
            {
                var data = joueur.ToData();
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(cheminFichier, json);
            }

            public static Joueur Charger(string cheminFichier)
            {
                string json = File.ReadAllText(cheminFichier);
                JoueurData data = JsonSerializer.Deserialize<JoueurData>(json);

                // Charger les listes de classes et backgrounds existants
                List<Classe> classes = BasicLoaders.ChargerClasses();
                List<Background> backgrounds = BasicLoaders.ChargerBackgrounds();

                // Rechercher la classe correspondant au nom sauvegarde
                Classe classeSelectionnee = classes.FirstOrDefault(c => c.Nom.Equals(data.ClasseNom, StringComparison.OrdinalIgnoreCase));
                if (classeSelectionnee == null)
                    throw new Exception($"Classe '{data.ClasseNom}' introuvable.");

                // Rechercher le background correspondant au nom sauvegarde
                Background backgroundSelectionne = backgrounds.FirstOrDefault(b => b.Nom.Equals(data.BackgroundNom, StringComparison.OrdinalIgnoreCase))
                    ?? throw new Exception($"Background '{data.BackgroundNom}' introuvable.");

                var joueur = new Joueur(data.Nom, backgroundSelectionne, classeSelectionnee, data.Stats, data.Niveau, data.Exp, data.Argent, data.Etage, data.Difficulte ?? "Normal", data.PointsNonDepenses, appliquerAttributs: false);
            EquipArtefacts(joueur, data.Artefacts);
            return joueur;
        }

            // Charge la liste complete des joueurs sauvegardes
            public static List<JoueurData> ChargerTous()
            {
                var filePath = ResolvePath();
                if (!File.Exists(filePath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    File.WriteAllText(filePath, "[]");
                    return new List<JoueurData>();
                }
                string json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<List<JoueurData>>(json) ?? new List<JoueurData>();
                return data;
            }

            // Sauvegarde la liste complete des joueurs dans le fichier JSON
            public static void SauvegarderTous(List<JoueurData> joueurs)
            {
                var filePath = ResolvePath();
                string json = JsonSerializer.Serialize(joueurs, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }

            // Ajoute un nouveau joueur a la liste sauvegardee
            public static void Sauvegarder(Joueur joueur)
            {
                List<JoueurData> joueurs = ChargerTous();

                // Trouver l'index du joueur avec le meme nom
                int index = joueurs.FindIndex(j => j.Nom.Equals(joueur.Nom, StringComparison.OrdinalIgnoreCase));

                if (index != -1)
                {
                    // Remplacer le joueur existant
                    joueurs[index] = joueur.ToData();
                }
                else
                {
                    // Ajouter le nouveau joueur
                    joueurs.Add(joueur.ToData());
                }

                SauvegarderTous(joueurs);
            }

            public static void Supprimer(string nomJoueur)
            {
                var joueurs = ChargerTous();
                int index = joueurs.FindIndex(j => j.Nom.Equals(nomJoueur, StringComparison.OrdinalIgnoreCase));
                if (index != -1)
                {
                    joueurs.RemoveAt(index);
                    SauvegarderTous(joueurs);
                }
            }

            // Charge un joueur par son nom en recherchant dans la liste et en recherchant aussi les objets Background et Classe
            public static Joueur Charger(string cheminFichier, List<Classe> classes, List<Background> backgrounds)
            {
                string json = File.ReadAllText(cheminFichier);
                JoueurData data = JsonSerializer.Deserialize<JoueurData>(json);

                Classe classeSelectionnee = classes.FirstOrDefault(c => c.Nom.Equals(data.ClasseNom, StringComparison.OrdinalIgnoreCase));
                if (classeSelectionnee == null)
                    throw new Exception($"Classe '{data.ClasseNom}' introuvable.");

                Background backgroundSelectionne = backgrounds.FirstOrDefault(b => b.Nom.Equals(data.BackgroundNom, StringComparison.OrdinalIgnoreCase));
                if (backgroundSelectionne == null)
                    throw new Exception($"Background '{data.BackgroundNom}' introuvable.");

                var joueur = new Joueur(data.Nom, backgroundSelectionne, classeSelectionnee, data.Stats, data.Niveau, data.Exp, data.Argent, data.Etage, data.Difficulte ?? "Normal", data.PointsNonDepenses, appliquerAttributs: false);
                EquipArtefacts(joueur, data.Artefacts);
                return joueur;
            }

        private static void EquipArtefacts(Joueur joueur, List<string> artefactNoms)
        {
            if (artefactNoms == null || artefactNoms.Count == 0) return;
            var ordered = artefactNoms
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => ArtefactLoader.CreerInstance(n))
                .Where(a => a != null)
                .ToList();

            foreach (var art in ordered)
            {
                joueur.EquipArtefact(art!, applyOnEquip: false);
            }

            // Affichage concis : regrouper par nom (pour debug/ui console)
            var groups = ordered
                .GroupBy(a => a!.Nom, StringComparer.OrdinalIgnoreCase)
                .Select(g => (Nom: g.First()!.Nom, Count: g.Count()))
                .ToList();
            if (groups.Count > 0 && joueur is Joueur j)
            {
                var summary = string.Join(", ", groups.Select(g => g.Count > 1 ? $"{g.Count}x {g.Nom}" : g.Nom));
                j.Logger?.Log(new CombatDataItem { Event = TypeEvenement.Autre, Message = $"Artefacts charges : {summary}" });
            }
        }
    }
}

}
