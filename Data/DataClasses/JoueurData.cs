using ProjetDelta.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ProjetDelta.Data.DataLoader;
using ProjetDelta.Artifacts;

namespace ProjetDelta.Data.DataClasses
{
    public class JoueurData
    {
        public string Nom { get; set; } = string.Empty;
        public int Niveau { get; set; }
        public int Exp { get; set; }
        public AttributsDto Attributs { get; set; } = new AttributsDto();
        public string BackgroundNom { get; set; } = string.Empty;
        public string ClasseNom { get; set; } = string.Empty;
        public Stats Stats { get; set; } = new Stats();
        public List<string> Artefacts { get; set; } = new();
        public int Argent { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public bool Mort { get; set; } = false;
        public int Etage { get; set; } = 1;
        public string Difficulte { get; set; } = "Normal";
        public int DifficulteLevel { get; set; } = 1;
        public int PointsNonDepenses { get; set; } = 0;
        public Joueur ToJoueur()
        {
            var j = new Joueur(Nom, BackgroundNom, ClasseNom, Niveau, Exp, Attributs.ToAttributs(), Stats, Argent, Etage, Difficulte, PointsNonDepenses, appliquerAttributs: false);
            j.RoomName = RoomName;
            if (Artefacts != null && Artefacts.Count > 0)
            {
                foreach (var nom in Artefacts)
                {
                    if (string.IsNullOrWhiteSpace(nom)) continue;
                    var art = ArtefactLoader.CreerInstance(nom);
                    if (art != null) j.EquipArtefact(art, applyOnEquip: false);
                }
            }
            return j;
        }
    }
}
