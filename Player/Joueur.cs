using ProjetDelta.Artifacts;
using ProjetDelta.Data.DataClasses;
using ProjetDelta.Donjon.Monstres;
using ProjetDelta.Logger;
using ProjetDelta.UI;

namespace ProjetDelta.Player
{
    public class Joueur : Combattant
    {
        private int _niveau;
        private int _exp;
        private int _pointsNonDepenses = 0;
        private string _backgroundNom;
        private string _classeNom;
        private int _argent;
        private int _etage = 1;
        private string _difficulte = "Normal";
        private int _difficulteLevel = 1;

        public int Argent => _argent;
        public int Etage => _etage;
        public string Difficulte => _difficulte;
        public int DifficulteLevel => _difficulteLevel;
        public int Niveau => _niveau;
        public int Exp => _exp;
        public int PointsNonDepenses => _pointsNonDepenses;
        public string ClasseNom => _classeNom;
        public string BackgroundNom => _backgroundNom;

        public string RoomName { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"Nom: {_nom}, Niveau: {_niveau}";
        }

        public Joueur(string nom, Background background, Classe classe, Stats stats, int niveau = 1, int exp = 0, int argent = 0, int etage = 1, string difficulte = "Normal", int pointsNonDepenses = 0)
        {
            _nom = nom;
            _niveau = niveau;
            _exp = exp;
            _pointsNonDepenses = pointsNonDepenses;
            _argent = argent;
            _etage = etage;
            _difficulte = string.IsNullOrWhiteSpace(difficulte) ? "Normal" : difficulte;
            _difficulteLevel = DifficultyModifier.FromName(_difficulte).Level;
            _attributs = new Attributs();
            background.AdjustAttributes(_attributs);
            classe.AdjustAttributes(_attributs);
            _backgroundNom = background.Nom;
            _classeNom = classe.Nom;
            _stats = stats;
            _couleur = "lime";
            _stats.AjusterParAttributs(_attributs);
        }

        public Joueur(string nom, string backgroundNom, string classeNom, int niveau, int exp, Attributs attributs, Stats stats, int argent = 0, int etage = 1, string difficulte = "Normal", int pointsNonDepenses = 0)
        {
            _nom = nom;
            _niveau = niveau;
            _exp = exp;
            _pointsNonDepenses = pointsNonDepenses;
            _argent = argent;
            _etage = etage;
            _difficulte = string.IsNullOrWhiteSpace(difficulte) ? "Normal" : difficulte;
            _difficulteLevel = DifficultyModifier.FromName(_difficulte).Level;
            _attributs = attributs;
            _backgroundNom = backgroundNom;
            _classeNom = classeNom;
            _stats = stats;
            _couleur = "lime";
            _stats.AjusterParAttributs(_attributs);
        }

        public JoueurData ToData()
        {
            return new JoueurData
            {
                Nom = _nom,
                Niveau = _niveau,
                Exp = _exp,
                Attributs = _attributs.ToDto(),
                BackgroundNom = _backgroundNom,
                ClasseNom = _classeNom,
                Stats = _stats,
                Argent = _argent,
                PointsNonDepenses = _pointsNonDepenses,
                Artefacts = Artefacts.Select(a => a.Nom).ToList(),
                RoomName = RoomName,
                Mort = false,
                Etage = _etage,
                Difficulte = _difficulte,
                DifficulteLevel = _difficulteLevel
            };
        }

        public void AjouterArgent(int montant)
        {
            _argent += montant;
            if (_argent < 0) _argent = 0;
        }

        public bool DepenserArgent(int montant)
        {
            if (montant <= 0) return true;
            if (_argent < montant) return false;
            _argent -= montant;
            return true;
        }

        public void AvancerEtage()
        {
            _etage++;
            if (_etage < 1) _etage = 1;
            if (_etage > 5) _etage = 5; // plafond d'etages
        }

        public void SetDifficulte(string diff)
        {
            if (!string.IsNullOrWhiteSpace(diff))
            {
                _difficulte = diff;
                _difficulteLevel = DifficultyModifier.FromName(diff).Level;
            }
        }

