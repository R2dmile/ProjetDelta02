using ProjetDelta.Systemes;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjetDelta.UI
{
    class UIText2 : IUI
    {
        private const int MaxLines = 20;

        private class ConsoleSegment
        {
            public string Text { get; }
            public string Color { get; }

            public ConsoleSegment(string text, string color)
            {
                Text = text;
                Color = color;
            }
        }

        private static List<List<ConsoleSegment>> consoleLines = new List<List<ConsoleSegment>>();
        private static List<List<ConsoleSegment>> previousConsoleLines = new List<List<ConsoleSegment>>();
        private static List<ConsoleSegment> currentLineSegments = new List<ConsoleSegment>();

        public UIText2() { }

        public void Afficher(string text)
        {
            AddLine(new List<ConsoleSegment> { new ConsoleSegment(text, "gray") });
            RefreshConsole();
        }

        public void AffichageBasicInteractif(string text, string couleur)
        {
            currentLineSegments.Add(new ConsoleSegment(text, couleur));
        }

        public void TerminerLigne()
        {
            if (currentLineSegments.Count > 0)
            {
                AddLine(currentLineSegments);
                currentLineSegments = new List<ConsoleSegment>();
                RefreshConsole();
            }
        }

        private void AddLine(List<ConsoleSegment> segments)
        {
            if (consoleLines.Count >= MaxLines)
            {
                consoleLines.RemoveAt(0);
            }
            consoleLines.Add(segments);
        }

        private void RefreshConsole()
        {
            var sb = new StringBuilder();

            for (int i = 0; i < consoleLines.Count; i++)
            {
                if (i >= previousConsoleLines.Count || !AreLinesEqual(consoleLines[i], previousConsoleLines[i]))
                {
                    foreach (var segment in consoleLines[i])
                    {
                        Console.ForegroundColor = segment.Color.ToLowerInvariant() switch
                        {
                            "red" => ConsoleColor.Red,
                            "green" => ConsoleColor.Green,
                            "yellow" => ConsoleColor.Yellow,
                            "white" => ConsoleColor.White,
                            "cyan" => ConsoleColor.Cyan,
                            "teal" => ConsoleColor.Cyan,
                            "special" => ConsoleColor.Cyan,
                            "blue" => ConsoleColor.Blue,
                            "magenta" => ConsoleColor.Magenta,
                            "purple" => ConsoleColor.DarkMagenta,
                            "darkmagenta" => ConsoleColor.DarkMagenta,
                            "gray" => ConsoleColor.Gray,
                            "gold" => ConsoleColor.DarkYellow,
                            "darkyellow" => ConsoleColor.DarkYellow,
                            _ => ConsoleColor.Gray,
                        };
                        Console.Write(segment.Text);
                        Console.ResetColor();
                    }
                    Console.WriteLine();
                }
            }

            previousConsoleLines = new List<List<ConsoleSegment>>(consoleLines);
        }

        private bool AreLinesEqual(List<ConsoleSegment> line1, List<ConsoleSegment> line2)
        {
            if (line1.Count != line2.Count) return false;

            for (int i = 0; i < line1.Count; i++)
            {
                if (line1[i].Text != line2[i].Text || line1[i].Color != line2[i].Color)
                    return false;
            }

            return true;
        }

        public void AfficherNonSelectionnable(string text)
        {
            AddLine(new List<ConsoleSegment> { new ConsoleSegment(text, "red") });
            RefreshConsole();
        }

        public void Degats(string text)
        {
            AddLine(new List<ConsoleSegment> { new ConsoleSegment(text, "red") });
            RefreshConsole();
        }

        public void Effet(string text)
        {
            AddLine(new List<ConsoleSegment> { new ConsoleSegment(text, "green") });
            RefreshConsole();
        }

        public string QuestionReponse(string question = "")
        {
            Afficher(question);
            while (true)
            {
                var input = Console.ReadLine();
                return input ?? "";
            }
        }

        public void GoNext(string prompt = "Continuer")
        {
            var message = string.IsNullOrWhiteSpace(prompt) || prompt.Equals("Continuer", StringComparison.OrdinalIgnoreCase)
                ? "Appuie sur Entree pour continuer."
                : prompt;
            QuestionReponse(message);
        }

        public void UpdatePlayerState(UIPlayerState state)
        {
            // HUD avance non affiche sur cette UI
        }

        public (string, string) ChoisirOption(string titre, Dictionary<string, string> options)
        {
            bool choixValide = false;
            string selectionKey = "";
            string selectionValue = "";
            while (!choixValide)
            {
                Afficher(titre);
                foreach (var option in options)
                {
                    Afficher($"{option.Key}. {option.Value}");
                }
                string reponse = QuestionReponse("Votre choix :");
                if (options.ContainsKey(reponse))
                {
                    selectionKey = reponse;
                    selectionValue = options[reponse];
                    choixValide = true;
                }
                else
                {
                    Afficher("Choix invalide. Veuillez réessayer.");
                }
            }
            return (selectionKey, selectionValue);
        }
        // Méthode qui demande une confirmation après le choix
        public string ChoisirOptionAvecConfirmation(string titre, Dictionary<string, string> options)
        {
            string selection = "";
            string confirmation = "";
            do
            {
                selection = ChoisirOption(titre, options).Item2;
                confirmation = QuestionReponse($"Vous avez choisi {selection}. Confirmez-vous ? (oui/non)");
            } while (confirmation.ToLower() != "oui");
            return selection;
        }
        public T ChoisirOption<T>(string titre, List<T> options, Func<T, string> optionToString)
        {
            bool choixValide = false;
            T selection = default(T);
            while (!choixValide)
            {
                Afficher(titre);
                for (int i = 0; i < options.Count; i++)
                {
                    Afficher($"{i + 1}. {optionToString(options[i])}");
                }
                string reponse = QuestionReponse("Votre choix (numéro) :");
                if (int.TryParse(reponse, out int index) && index > 0 && index <= options.Count)
                {
                    selection = options[index - 1];
                    choixValide = true;
                }
                else
                {
                    Afficher("Choix invalide. Veuillez réessayer.");
                }
            }
            return selection;
        }

        public T ChoisirOptionAvecConfirmation<T>(string titre, List<T> options, Func<T, string> optionToString)
        {
            T selection;
            string confirmation = "";
            do
            {
                selection = ChoisirOption(titre, options, optionToString);
                confirmation = QuestionReponse($"Vous avez choisi : {optionToString(selection)}. Confirmez-vous ? (oui/non)");
            } while (confirmation.ToLower() != "oui");
            return selection;
        }
        public (string, string) ChoisirOption(string titre, Dictionary<string, string> options, List<string> nonSelectableKeys)
        {
            bool choixValide = false;
            string selectionValue = "";
            string selectionKey = "";
            while (!choixValide)
            {
                Afficher(titre);
                foreach (var option in options)
                {
                    // Si l'option est non sélectionnable, on utilise AfficherNonSelectionnable
                    if (nonSelectableKeys.Contains(option.Key))
                        AfficherNonSelectionnable($"{option.Key}. {option.Value}");
                    else
                        Afficher($"{option.Key}. {option.Value}");
                }
                string reponse = QuestionReponse("Votre choix :");
                if (options.ContainsKey(reponse))
                {
                    // Vérifie si l'option choisie est non sélectionnable
                    if (nonSelectableKeys.Contains(reponse))
                    {
                        Afficher("Cette option n'est pas sélectionnable. Veuillez réessayer.");
                    }
                    else
                    {
                        selectionValue = options[reponse];
                        selectionKey = reponse;
                        choixValide = true;
                    }
                }
                else
                {
                    Afficher("Choix invalide. Veuillez réessayer.");
                }
            }
            return (selectionKey, selectionValue);
        }
    }
}
