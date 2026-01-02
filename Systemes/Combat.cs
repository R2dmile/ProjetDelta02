using ProjetDelta.Donjon.Monstres;
using ProjetDelta.Logger;
using ProjetDelta.Player;
using ProjetDelta.Systemes.UIJeu;
using ProjetDelta.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ProjetDelta.Systemes;

namespace ProjetDelta.Jeu
{
    public enum ResultatCombat
    {
        Win,
        Lose,
        Unresolved
    }
    public class Combat
    {
        private Joueur _joueur;
        private List<Monstre> _monstres;
        private IUI _ui;
        public ResultatCombat ResultatCombat { get; private set; }
        public Combat(Joueur joueur, List<Monstre> monstres, IUI ui)
        {
            _joueur = joueur;
            _monstres = monstres;
            _ui = ui;
            ResultatCombat = ResultatCombat.Unresolved;
        }

        public void Demarrer()
        {
            _ui.Afficher("Le combat commence !");
            CommandHandler.SetOpponentsProvider(() => _monstres.Where(m => m.EstVivant()));
            if (_monstres.Count > 1)
            {
                _ui.Afficher("Adversaires : " + string.Join(", ", _monstres.Select(m => m.Nom)));
            }

            // Liste de tous les combattants
            List<Combattant> combattants = new List<Combattant> { _joueur };
            combattants.AddRange(_monstres);

            // Initialisation des initiatives
            Dictionary<Combattant, float> initiatives = combattants.ToDictionary(c => c, c => 0f);

            int nbAttaques = 0;
            int tours = 0;
            const int maxTours = 400;
            const int toursParPalier = 10;
            const float stepPct = 0.10f; // 10% par palier
            const float stepHard = 0.10f; // malus heals/esquive apres 100 tours
            int palierDegats = 0;       // montees de degats (toujours actives)
            int palierHard = 0;         // malus suppl. actives apres 100 tours
            // Reset des multiplicateurs globaux
            Combattant.GlobalDamageMultiplier = 1f;
            Combattant.GlobalHealMultiplier = 1f;
            Combattant.GlobalDodgeMultiplier = 1f;

            // Calcul des PV totaux initiaux
            float pvInitiaux = combattants.Sum(c => c.PV);

            CombatLogger logger = new CombatLogger(new List<CombatDataItem>());
            _joueur.SetLogger(logger);
            foreach (var m in combattants) m.SetLogger(logger);

            // Les artefacts peuvent reagir au debut du combat
            foreach (var combattant in combattants)
            {
                combattant.DebutCombat();
            }

            // Montrer les artefacts des monstres
            foreach (var monstre in _monstres)
            {
                if (monstre.Artefacts.Count > 0)
                {
                    var liste = string.Join(", ", monstre.Artefacts.Select(a => a.Nom));
                    _ui.Afficher($"{monstre.Nom} porte : {liste}");
                }
            }

            // Boucle de combat
            while (_joueur.EstVivant() && _monstres.Any(m => m.EstVivant()) && tours < maxTours)
            {
                // Accumulation des initiatives
                foreach (var combattant in combattants.Where(c => c.EstVivant()))
                {
                    initiatives[combattant] += combattant.SPD;
                }

                // Determination du combattant avec la plus haute initiative
                var attaquant = initiatives
                    .Where(i => i.Key.EstVivant())
                    .OrderByDescending(i => i.Value)
                    .First()
                    .Key;

                // Determination de la cible
                Combattant cible;
                if (attaquant == _joueur)
                {
                    cible = _monstres.FirstOrDefault(m => m.EstVivant());
                }
                else
                {
                    cible = _joueur;
                }
                CombatDataItem data = new CombatDataItem();
                // Effectuer l'attaque
                if (cible != null)
                {
                    attaquant.SetCurrentTarget(cible);
                    nbAttaques++;
                    // Pour eviter les combats infinis : on augmente progressivement les degats globaux.
                    float multiplicateurDegats = Combattant.GlobalDamageMultiplier;
                    logger.Log(new CombatDataItem
                    {
                        Event = TypeEvenement.Autre,
                        Message = $"--- Debut du tour {nbAttaques} ({attaquant.Nom}) ---"
                    });
                    attaquant.DebutTour();
                    if (!attaquant.EstVivant())
                    {
                        initiatives[attaquant] = 0;
                        continue;
                    }
                    attaquant.Attaquer(cible, multiplicateurDegats);
                }

                initiatives[attaquant] = 0;
                tours++;

                // Indiquer qu'on travaille encore si le combat s'etire
                if (tours > 50 && tours % 25 == 0)
                {
                    string resume = ConstruireResume(tours, combattants, logger);
                    _ui.Afficher(resume);
                    logger.Log(new CombatDataItem
                    {
                        Event = TypeEvenement.Autre,
                        Message = resume
                    });
                }

                if (tours % toursParPalier == 0)
                {
                    palierDegats++;
                    Combattant.GlobalDamageMultiplier = 1f + palierDegats * stepPct;
                    Combattant.GlobalDodgeMultiplier = MathF.Max(0.05f, 1f - palierDegats * stepPct);

                    if (tours >= 100)
                    {
                        palierHard++;
                        Combattant.GlobalHealMultiplier = MathF.Max(0.05f, 1f - palierHard * stepHard);
                        Combattant.GlobalDodgeMultiplier = MathF.Max(0.05f, Combattant.GlobalDodgeMultiplier - stepHard);
                        foreach (var c in combattants.Where(c => c.EstVivant()))
                        {
                            var perte = MathF.Max(1f, c.PVMax * stepPct);
                            c.SubirDegatsSansEffets(perte);
                        }
                    }

                    string msg = $"L'etau se resserre (palier degats {palierDegats}) : +{palierDegats * 10}% degats, -{palierDegats * 10}% esquive";
                    if (tours >= 100)
                    {
                        msg += $", -{palierHard * 10}% soins supl., -1% PV actuels";
                    }
                    logger.Log(new CombatDataItem
                    {
                        Event = TypeEvenement.Autre,
                        Message = msg
                    });

                    // Resume detaille a chaque palier
                    string resumePalier = ConstruireResume(tours, combattants, logger);
                    logger.Log(new CombatDataItem
                    {
                        Event = TypeEvenement.Autre,
                        Message = resumePalier
                    });
                }

                // Ralentir un peu l'affichage apres quelques tours pour lisibilite
                if (tours >= 5 && tours < 15)
                {
                    Thread.Sleep(200);
                }
                else if (tours >= 15)
                {
                    Thread.Sleep(100);
                }
            }
            logger.AfficherAvecDelais(_ui);
            Thread.Sleep(1000);

            // Fin de combat pour les artefacts et autres effets
            foreach (var combattant in combattants)
            {
                combattant.FinCombat();
            }
            // Reset des multiplicateurs globaux pour les prochains combats
            Combattant.GlobalDamageMultiplier = 1f;
            Combattant.GlobalHealMultiplier = 1f;
            Combattant.GlobalDodgeMultiplier = 1f;

            // Fin du combat
            if (!_joueur.EstVivant())
            {
                _ui.Afficher("Vous avez perdu le combat.");
                ResultatCombat = ResultatCombat.Lose;
            }
            else if (!_monstres.Any(m => m.EstVivant()))
            {
                _ui.Afficher("Vous avez gagne le combat !");
                ResultatCombat = ResultatCombat.Win;
            }
            else
            {
                _ui.Afficher("Fin du combat : limite de tours atteinte (defaite automatique).");
                ResultatCombat = ResultatCombat.Lose;
            }
            CommandHandler.ClearOpponentsProvider();
        }

        private string ConstruireResume(int tour, List<Combattant> combattants, CombatLogger logger)
        {
            string hp = string.Join(" | ", combattants.Where(c => c.EstVivant())
                .Select(c => $"{c.Nom}:{Math.Round(c.PV, 2)}/{Math.Round(c.PVMax, 2)}"));
            string arts = string.Join(" | ", combattants.Where(c => c.EstVivant())
                .Select(c => $"{c.Nom}:{(c.Artefacts.Count == 0 ? "aucun" : string.Join(", ", c.Artefacts.Select(a => a.Nom)))}"));
            string mults = $"DMG x{Combattant.GlobalDamageMultiplier:F2}, HEAL x{Combattant.GlobalHealMultiplier:F2}, DODGE x{Combattant.GlobalDodgeMultiplier:F2}";
            string last = logger.LastAction?.Message ?? "n/a";
            return $"[Resume T{tour}] PV: {hp} | Artefacts: {arts} | Mult: {mults} | Derniere action: {last}";
        }

    }
}


