using ProjetDelta.Data.DataClasses;
using ProjetDelta.Data.DataLoader;
using ProjetDelta.UI;
using ProjetDelta.Artifacts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetDelta.Player
{
    public class PlayerCreator
    {
        public Joueur? CreationJoueur(IUI ui, IEnumerable<string>? forbiddenNames = null)
        {
            string nom = "";
            var forbidden = new HashSet<string>(forbiddenNames ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            while (true)
            {
                nom = ui.QuestionReponse("Quel est votre nom ?");
                if (string.IsNullOrWhiteSpace(nom))
                {
                    ui.Afficher("Le nom ne peut pas etre vide.");
                    continue;
                }
                if (nom.Equals("0") || nom.Equals("back", StringComparison.OrdinalIgnoreCase) || nom.Equals("retour", StringComparison.OrdinalIgnoreCase))
                {
                    ui.Afficher("Retour au menu principal.");
                    return null;
                }
                if (forbidden.Contains(nom))
                {
                    ui.Afficher("Un personnage porte deja ce nom. Choisis-en un autre.");
                    continue;
                }
                break;
            }

            Background? background = SelectionnerBackground(ui);
            if (background == null) return null;
            Classe? classe = SelectionnerClasse(ui);
            if (classe == null) return null;

            // Instanciation d'un nouveau joueur avec les informations recuperees
            Joueur joueur = new Joueur(nom, background, classe, new Stats());

            // Artefacts thematiques automatiques
            EquiperArtefactsThematiques(ui, joueur, background, classe);
            // Artefacts meta permanents (profil)
            var profile = ProfileLoader.Load();
            if (profile.MetaArtefacts != null && profile.MetaArtefacts.Count > 0)
            {
                var artefactsDispo = ArtefactLoader.ChargerDepuisJson();
                foreach (var id in profile.MetaArtefacts)
                {
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    if (artefactsDispo.TryGetValue(id, out var art))
                    {
                        joueur.EquipArtefact(art);
                        ui.Afficher($"Artefact meta obtenu : {art.Nom}");
                    }
                }
            }

            return joueur;
        }

        private Background? SelectionnerBackground(IUI ui)
        {
                var profile = ProfileLoader.Load();
                List<Background> backgrounds = BasicLoaders.ChargerBackgrounds(profile.MetaBackgroundFlatBonus);
            var artefactsDtos = ArtefactLoader.GetDtos()
                .Where(d => !string.IsNullOrWhiteSpace(d.Id))
                .ToDictionary(d => d.Id, d => d, StringComparer.OrdinalIgnoreCase);

            while (true)
            {
                var options = new Dictionary<string, Background>();
                var labels = new Dictionary<string, string>();
                int idx = 1;
                foreach (var b in backgrounds)
                {
                    var key = idx.ToString();
                    options[key] = b;
                    var attr = b.Attributs;
                    var artefactId = b.ArtefactId ?? "";
                    var artefactNom = (!string.IsNullOrEmpty(artefactId) && artefactsDtos.TryGetValue(artefactId, out var dto))
                        ? dto.Nom
                        : (string.IsNullOrEmpty(artefactId) ? "Artefact thematique" : artefactId);
                    labels[key] = $"{b.Nom} - {b.Description} | Stats: +Intel {attr.Intel}, +Force {attr.Force}, +Const {attr.Constitution}, +Dex {attr.Dex} | Artefact: {artefactNom}";
                    idx++;
                }
                labels["0"] = "Retour";
                var choix = ui.ChoisirOption("Choisissez votre background :", labels).Item1;
                if (choix == "0") return null;
                if (options.TryGetValue(choix, out var selected)) return selected;
                ui.Afficher("Choix invalide.");
            }
        }

        private Classe? SelectionnerClasse(IUI ui)
        {
            var profile = ProfileLoader.Load();
            List<Classe> classes = BasicLoaders.ChargerClasses();
            if (profile.MetaClasseMultBonus > 0)
            {
                foreach (var c in classes)
                {
                    c.Attributs.MulIntel *= (1 + profile.MetaClasseMultBonus);
                    c.Attributs.MulForce *= (1 + profile.MetaClasseMultBonus);
                    c.Attributs.MulConst *= (1 + profile.MetaClasseMultBonus);
                    c.Attributs.MulDex *= (1 + profile.MetaClasseMultBonus);
                }
            }
            // filtrage par difficulte deblocage
            classes = classes.Where(c => c.DifficultyUnlock <= profile.HighestDifficultyUnlocked).ToList();
            var artefactsDtos = ArtefactLoader.GetDtos()
                .Where(d => !string.IsNullOrWhiteSpace(d.Id))
                .ToDictionary(d => d.Id, d => d, StringComparer.OrdinalIgnoreCase);

            while (true)
            {
                var options = new Dictionary<string, Classe>();
                var labels = new Dictionary<string, string>();
                int idx = 1;
                foreach (var b in classes)
                {
                    var key = idx.ToString();
                    options[key] = b;
                    var a = b.Attributs;
                    var artefactId = b.ArtefactId ?? "";
                    var artefactNom = (!string.IsNullOrEmpty(artefactId) && artefactsDtos.TryGetValue(artefactId, out var dto))
                        ? dto.Nom
                        : (string.IsNullOrEmpty(artefactId) ? "Artefact thematique" : artefactId);
                    labels[key] = $"{b.Nom} - {b.Description} | Multiplicateurs: Intel x{a.MulIntel}, Force x{a.MulForce}, Const x{a.MulConst}, Dex x{a.MulDex} | Artefact: {artefactNom}";
                    idx++;
                }
                labels["0"] = "Retour";
                var choix = ui.ChoisirOption("Choisissez votre classe :", labels).Item1;
                if (choix == "0") return null;
                if (options.TryGetValue(choix, out var selected)) return selected;
                ui.Afficher("Choix invalide.");
            }
        }

        private void EquiperArtefactsThematiques(IUI ui, Joueur joueur, Background background, Classe classe)
        {
            var artefactsDispo = ArtefactLoader.ChargerDepuisJson();

            if (!string.IsNullOrWhiteSpace(background.ArtefactId) && artefactsDispo.TryGetValue(background.ArtefactId, out var artefactBg))
            {
                joueur.EquipArtefact(artefactBg);
                ui.Afficher($"Artefact de background obtenu : {artefactBg.Nom}");
            }

            if (!string.IsNullOrWhiteSpace(classe.ArtefactId) && artefactsDispo.TryGetValue(classe.ArtefactId, out var artefactCls))
            {
                joueur.EquipArtefact(artefactCls);
                ui.Afficher($"Artefact de classe obtenu : {artefactCls.Nom}");
            }
        }
    }
}
