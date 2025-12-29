using ProjetDelta.Data.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetDelta.Data.DataGenerators
{
    public class SectionGenerator
    {
        public static List<Section> GenerateSections()
        {
            List<Section> sections = new List<Section>();

            // Section 1: Start
            var startSection = new Section(
                "start",
                new List<string> {
                "Vous vous trouvez dans une forêt sombre.",
                "Les arbres s'étendent haut dans le ciel, bloquant la lumière du soleil.",
                "Des bruits étranges résonnent dans l'air, mais vous ne pouvez pas dire d'où ils viennent."
                },
                new List<Section.Choice>
                {
                new Section.Choice("Aller à gauche", "left_path"),
                new Section.Choice("Aller à droite", "right_path"),
                new Section.Choice("Avancer tout droit", "straight_path")
                }
            );
            sections.Add(startSection);

            // Section 2: Left Path
            var leftPathSection = new Section(
                "left_path",
                new List<string> {
                "Vous marchez sur un sentier étroit entouré d'arbres sinistres.",
                "Un hurlement vous fait frissonner."
                },
                new List<Section.Choice>
                {
                new Section.Choice("Explorer les buissons", "wolf_fight", true, "Loup"),
                new Section.Choice("Revenir sur vos pas", "start")
                }
            );
            sections.Add(leftPathSection);

            // Section 3: Right Path
            var rightPathSection = new Section(
                "right_path",
                new List<string> {
                "Vous suivez un chemin parsemé de pierres anciennes.",
                "Une clarté étrange émane d'une caverne en contrebas."
                },
                new List<Section.Choice>
                {
                new Section.Choice("Entrer dans la caverne", "cave_entrance"),
                new Section.Choice("Revenir sur vos pas", "start")
                }
            );
            sections.Add(rightPathSection);

            // Section 4: Straight Path (combat)
            var straightPathSection = new Section(
                "straight_path",
                new List<string> {
                "Vous avancez courageusement entre les arbres.",
                "Une créature surgit soudainement de l'ombre."
                },
                new List<Section.Choice>
                {
                new Section.Choice("Combattre la créature", "goblin_fight", true, "Gobelin")
                }
            );
            sections.Add(straightPathSection);

            // Ajoutez d'autres sections comme les combats, exploration, etc.

            return sections;
        }
    }
}