        public void AppliquerMetaBonuses(float goldBonus, float lootBonus, float statBonusPct)
        {
            SetMetaBonuses(goldBonus, lootBonus);
            if (statBonusPct > 0)
            {
                var pvBoost = _stats.VieMax * statBonusPct;
                var atkBoost = _stats.Attaque * statBonusPct;
                var spdBoost = _stats.Vitesse * statBonusPct;
                _stats.AjouterBonus(pvBoost, atkBoost, spdBoost);
            }
        }

        private int XpPourNiveauSuivant()
        {
            // Courbe douce : base 40 puis +20 par niveau actuel
            return 40 + (_niveau - 1) * 20;
        }

        public void GagnerExperience(int montant, IUI? ui = null)
        {
            if (montant <= 0) return;
            int gain = (int)Math.Ceiling(montant * (1 + XpBonusPct));
            _exp += gain;
            ui?.Afficher($"XP gagnee : {gain} (total {_exp}/{XpPourNiveauSuivant()})");
            bool leveled = false;
            var required = XpPourNiveauSuivant();
            while (_exp >= required)
            {
                _exp -= required;
                _niveau++;
                _pointsNonDepenses += 2;
                leveled = true;
                OnLevelUpBonus(ui);
                ui?.Afficher($"Niveau {_niveau} atteint ! +2 points d'attributs a repartir.");
                required = XpPourNiveauSuivant();
            }
            if (leveled)
            {
                if (_pointsNonDepenses > 0)
                {
                    ui?.Afficher($"Points disponibles : {_pointsNonDepenses}. Utilise la commande 'levelup' pour les depenser quand tu veux.");
                }
            }
        }

        public void DistribuerPoints(IUI? ui)
        {
            if (ui == null) return;
            while (_pointsNonDepenses > 0)
            {
                ui.Afficher($"Points a attribuer : {_pointsNonDepenses}");
                var options = new Dictionary<string, string>
                {
                    {"1", "Force"},
                    {"2", "Constitution"},
                    {"3", "Dexterite"},
                    {"4", "Intelligence"},
                };
                var choix = ui.ChoisirOption("Choisir un attribut a augmenter", options).Item1;
                switch (choix)
                {
                    case "1":
                        _attributs.AddAttributes(new AttributsDto { Force = 1 });
                        _stats.AjouterBonus(0, 1, 0);
                        ui.Afficher("Force +1.");
                        break;
                    case "2":
                        _attributs.AddAttributes(new AttributsDto { Constitution = 1 });
                        _stats.AjouterBonus(1, 0, 0);
                        ui.Afficher("Constitution +1.");
                        break;
                    case "3":
                        _attributs.AddAttributes(new AttributsDto { Dex = 1 });
                        _stats.AjouterBonus(0, 0, 0.25f);
                        ui.Afficher("Dexterite +1.");
                        break;
                    case "4":
                        _attributs.AddAttributes(new AttributsDto { Intel = 1 });
                        ui.Afficher("Intelligence +1.");
                        break;
                    default:
                        ui.Afficher("Choix invalide.");
                        continue;
                }
                _pointsNonDepenses--;
                if (_pointsNonDepenses == 0)
                {
                    ui.Afficher($"Distribution terminee. Niveau: {_niveau}, PV: {_stats.VieCourante}/{_stats.VieMax}, ATK: {_stats.Attaque}, VIT: {_stats.Vitesse}");
                }
            }
        }

        // Ajustement apres level-up : regen partielle et gains plats
        private void OnLevelUpBonus(IUI? ui)
        {
            // +1 a chaque attribut
            _attributs.AddAttributes(new AttributsDto { Force = 1, Constitution = 1, Dex = 1, Intel = 1 });
            // Regen 20% PV max, pas de gains plats
            var soin = _stats.VieMax * 0.2f;
            Heal(soin);
            ui?.Afficher($"Level up bonus : +1 a tous les attributs, regen +{MathF.Round(soin,2)} PV.");
        }
    }
}
