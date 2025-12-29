using ProjetDelta.Choix;
using ProjetDelta.Data.DataClasses;
using ProjetDelta.Player;
using ProjetDelta.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetDelta.Donjon
{
    /// <summary>
    /// old code
    /// </summary>
    public class NarrationManager
    {
        private List<Section> _sections;
        private Section? _sectionActuelle;
        private readonly IUI _ui;
        private readonly Joueur _joueur;
        public NarrationManager(List<Section> sections, Joueur joueur, IUI ui, string room = "start")
        {
            _sections = sections;
            _sectionActuelle = sections.Find(s => s.Id == room);
            _ui = ui;
            _joueur = joueur;
        }

        public void Demarrer()
        {
            while (true)
            {
                Console.Clear();
                _sectionActuelle.Afficher(_ui);

                Console.WriteLine("\nQue voulez-vous faire ? (Entrez le numéro du choix)");

                if (!int.TryParse(Console.ReadLine(), out int choix) || choix < 1 || choix > _sectionActuelle.Choices.Count)
                {
                    Console.WriteLine("Choix invalide, essayez encore.");
                    continue;
                }

                var choixSelectionne = _sectionActuelle.Choices[choix - 1];

                //if (choixSelectionne.IsCombat)
                //{
                //    bool victoire = DemarrerCombat(choixSelectionne.Monstre);
                //    _sectionActuelle = _sections.Find(s => s.Id == (victoire ? choixSelectionne.WinSection : choixSelectionne.LoseSection));
                //}
                //else
                //{
                //    _sectionActuelle = _sections.Find(s => s.Id == choixSelectionne.NextSection);
                //}
            }
        }

        private bool DemarrerCombat(string nomMonstre)
        {
            Console.Clear();
            Console.WriteLine($"Vous affrontez {nomMonstre} !");

            // Simule un combat 
            Random rand = new Random();
            bool victoire = rand.Next(2) == 0;

            if (victoire)
            {
                Console.WriteLine("Vous avez vaincu le monstre !");
            }
            else
            {
                Console.WriteLine("Vous avez été vaincu par le monstre...");
            }

            Console.WriteLine("Appuyez sur Entrée pour continuer.");
            Console.ReadLine();

            return victoire;
        }
    }
}
