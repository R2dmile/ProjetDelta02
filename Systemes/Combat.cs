using ProjetDelta.Donjon.Monstres;
using ProjetDelta.Logger;
using ProjetDelta.Player;
using ProjetDelta.Systemes.UIJeu;
using ProjetDelta.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
            const int seuilSuddenDeath = 80;
            const float incrementSuddenDeath = 0.1f;

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

                // Détermination du combattant avec la plus haute initiative
                var attaquant = initiatives
                    .Where(i => i.Key.EstVivant())
                    .OrderByDescending(i => i.Value)
                    .First()
                    .Key;

                // Détermination de la cible
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
                    // Pour éviter les combats infinis : on augmente progressivement les dégâts globaux.
                    float multiplicateurDegats = 1f;
                    if (nbAttaques > seuilSuddenDeath)
                    {
                        multiplicateurDegats *= 1f + (nbAttaques - seuilSuddenDeath) * incrementSuddenDeath;
                    }
                    logger.Log(new CombatDataItem
                    {
                        Event = TypeEvenement.Autre,
                        Message = $"--- Debut du tour {nbAttaques} ({attaquant.Nom}) ---"
                    });
                    attaquant.DebutTour();
                    attaquant.Attaquer(cible, multiplicateurDegats);
                }

                initiatives[attaquant] = 0;
                tours++;

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

            // Fin du combat
            if (!_joueur.EstVivant())
            {
                _ui.Afficher("Vous avez perdu le combat.");
                ResultatCombat = ResultatCombat.Lose;
            }
            else if (!_monstres.Any(m => m.EstVivant()))
            {
                _ui.Afficher("Vous avez gagné le combat !");
                ResultatCombat = ResultatCombat.Win;
            }
            else
            {
                float pvMonstres = _monstres.Where(m => m.EstVivant()).Sum(m => m.PV);
                _ui.Afficher("Fin du combat : limite de tours atteinte.");
                if (_joueur.PV >= pvMonstres)
                {
                    _ui.Afficher("Victoire par décision (plus de PV restants).");
                    ResultatCombat = ResultatCombat.Win;
                }
                else
                {
                    _ui.Afficher("Défaite par décision (moins de PV restants).");
                    ResultatCombat = ResultatCombat.Lose;
                }
            }
                

        }
        


    }
}
