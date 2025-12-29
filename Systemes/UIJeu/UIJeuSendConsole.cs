using ProjetDelta.Logger;
using ProjetDelta.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetDelta.Systemes.UIJeu
{
    //classe non existante si on change a un autre type de ui
    internal class UIJeuSendConsole
    {
        private IUI _ui;
        public UIJeuSendConsole(IUI ui) { _ui = ui; }
        private static string FormatNombre(float value) => value.ToString("0.##");
        public void AfficherCombatItem(CombatDataItem combatData)
        {
            if (combatData.Event == TypeEvenement.Attaque)
            {
                _ui.AffichageBasicInteractif("\n", "white");
                _ui.AffichageBasicInteractif($"{combatData.NomAttaquant}", $"{combatData.CouleurAtt}");
                _ui.AffichageBasicInteractif(" attaque pour ", "white");
                _ui.AffichageBasicInteractif($"{FormatNombre(combatData.DegatInflige)}", $"{combatData.Type}");
                _ui.AffichageBasicInteractif(" degats a ", "white");
                _ui.AffichageBasicInteractif($"{combatData.NomRecevant}", $"{combatData.CouleurDef}");
                _ui.TerminerLigne();
            }
            else if (combatData.Event == TypeEvenement.Degats)
            {
                _ui.AffichageBasicInteractif("\n", "white");
                _ui.AffichageBasicInteractif($"{combatData.NomRecevant}", $"{combatData.CouleurDef}");
                _ui.AffichageBasicInteractif(" a recu ", "white");
                _ui.AffichageBasicInteractif($"{FormatNombre(combatData.DegatSubi)}", "red");
                _ui.AffichageBasicInteractif(" degats. PV restants:", "white");
                _ui.AffichageBasicInteractif($"{combatData.HpCourant}/{combatData.HpTotal}", "white");
                _ui.TerminerLigne();
            }
            else if (combatData.Event == TypeEvenement.Artefact)
            {
                _ui.AffichageBasicInteractif("\n", "white");
                _ui.AffichageBasicInteractif($"{combatData.NomAttaquant}", "green");
                _ui.AffichageBasicInteractif(":", "white");
                _ui.AffichageBasicInteractif($"{combatData.Message ?? combatData.NomRecevant}", "white");
                if (!string.IsNullOrWhiteSpace(combatData.HpCourant) && !string.IsNullOrWhiteSpace(combatData.HpTotal))
                {
                    _ui.AffichageBasicInteractif($" ({combatData.HpCourant}/{combatData.HpTotal}) pv", "white");
                }
                _ui.TerminerLigne();
            }
            else if (combatData.Event == TypeEvenement.Autre)
            {
                _ui.AffichageBasicInteractif("\n", "white");
                _ui.AffichageBasicInteractif($"{combatData.Message}", "yellow");
                _ui.TerminerLigne();
            }

        }
    }
}
