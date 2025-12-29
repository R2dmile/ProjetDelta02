using ProjetDelta.Data.DataClasses;
using ProjetDelta.Logger;
using ProjetDelta.Player;
using ProjetDelta.UI;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ProjetDelta.Donjon.Monstres
{
    public class Monstre : Combattant
    {
        private readonly int _tier;
        public int Tier => _tier;

        public Monstre(string nom, string couleur, Attributs attributs, Stats stats, int tier = 1)
        {
            _nom = nom;
            _couleur = couleur;
            _attributs = attributs;
            _stats = stats;
            _stats.AjusterParAttributs(_attributs);
            _tier = tier <= 0 ? 1 : tier;
        }

        public string MessageDerniereAttaque()
        {
            return $"{Nom} attaque et inflige {_stats.Attaque} dégâts.";
        }

        public override string ToString()
        {
            return $"{Nom} (Vie: {_stats.VieMax}, Attaque: {_stats.Attaque}, Vitesse: {_stats.Vitesse})";
        }
    }
}
