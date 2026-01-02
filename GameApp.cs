using System;
using System.Collections.Generic;
using ProjetDelta.Data.DataClasses;
using ProjetDelta.Data.DataLoader;
using ProjetDelta.Donjon;
using ProjetDelta.Donjon.Monstres;
using ProjetDelta.Donjon.Rooms;
using ProjetDelta.Jeu;
using ProjetDelta.Player;
using ProjetDelta.Player.ProjetDelta.PlayerLoader;
using ProjetDelta.UI;
using ProjetDelta.Systemes.events;
using ProjetDelta.Artifacts;
using ProjetDelta.Systemes;
using System.Linq;
using System.Threading;

public class GameApp
{
    private readonly IUI _ui;

    public GameApp(IUI ui)
    {
        _ui = ui;
    }

    public void Run(string[] args)
    {
        bool testArtefact = args.Any(a =>
            string.Equals(a, "--test-artefact", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(a, "-t", StringComparison.OrdinalIgnoreCase));
        bool randomDungeon = true || args.Any(a =>
            string.Equals(a, "--random-dungeon", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(a, "-r", StringComparison.OrdinalIgnoreCase));

        _ui.Afficher("Debut du jeu");

        var profile = ProfileLoader.Load();
        CommandHandler.SetProfile(profile);
        // Si la meta est debloquee sans avoir battu le boss final, la reverrouiller
        if (profile.MetaUnlocked && !profile.FinalBossBeaten)
        {
            profile.MetaUnlocked = false;
            ProfileLoader.Save(profile);
        }
        var finalBossData = FinalBossLoader.Load();
        if (profile.MetaUnlocked || profile.FinalBossBeaten)
        {
            _ui.Afficher($"Meta-currency : {profile.MetaCurrency}");
        }

        if (_ui is IDarkModeAware darkUi)
        {
            darkUi.SetDarkMode(profile.DarkModeUnlocked, profile.DarkModeEnabled);
        }

        if (testArtefact)
        {
            _ui.Afficher("Mode test artefact - combat de demonstration");
            DemoArtefactCombat();
            return;
        }

        while (true)
        {
            List<JoueurData> joueursSauvegardes = PlayerLoadeur.ChargerTous()
                .Where(j => !j.Mort)
                .ToList();
            string choix = "";
            Joueur joueur = null;
            while (joueur == null)
            {
                var menuOptions = new Dictionary<string, string>()
                {
                    {"1", "Creer un personnage"},
                    {"2", "Charger un personnage"},
                    {"3", profile.MetaUnlocked ? "Boutique meta (perso)" : "Boutique meta (verrouillee)"},
                    {"4", profile.FinalBossBeaten ? "Tour de test du boss" : "Tour de test du boss (verrouillee)"},
                    {"5", "Quitter le jeu"}
                };
                var nonSelectable = new List<string>();
                if (!profile.MetaUnlocked) nonSelectable.Add("3");
                if (joueursSauvegardes.Count == 0) nonSelectable.Add("2");
                if (!profile.FinalBossBeaten) nonSelectable.Add("4");
                choix = _ui.ChoisirOption("Voulez-vous :", menuOptions, nonSelectable).Item1;

                if (choix == "3")
                {
                    if (!profile.MetaUnlocked)
                    {
                        _ui.Afficher("Boutique meta verrouillee. Bats le boss final pour la debloquer.");
                        continue;
                    }
                    AfficherBoutiqueMeta(profile);
                    continue;
                }
                if (choix == "4")
                {
                    if (!profile.FinalBossBeaten)
                    {
                        _ui.Afficher("Option verrouillee. Termine au moins une fois (mode facile) pour debloquer la tour.");
                        continue;
                    }
                    LancerTourBoss(profile);
                    continue;
                }
                if (choix == "5")
                {
                    Environment.Exit(0);
                }

                var difficulteChoisie = "";
                joueur = choix switch
                {
                    "1" => new PlayerCreator().CreationJoueur(_ui, joueursSauvegardes.Select(j => j.Nom).Concat(finalBossData?.Nom is null ? Array.Empty<string>() : new[] { finalBossData.Nom })),
                    "2" => _ui.ChoisirOption("Personnages disponibles : ", joueursSauvegardes,
                                j =>
                                {
                                    var room = string.IsNullOrWhiteSpace(j.RoomName) ? "GoldenVault" : j.RoomName;
                                    var artefacts = (j.Artefacts != null && j.Artefacts.Count > 0)
                                        ? string.Join(", ", j.Artefacts)
                                        : "aucun artefact";
                                    return $"{j.Nom} - {j.ClasseNom} - {j.BackgroundNom} - Nv {j.Niveau} - Salle: {room} - PV: {j.Stats.VieCourante}/{j.Stats.VieMax} - Artefacts: {artefacts} | Diff: {j.Difficulte}";
                                })
                             .ToJoueur(),
                    _ => throw new Exception("Option invalide")
                };
                if (choix == "1" && joueur == null)
                {
                    // Creation annulee -> retour menu
                    continue;
                }
                  if (choix == "1")
                  {
                      difficulteChoisie = ChoisirDifficulte(profile);
                      joueur.SetDifficulte(difficulteChoisie);
                      ShowQuickProgress("Creation du personnage...");
                      // S'assurer de nettoyer toute ancienne sauvegarde portant le meme nom (evite de recuperer d'anciens artefacts)
                      PlayerLoadeur.Supprimer(joueur.Nom);
                      PlayerLoadeur.Sauvegarder(joueur);
                  }
            }

            joueur.AppliquerMetaBonuses(profile.MetaGoldBonus, profile.MetaLootBonus, profile.MetaStatBonus);
            var diff = DifficultyModifier.FromLevel(joueur.DifficulteLevel);
            MonstreBuilder.CurrentDifficulty = diff;
            int maxEtage = diff.Level >= 5 ? 6 : 5;

            ShowQuickProgress("Chargement du personnage...");

            _ui.Afficher($"Bonjour, {joueur.Nom}");
            _ui.Afficher($"Objectif : franchis les {maxEtage} etages et bats le boss final.");
            _ui.GoNext("Commencer l'aventure");
            _ui.Afficher($"Salle courante: {(string.IsNullOrWhiteSpace(joueur.RoomName) ? "GoldenVault" : joueur.RoomName)}");
            _ui.Afficher($"PV: {joueur.PV}/{joueur.PVMax}");
            if (joueur.Artefacts.Count > 0)
            {
                _ui.Afficher("Artefacts equipes : " + string.Join(", ", joueur.Artefacts.Select(a => a.Nom)));
            }
            else
            {
                _ui.Afficher("Aucun artefact equipe.");
            }
            PushPlayerState(joueur);

            RoomLoader loader = new RoomLoader();
            Dictionary<string, Room> rooms = loader.GetRooms(randomDungeon, joueur.Etage);

            // Enregistrer les commandes globales (ex: "artifacts")
            var hook = new Func<string, bool>(cmd => CommandHandler.TryHandle(cmd, joueur, _ui));
            UIText.SetCommandHook(hook);
            SpectreUI.SetCommandHook(hook);

            // starting point : reprendre la room sauvegardee si elle existe, sinon debut
            string startRoomName = string.IsNullOrWhiteSpace(joueur.RoomName) ? "GoldenVault" : joueur.RoomName;
            joueur.RoomName = startRoomName; // s'assurer qu'on sauvegarde une valeur non vide
            if (!rooms.TryGetValue(startRoomName, out var currentRoom))
            {
                currentRoom = rooms["GoldenVault"];
                joueur.RoomName = currentRoom.Name;
            }
            // Sauvegarde au debut d'etage (pas a chaque salle)
            PlayerLoadeur.Sauvegarder(joueur);

            // Bonus de depart : un artefact rare aleatoire dans la premiere salle du run initial
            if (startRoomName.Equals("GoldenVault", StringComparison.OrdinalIgnoreCase) && joueur.Etage == 1 && joueur.Niveau == 1 && joueur.Exp == 0)
            {
                var rares = ArtefactLoader.GetDtos()
                    .Where(d => d.Rarity != null && d.Rarity.Equals("rare", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (rares.Count > 0)
                {
                    var rand = new Random();
                    var pick = rares[rand.Next(rares.Count)];
                    var art = ArtefactLoader.CreerInstance(pick.Id ?? pick.Nom ?? "");
                    if (art != null)
                    {
                        joueur.EquipArtefact(art);
                        var desc = pick.Description ?? art.Description;
                        _ui.Afficher($"Bonus de depart : tu trouves un artefact rare {art.Nom} !");
                        if (!string.IsNullOrWhiteSpace(desc))
                            _ui.Afficher($"Description : {desc}");
                        PlayerLoadeur.Sauvegarder(joueur);
                    }
                }
            }

            int bossesThisRun = 0;

            while (currentRoom != null)
            {
                try
                {
                    currentRoom.ResolveRoom(_ui, joueur);
                    PushPlayerState(joueur);
                    // Pas de pause si c'est une intersection (embranchement)
                    // Ne pas imposer de pause apres un embranchement ou une boutique
                    if (currentRoom is not IntersectionRoom && currentRoom is not ShopRoom)
                    {
                        _ui.GoNext();
                    }
                    if (currentRoom is CombatRoom combatRoom && combatRoom.IsBoss && joueur.EstVivant())
                    {
                        bossesThisRun++;
                        profile.BossesDefeated += 1;
                        var diffBoss = DifficultyModifier.FromName(joueur.Difficulte);
                        // Deblocage de difficulte uniquement apres un boss d'etage final
                        if (combatRoom.IsBoss && joueur.Etage >= maxEtage && diffBoss.Level >= profile.HighestDifficultyUnlocked && profile.HighestDifficultyUnlocked < 10)
                        {
                            profile.HighestDifficultyUnlocked = diffBoss.Level + 1;
                            var nomDiff = DifficultyModifier.FromLevel(profile.HighestDifficultyUnlocked).Nom;
                            _ui.Afficher($"Nouvelle difficulte debloquee : {nomDiff}");
                        }
                        ProfileLoader.Save(profile);

                        if (joueur.Etage >= maxEtage)
                        {
                            bool firstClear = !profile.FinalBossBeaten;
                            if (firstClear)
                            {
                                _ui.Afficher("Felicitations pour le tutoriel, le veritable jeu commence maintenant !");
                                profile.MetaUnlocked = true;
                                profile.FinalBossBeaten = true;
                                if (profile.BossTestLevelUnlocked < 1) profile.BossTestLevelUnlocked = 1;
                                ProfileLoader.Save(profile);
                                _ui.Afficher("Meta-progression debloquee ! Un nouvel onglet boutique est disponible au menu principal.");
                            }
                            // Burst de meta-currency selon la difficulte vaincue
                            int Tri(int n) => n * (n + 1) / 2;
                            var burst = Tri(diffBoss.Level + 1) * 20;
                            profile.MetaCurrency += burst;
                            ProfileLoader.Save(profile);
                            _ui.Afficher($"Gain meta-currency pour avoir termine la difficulte {diff.Nom} : +{burst} (total {profile.MetaCurrency}).");
                            // Sauvegarder ce joueur comme futur boss final et retirer la save jouable
                            FinalBossLoader.Save(joueur);
                            PlayerLoadeur.Supprimer(joueur.Nom);
                            _ui.Afficher($"{joueur.Nom} prend place sur le trone et devient le prochain boss final.");
                            _ui.Afficher("Retour au menu principal...");
                            currentRoom = null;
                            break;
                        }

                        // Meta-currency : gain immediat apres chaque boss si la meta est debloquee
                        if (profile.MetaUnlocked)
                        {
                            int Tri(int n) => n * (n + 1) / 2;
                            int gain = Tri(bossesThisRun) * 10;
                            profile.MetaCurrency += gain;
                            ProfileLoader.Save(profile);
                            _ui.Afficher($"Meta-progression : +{gain} fragments (total: {profile.MetaCurrency}).");
                        }

                        // Artefact de boss : choix parmi 3 (etage 1 = rare, sinon epic)
                        var dtosBoss = ArtefactLoader.GetDtos();
                        var rarityWanted = joueur.Etage <= 1 ? "rare" : "epic";
                        var poolBoss = dtosBoss.Where(d => d.Rarity != null && d.Rarity.Equals(rarityWanted, StringComparison.OrdinalIgnoreCase)).ToList();
                        if (poolBoss.Count > 0)
                        {
                            var randBoss = new Random();
                            var picks = poolBoss.OrderBy(_ => randBoss.Next()).Take(3).ToList();
                            var optionsBoss = new Dictionary<string, ArtefactDto>();
                            int idxBoss = 1;
                            foreach (var p in picks)
                            {
                                optionsBoss[idxBoss.ToString()] = p;
                                idxBoss++;
                            }
                            var labels = optionsBoss.ToDictionary(
                                kv => kv.Key,
                                kv => $"{kv.Value.Nom} ({rarityWanted}) - {kv.Value.Description}"
                            );
                            var choixKey = _ui.ChoisirOption("Choisis ton butin de boss :", labels).Item1;
                            if (!optionsBoss.TryGetValue(choixKey, out var chosenDto)) chosenDto = optionsBoss.Values.First();
                            if (chosenDto != null)
                            {
                                var art = ArtefactLoader.CreerInstance(chosenDto.Id ?? chosenDto.Nom ?? "");
                                if (art != null)
                                {
                                    joueur.EquipArtefact(art);
                                    var desc = chosenDto.Description ?? art.Description;
                                    var color = rarityWanted.Equals("epic", StringComparison.OrdinalIgnoreCase) ? "magenta" : "blue";
                                    _ui.AffichageBasicInteractif("Butin de boss : ", "white");
                                    _ui.AffichageBasicInteractif($"[{rarityWanted}] ", color);
                                    _ui.AffichageBasicInteractif($"{art.Nom}", color);
                                    _ui.TerminerLigne();
                                    if (!string.IsNullOrWhiteSpace(desc))
                                        _ui.Afficher($"Description : {desc}");
                                }
                            }
                        }

                        joueur.AvancerEtage();
                        joueur.RoomName = "GoldenVault";
                        ShowQuickProgress($"Passage a l'etage {joueur.Etage}");
                        rooms = loader.GetRooms(randomDungeon, joueur.Etage);
                        currentRoom = rooms["GoldenVault"];
                        PlayerLoadeur.Sauvegarder(joueur); // save au debut du nouvel etage
                        continue;
                    }
                    currentRoom = currentRoom.GetNextRoom();
                    joueur.RoomName = currentRoom?.Name;
                }
                catch (GameOverException e)
                {
                    PlayerLoadeur.Supprimer(joueur.Nom); // permadeath : effacer la save
                    currentRoom = null;
                    _ui.Degats(e.Message);
                    if (profile.MetaUnlocked && bossesThisRun > 0)
                    {
                        int Tri(int n) => n * (n + 1) / 2;
                        int gain = Tri(bossesThisRun) * 10;
                        profile.MetaCurrency += gain;
                        ProfileLoader.Save(profile);
                        _ui.Afficher($"Meta-progression : tu recuperes {gain} fragments (total: {profile.MetaCurrency}).");
                    }
                    _ui.Afficher("Mort... retour au menu principal.");
                    break; // revenir au menu principal
                }

            }

            // boucle infinie pour retourner au menu apres un run
        }
    }

    private void ShowQuickProgress(string message)
    {
        if (_ui is SpectreUI spectre)
        {
            spectre.QuickProgress(message);
        }
        else
        {
            _ui.Afficher(message + " ...");
            Thread.Sleep(300);
        }
    }

    private UIPlayerState BuildPlayerState(Joueur joueur)
    {
        var dtos = ArtefactLoader.GetDtos();
        var artefacts = new List<UIArtefact>();
        var indexByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var a in joueur.Artefacts)
        {
            var key = a.Nom;
            if (indexByName.TryGetValue(key, out var idx))
            {
                var existing = artefacts[idx];
                artefacts[idx] = existing with { Count = existing.Count + 1 };
                continue;
            }

            var dto = dtos.FirstOrDefault(d =>
                string.Equals(d.Nom, key, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(d.Id, key, StringComparison.OrdinalIgnoreCase));
            var rarity = dto?.Rarity ?? "common";
            var desc = dto?.Description ?? a.Description ?? "";
            var color = !string.IsNullOrWhiteSpace(dto?.Color)
                ? dto!.Color!.Trim().ToLowerInvariant()
                : (!string.IsNullOrWhiteSpace(a.Color) ? a.Color : "green");

            indexByName[key] = artefacts.Count;
            artefacts.Add(new UIArtefact(key, desc, rarity, color, 1));
        }

        return new UIPlayerState(
            joueur.Nom,
            joueur.BackgroundNom,
            joueur.ClasseNom,
            joueur.Niveau,
            joueur.Etage,
            joueur.Difficulte,
            joueur.Argent,
            joueur.PV,
            joueur.PVMax,
            joueur.Attaque,
            joueur.SPD,
            joueur.PointsNonDepenses,
            artefacts,
            joueur.EstVivant()
        );
    }

    private void PushPlayerState(Joueur joueur)
    {
        try
        {
            _ui.UpdatePlayerState(BuildPlayerState(joueur));
        }
        catch
        {
            // UI non compatible : ignorer
        }
    }

    private string ChoisirDifficulte(ProfileData profile)
    {
        // Construire les niveaux disponibles (0..10)
        var niveaux = Enumerable.Range(0, 11)
            .Select(DifficultyModifier.FromLevel)
            .ToList();
        int maxUnlock = Math.Min(profile.HighestDifficultyUnlocked, niveaux.Count - 1);

        var options = new Dictionary<string, string>();
        var keyToName = new Dictionary<string, string>();
        int idx = 1;
        foreach (var diff in niveaux.Where(d => d.Level <= maxUnlock))
        {
            var desc = diff.Level switch
            {
                0 => " (ennemis un peu plus faibles)",
                1 => " (baseline)",
                2 => " (renforces)",
                _ => $" ({diff.HpMult:0.00}x stats)"
            };
            options[idx.ToString()] = $"{diff.Nom}{desc}";
            keyToName[idx.ToString()] = diff.Nom;
            idx++;
        }
        var choix = _ui.ChoisirOption("Choisis la difficulte :", options).Item1;
        return keyToName[choix];
    }

    private void AfficherBoutiqueMeta(ProfileData profile)
    {
        int Count(float current, float step) => step <= 0 ? 0 : (int)MathF.Round(current / step, MidpointRounding.AwayFromZero);
        int CountInt(int current, int step) => step <= 0 ? 0 : current / step;

        while (true)
        {
            int cLoot = (int)Math.Ceiling(50 * (1 + 0.1f * Count(profile.MetaLootBonus, 0.01f)));
            int cGold = (int)Math.Ceiling(50 * (1 + 0.1f * Count(profile.MetaGoldBonus, 0.05f)));
            int cStats = (int)Math.Ceiling(50 * (1 + 0.1f * Count(profile.MetaStatBonus, 0.01f)));
            int cBg = (int)Math.Ceiling(100 * (1 + 0.1f * CountInt(profile.MetaBackgroundFlatBonus, 1)));
            int cClasse = (int)Math.Ceiling(100 * (1 + 0.1f * Count(profile.MetaClasseMultBonus, 0.10f)));
            int cArtefact = (int)Math.Ceiling(75 * (1 + 0.15f * (profile.MetaArtefacts?.Count ?? 0)));
            int cUi = 100;
            int cDark = 75;

            _ui.Afficher($"Meta-currency actuelle : {profile.MetaCurrency}");
            var options = new Dictionary<string, string>
        {
            {"1", $"+1% chance drop artefact permanent (cout {cLoot})"},
            {"2", $"+5% or permanent (cout {cGold})"},
            {"3", $"+1% stats (PV/ATK/VIT) permanent (cout {cStats})"},
            {"4", $"+1 aux stats de base des backgrounds (cout {cBg})"},
            {"5", $"+10% aux multiplicateurs de classes (cout {cClasse})"},
            {"6", $"Artefact commun permanent pour nouveaux persos (cout {cArtefact})"},
            {"0", "Quitter la boutique meta"}
        };
            if (!profile.MauiUnlocked)
            {
                options["7"] = $"Debloquer l'UI MAUI/Blazor (cout {cUi})";
            }
            else if (!profile.DarkModeUnlocked)
            {
                options["8"] = $"Debloquer le dark mode (cout {cDark})";
            }
            var choix = _ui.ChoisirOption("Boutique meta :", options).Item1;
            if (choix == "0") return;
            if (choix == "1")
            {
                int cout = cLoot;
                if (profile.MetaCurrency < cout)
                {
                    _ui.Afficher("Pas assez de meta-currency.");
                    continue;
                }
                profile.MetaCurrency -= cout;
                profile.MetaLootBonus += 0.01f;
                _ui.Afficher($"Achat effectue. Bonus drop total : {MathF.Round(profile.MetaLootBonus * 100, 1)}%");
            }
            else if (choix == "2")
            {
                int cout = cGold;
                if (profile.MetaCurrency < cout)
                {
                    _ui.Afficher("Pas assez de meta-currency.");
                    continue;
                }
                profile.MetaCurrency -= cout;
                profile.MetaGoldBonus += 0.05f;
                _ui.Afficher($"Achat effectue. Bonus or total : {MathF.Round(profile.MetaGoldBonus * 100, 1)}%");
            }
            else if (choix == "3")
            {
                int cout = cStats;
                if (profile.MetaCurrency < cout)
                {
                    _ui.Afficher("Pas assez de meta-currency.");
                    continue;
                }
                profile.MetaCurrency -= cout;
                profile.MetaStatBonus += 0.01f;
                _ui.Afficher($"Achat effectue. Bonus stats total : {MathF.Round(profile.MetaStatBonus * 100, 1)}%");
            }
            else if (choix == "4")
            {
                int cout = cBg;
                if (profile.MetaCurrency < cout)
                {
                    _ui.Afficher("Pas assez de meta-currency.");
                    continue;
                }
                profile.MetaCurrency -= cout;
                profile.MetaBackgroundFlatBonus += 1;
                ProfileLoader.Save(profile);
                _ui.Afficher($"Achat effectue. Bonus stats de base background : +{profile.MetaBackgroundFlatBonus}.");
            }
            else if (choix == "5")
            {
                int cout = cClasse;
                if (profile.MetaCurrency < cout)
                {
                    _ui.Afficher("Pas assez de meta-currency.");
                    continue;
                }
                profile.MetaCurrency -= cout;
                profile.MetaClasseMultBonus += 0.10f;
                ProfileLoader.Save(profile);
                _ui.Afficher($"Achat effectue. Bonus multiplicateurs de classe : +{MathF.Round(profile.MetaClasseMultBonus * 100,1)}%.");
            }
            else if (choix == "6")
            {
                int cout = cArtefact;
                if (profile.HighestDifficultyUnlocked < 2)
                {
                    _ui.Afficher("Cette amelioration se debloque apres avoir complete le mode Normal.");
                    continue;
                }
                if (profile.MetaCurrency < cout)
                {
                    _ui.Afficher("Pas assez de meta-currency.");
                    continue;
                }
                var commons = ArtefactLoader.GetDtos()
                    .Where(d => string.IsNullOrWhiteSpace(d.Rarity) || d.Rarity.Equals("common", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (commons.Count == 0)
                {
                    _ui.Afficher("Aucun artefact commun disponible.");
                    continue;
                }
                var rand = new Random();
                var pick = commons[rand.Next(commons.Count)];
                var id = pick.Id ?? pick.Nom ?? "";
                profile.MetaCurrency -= cout;
                if (!string.IsNullOrWhiteSpace(id))
                {
                    profile.MetaArtefacts ??= new List<string>();
                    profile.MetaArtefacts.Add(id);
                    var nomAff = pick.Nom ?? id;
                    var descAff = string.IsNullOrWhiteSpace(pick.Description) ? "" : $" | {pick.Description}";
                    _ui.Afficher($"Achat effectue. Artefact permanent ajoute : {nomAff} (commun){descAff}");
                }
                ProfileLoader.Save(profile);
            }
            else if (choix == "7")
            {
                if (profile.MauiUnlocked)
                {
                    _ui.Afficher("UI MAUI/Blazor deja debloquee.");
                    continue;
                }
                int cout = cUi;
                if (profile.MetaCurrency < cout)
                {
                    _ui.Afficher("Pas assez de meta-currency.");
                    continue;
                }
                profile.MetaCurrency -= cout;
                profile.MauiUnlocked = true;
                profile.UseBlazorUI = true;
                ProfileLoader.Save(profile);
                _ui.Afficher("UI MAUI/Blazor debloquee ! Redemarre le jeu pour voir l'option au lancement.");
            }
            else if (choix == "8")
            {
                if (!profile.MauiUnlocked)
                {
                    _ui.Afficher("Debloque d'abord l'UI MAUI/Blazor.");
                    continue;
                }
                if (profile.DarkModeUnlocked)
                {
                    _ui.Afficher("Dark mode deja debloque.");
                    continue;
                }
                int cout = cDark;
                if (profile.MetaCurrency < cout)
                {
                    _ui.Afficher("Pas assez de meta-currency.");
                    continue;
                }
                profile.MetaCurrency -= cout;
                profile.DarkModeUnlocked = true;
                ProfileLoader.Save(profile);
                if (_ui is IDarkModeAware darkUi)
                {
                    darkUi.SetDarkMode(true, profile.DarkModeEnabled);
                }
                _ui.Afficher("Dark mode debloque ! Un bouton apparait dans l'UI MAUI.");
            }
            ProfileLoader.Save(profile);
        }
    }

    private void AfficherBossActuel()
    {
        var state = FinalBossLoader.LoadFull();
        var boss = state.Current;
        if (boss == null || string.IsNullOrWhiteSpace(boss.Nom))
        {
            _ui.Afficher("Aucun boss final enregistre pour l'instant.");
            return;
        }
        _ui.Afficher($"Boss actuel : {boss.Nom}");
        _ui.Afficher($"Classe: {boss.ClasseNom}, Background: {boss.BackgroundNom}");
        _ui.Afficher($"Stats: PV {boss.Stats.VieCourante}/{boss.Stats.VieMax}, ATK {boss.Stats.Attaque}, VIT {boss.Stats.Vitesse}");
        if (boss.Artefacts != null && boss.Artefacts.Count > 0)
        {
            _ui.Afficher("Artefacts : " + string.Join(", ", boss.Artefacts));
        }
        else
        {
            _ui.Afficher("Aucun artefact enregistre.");
        }
        if (state.History != null && state.History.Count > 0)
        {
            _ui.Afficher("Derniers bosses vaincus :");
            foreach (var b in state.History)
            {
                _ui.Afficher($"- {b.Nom} (PV {b.Stats.VieMax}, ATK {b.Stats.Attaque}, VIT {b.Stats.Vitesse})");
            }
        }
    }

    private void LancerTourBoss(ProfileData profile)
    {
        var bossData = FinalBossLoader.Load();
        if (bossData == null || string.IsNullOrWhiteSpace(bossData.Nom))
        {
            _ui.Afficher("Aucun boss enregistre. Termine le donjon une fois pour definir un boss.");
            return;
        }
        if (profile.BossTestLevelUnlocked < 1) profile.BossTestLevelUnlocked = 1;
        _ui.Afficher($"Tour de test du boss. Niveau maximal debloque : {profile.BossTestLevelUnlocked}. Meilleur niveau atteint : {profile.BossTestBestLevel}.");
        while (true)
        {
            _ui.Afficher($"Niveaux disponibles : 1..{profile.BossTestLevelUnlocked} (meilleur atteint : {profile.BossTestBestLevel}).");
            var input = _ui.QuestionReponse("Entrez un niveau, 'INFO' pour details du boss, 'BACK' (ou B/BAC/0/Q) pour quitter :").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input is "BACK" or "B" or "BAC" or "BACKSPACE" or "0" or "Q") return;
            if (input == "INFO")
            {
                AfficherBossActuel();
                _ui.Afficher($"Niveau max debloque : {profile.BossTestLevelUnlocked}, meilleur atteint : {profile.BossTestBestLevel}");
                continue;
            }
            if (!int.TryParse(input, out int level) || level < 1)
            {
                _ui.Afficher("Niveau invalide.");
                continue;
            }
            if (level > profile.BossTestLevelUnlocked)
            {
                _ui.Afficher("Niveau non debloque.");
                continue;
            }
            bool victoire = SimulerTestBoss(level, bossData);
            if (victoire)
            {
                if (level >= profile.BossTestLevelUnlocked)
                {
                    profile.BossTestLevelUnlocked = level + 1;
                }
                if (level > profile.BossTestBestLevel) profile.BossTestBestLevel = level;
                ProfileLoader.Save(profile);
                _ui.Afficher($"Victoire au niveau {level}! Prochain niveau debloque : {profile.BossTestLevelUnlocked}");
            }
            else
            {
                _ui.Afficher($"Defaite au niveau {level}. Le boss reste intact.");
            }
        }
    }

    private bool SimulerTestBoss(int level, JoueurData bossData)
    {
        try
        {
            // Reconstituer le boss en Joueur
            var classes = BasicLoaders.ChargerClasses();
            var backgrounds = BasicLoaders.ChargerBackgrounds();
            var cls = classes.FirstOrDefault(c => c.Nom.Equals(bossData.ClasseNom, StringComparison.OrdinalIgnoreCase)) ?? classes.First();
            var bg = backgrounds.FirstOrDefault(b => b.Nom.Equals(bossData.BackgroundNom, StringComparison.OrdinalIgnoreCase)) ?? backgrounds.First();
            var boss = new Joueur(bossData.Nom, bg, cls, bossData.Stats, bossData.Niveau, bossData.Exp, bossData.Argent, bossData.Etage, bossData.Difficulte ?? "Normal", bossData.PointsNonDepenses);
            if (bossData.Artefacts != null)
            {
                foreach (var nom in bossData.Artefacts)
                {
                    if (string.IsNullOrWhiteSpace(nom)) continue;
                    var art = ArtefactLoader.CreerInstance(nom);
                    if (art != null) boss.EquipArtefact(art);
                }
            }

            // Construire l'adversaire
            var builder = new MonstreBuilder();
            var adversaire = builder.CreerMonstreParTier(5);
            var dtos = ArtefactLoader.GetDtos();
            int commons = 3 * (int)Math.Pow(2, Math.Max(0, level - 1));
            int rares = level >= 5 ? (int)Math.Pow(2, level - 5) : 0;
            int epics = level >= 9 ? (int)Math.Pow(2, level - 9) : 0;

            var commonsPool = dtos.Where(d => string.IsNullOrWhiteSpace(d.Rarity) || d.Rarity.Equals("common", StringComparison.OrdinalIgnoreCase)).ToList();
            var raresPool = dtos.Where(d => d.Rarity != null && d.Rarity.Equals("rare", StringComparison.OrdinalIgnoreCase)).ToList();
            var epicsPool = dtos.Where(d => d.Rarity != null && d.Rarity.Equals("epic", StringComparison.OrdinalIgnoreCase)).ToList();
            var rand = new Random();
            void EquipMany(int count, List<ArtefactDto> pool)
            {
                for (int i = 0; i < count; i++)
                {
                    if (pool.Count == 0) break;
                    var pick = pool[rand.Next(pool.Count)];
                    var art = ArtefactLoader.CreerInstance(pick.Id ?? pick.Nom ?? "");
                    if (art != null) adversaire.EquipArtefact(art);
                }
            }
            EquipMany(commons, commonsPool);
            EquipMany(rares, raresPool);
            EquipMany(epics, epicsPool);

            _ui.Afficher($"Test niveau {level} : Boss {boss.Nom} vs {adversaire.Nom} (artefacts communs:{commons}, rares:{rares}, epics:{epics})");
            var combat = new Combat(boss, new List<Monstre> { adversaire }, _ui);
            combat.Demarrer();
            return boss.EstVivant();
        }
        catch (Exception ex)
        {
            _ui.Afficher($"Erreur pendant le test : {ex.Message}");
            return false;
        }
    }

    private void DemoArtefactCombat()
    {
        var bg = new Background
        {
            Nom = "TestBG",
            Description = "Contexte test",
            Attributs = new AttributsDto(0, 0, 0, 0, 0) { MulConst = 1, MulDex = 1, MulForce = 1, MulIntel = 1 }
        };
        var cl = new Classe
        {
            Nom = "TestClasse",
            Description = "Classe test",
            Attributs = new AttributsDto(0, 0, 0, 0, 0) { MulConst = 1, MulDex = 1, MulForce = 1, MulIntel = 1 }
        };

        var hero = new Joueur("HeroTest", bg, cl, new Stats(30, 30, 6, 10));
        var gob = new Monstre("GobTest", "red", new Attributs(), new Stats(20, 20, 3, 6));

        var artefacts = ArtefactLoader.ChargerDepuisJson();
        // Artefacts du heros (depuis le JSON)
        hero.EquipArtefact(artefacts["poison2"]);
        hero.EquipArtefact(artefacts["heal_atk1"]);
        hero.EquipArtefact(artefacts["regen2"]);
        hero.EquipArtefact(artefacts["shield20"]);
        hero.EquipArtefact(artefacts["shield10"]); // second bouclier pour voir l'interaction

        // Artefacts du gobelin
        gob.EquipArtefact(artefacts["regen2"]);
        gob.EquipArtefact(artefacts["armor1"]);
        gob.EquipArtefact(artefacts["thorns"]);

        var combat = new Combat(hero, new List<Monstre> { gob }, _ui);
        combat.Demarrer();
    }
}
