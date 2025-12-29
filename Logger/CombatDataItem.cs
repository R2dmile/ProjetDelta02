using ProjetDelta.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetDelta.Logger
{
    public enum TypeEvenement
    {
        Attaque,
        Degats,
        Soin,
        Artefact,
        Buff,
        Debuff,
        Autre
    }
    public class CombatDataItem
    {
        public TypeEvenement Event { get; set; }
        public string NomAttaquant { get; set; }
        public float DegatInflige { get; set; }
        public string Type { get; set; } = "red";
        public string NomRecevant { get; set; }
        public float DegatSubi { get; set; }
        public string CouleurAtt { get; set; } = "white";
        public string CouleurDef { get; set; } = "white";
        public string HpTotal { get; set; }
        public string HpCourant { get; set; }
        public string? Message { get; set; }

        public CombatDataItem()
        {

        }

    }
}
