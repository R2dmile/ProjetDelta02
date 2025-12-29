using ProjetDelta.Systemes;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ProjetDelta.UI
{
    class UIText : IUI
    {
        private static Func<string, bool>? _commandHook;

        public static void SetCommandHook(Func<string, bool> hook)
        {
            _commandHook = hook;
        }

        public UIText() { }

        public void Afficher(string text)
        {
            Console.WriteLine(" " + text + "\n");
        }

        public void AffichageBasicInteractif(string text, string couleur)
        {
            // Harmonise avec les couleurs utilisees pour les noms / raretes
            switch (couleur.ToLowerInvariant())
            {
                case "red":
                    Console.ForegroundColor = ConsoleColor.Red; break;
                case "green":
                    Console.ForegroundColor = ConsoleColor.DarkGreen; break;
                case "yellow":
                    Console.ForegroundColor = ConsoleColor.Yellow; break;
                case "gray":
                    Console.ForegroundColor = ConsoleColor.Gray; break;
                case "white":
                    Console.ForegroundColor = ConsoleColor.White; break;
                case "cyan":
                case "teal":
                case "special":
                    Console.ForegroundColor = ConsoleColor.Cyan; break;
                case "blue":
                    Console.ForegroundColor = ConsoleColor.Blue; break;
                case "magenta":
                    Console.ForegroundColor = ConsoleColor.Magenta; break;
                case "purple":
                case "darkmagenta":
                    Console.ForegroundColor = ConsoleColor.DarkMagenta; break;
                case "lime":
                    Console.ForegroundColor = ConsoleColor.Green; break;
                case "gold":
                case "darkyellow":
                    Console.ForegroundColor = ConsoleColor.DarkYellow; break;
                default:
                    Console.ResetColor(); break;
            }
            Console.Write(" " + text);
            Console.ResetColor();
        }

        public void AfficherNonSelectionnable(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" " + text + "\n");
            Console.ResetColor();
        }
        public void Degats(string text)
        { Console.WriteLine(" " + text + "\n"); }
        public void Effet(string text)
        { Console.WriteLine(" " + text + "\n"); }
        public string QuestionReponse(string question = "")
        {
            while (true)
            {
                Console.Write(" " + question + "\n");
                string input = Console.ReadLine();
                if (!string.IsNullOrEmpty(input) && _commandHook != null && _commandHook.Invoke(input))
                {
                    // commande traitee, on relance la question
                    continue;
                }
                return input ?? "";
            }
        }
        //a rajouter choix bloques
        public (string, string) ChoisirOption(string titre, Dictionary<string, string> options)
        {
            bool choixValide = false;
            string selectionKey = "";
            string selectionValue = "";
            var optionsUpper = options.ToDictionary(k => k.Key.ToUpperInvariant(), v => v.Value);
            if (optionsUpper.Count == 0)
            {
                Afficher("Aucune option disponible.");
                return (string.Empty, string.Empty);
            }
            while (!choixValide)
            {
                Afficher(titre);
                foreach (var option in options)
                {
                    Afficher($"{option.Key}. {option.Value}");
                }
                string reponse = QuestionReponse("Votre choix :");
                var rep = reponse.ToUpperInvariant();
                if (optionsUpper.ContainsKey(rep))
                {
                    selectionKey = rep;
                    selectionValue = optionsUpper[rep];
                    choixValide = true;
                }
                else
                {
                    Afficher("Choix invalide. Veuillez reessayer.");
                }
            }
            return (selectionKey, selectionValue);
        }

        public (string, string) ChoisirOption(string titre, Dictionary<string, string> options, List<string> nonSelectableKeys)
        {
            bool choixValide = false;
            string selectionValue = "";
            string selectionKey = "";
            var optionsUpper = options.ToDictionary(k => k.Key.ToUpperInvariant(), v => v.Value);
            var nonSelectableUpper = nonSelectableKeys.Select(k => k.ToUpperInvariant()).ToHashSet();
            if (optionsUpper.Count == 0)
            {
                Afficher("Aucune option disponible.");
                return (string.Empty, string.Empty);
            }
            while (!choixValide)
            {
                Afficher(titre);
                foreach (var option in options)
                {
                    // Si l'option est non selectionnable, on utilise AfficherNonSelectionnable
                    if (nonSelectableUpper.Contains(option.Key.ToUpperInvariant()))
                        AfficherNonSelectionnable($"{option.Key}. {option.Value}");
                    else
                        Afficher($"{option.Key}. {option.Value}");
                }
                string reponse = QuestionReponse("Votre choix :");
                var rep = reponse.ToUpperInvariant();
                if (optionsUpper.ContainsKey(rep))
                {
                    // Verifie si l'option choisie est non selectionnable
                    if (nonSelectableUpper.Contains(rep))
                    {
                        Afficher("Cette option n'est pas selectionnable. Veuillez reessayer.");
                    }
                    else
                    {
                        selectionValue = optionsUpper[rep];
                        selectionKey = rep;
                        choixValide = true;
                    }
                }
                else
                {
                    Afficher("Choix invalide. Veuillez reessayer.");
                }
            }
            return (selectionKey, selectionValue);
        }

        // Methode qui demande une confirmation apres le choix
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
                string reponse = QuestionReponse("Votre choix (numero) :");
                if (int.TryParse(reponse, out int index) && index > 0 && index <= options.Count)
                {
                    selection = options[index - 1];
                    choixValide = true;
                }
                else
                {
                    Afficher("Choix invalide. Veuillez reessayer.");
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

        public void TerminerLigne()
        {
            Console.WriteLine("");
        }
    }
}
