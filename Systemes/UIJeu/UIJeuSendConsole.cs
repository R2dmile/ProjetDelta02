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
        private bool TryLogSegments(CombatDataItem combatData)
        {
            if (_ui is not IUILogTarget logTarget)
                return false;

            List<UILogSegment> segments = new List<UILogSegment>();
            switch (combatData.Event)
            {
                case TypeEvenement.Attaque:
                    segments.Add(new UILogSegment("\n", "white"));
                    segments.Add(new UILogSegment(combatData.NomAttaquant, combatData.CouleurAtt));
                    segments.Add(new UILogSegment(" attaque pour ", "white"));
                    segments.Add(new UILogSegment(FormatNombre(combatData.DegatInflige), combatData.Type));
                    segments.Add(new UILogSegment(" degats a ", "white"));
                    segments.Add(new UILogSegment(combatData.NomRecevant, combatData.CouleurDef));
                    break;
                case TypeEvenement.Degats:
                    segments.Add(new UILogSegment("\n", "white"));
                    segments.Add(new UILogSegment(combatData.NomRecevant, combatData.CouleurDef));
                    segments.Add(new UILogSegment(" a recu ", "white"));
                    segments.Add(new UILogSegment(FormatNombre(combatData.DegatSubi), "red"));
                    segments.Add(new UILogSegment(" degats. PV restants:", "white"));
                    segments.Add(new UILogSegment($"{combatData.HpCourant}/{combatData.HpTotal}", "white"));
                    break;
                case TypeEvenement.Artefact:
                    var artifactColor = string.IsNullOrWhiteSpace(combatData.Type) ? "green" : combatData.Type;
                    segments.Add(new UILogSegment("\n", "white"));
                    segments.Add(new UILogSegment(combatData.NomAttaquant, artifactColor, Artefact: combatData.NomAttaquant, Owner: combatData.NomRecevant));
                    segments.Add(new UILogSegment(":", "white"));
                    segments.Add(new UILogSegment(combatData.Message ?? combatData.NomRecevant, artifactColor, Artefact: combatData.NomAttaquant, Owner: combatData.NomRecevant));
                    if (!string.IsNullOrWhiteSpace(combatData.HpCourant) && !string.IsNullOrWhiteSpace(combatData.HpTotal))
                    {
                        segments.Add(new UILogSegment($" ({combatData.HpCourant}/{combatData.HpTotal}) pv", artifactColor));
                    }
                    break;
                case TypeEvenement.Autre:
                    segments.Add(new UILogSegment("\n", "white"));
                    segments.Add(new UILogSegment(combatData.Message ?? "", "yellow"));
                    break;
                default:
                    return false;
            }

            logTarget.AddLogLine(segments);
            return true;
        }
        public void AfficherCombatItem(CombatDataItem combatData)
        {
            if (TryLogSegments(combatData)) return;

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
                var artifactColor = string.IsNullOrWhiteSpace(combatData.Type) ? "green" : combatData.Type;
                _ui.AffichageBasicInteractif("\n", "white");
                _ui.AffichageBasicInteractif($"{combatData.NomAttaquant}", artifactColor);
                _ui.AffichageBasicInteractif(":", "white");
                _ui.AffichageBasicInteractif($"{combatData.Message ?? combatData.NomRecevant}", artifactColor);
                if (!string.IsNullOrWhiteSpace(combatData.HpCourant) && !string.IsNullOrWhiteSpace(combatData.HpTotal))
                {
                    _ui.AffichageBasicInteractif($" ({combatData.HpCourant}/{combatData.HpTotal}) pv", artifactColor);
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
