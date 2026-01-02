using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;

namespace ProjetDelta.UI
{
    // Implémentation IUI basée sur Spectre.Console pour un rendu console amélioré
    public class SpectreUI : IUI
    {
        private static readonly IAnsiConsole ConsoleAnsi = AnsiConsole.Create(new AnsiConsoleSettings
        {
            // Detect pour éviter d'afficher des codes ANSI bruts sur les consoles non compatibles
            Ansi = AnsiSupport.Detect,
            ColorSystem = ColorSystemSupport.Detect,
            Interactive = InteractionSupport.Yes
        });
        private static readonly bool SupportsAnsi = ConsoleAnsi.Profile.Capabilities.Ansi;
        private static Func<string, bool>? _commandHook;

        public static void SetCommandHook(Func<string, bool> hook)
        {
            _commandHook = hook;
        }

        private static string Safe(string? text) => Markup.Escape(text ?? "");

        private static string ColorTag(string couleur) => couleur.ToLowerInvariant() switch
        {
            "red" => "red",
            "green" => "green",
            "yellow" => "yellow",
            "gray" => "grey",
            "white" => "white",
            "cyan" or "teal" or "special" => "cyan",
            "blue" => "blue",
            "magenta" => "magenta",
            "purple" or "darkmagenta" => "magenta",
            "lime" => "green",
            "gold" or "darkyellow" => "yellow",
            _ => "white"
        };

        private void Print(string text, string color = "white", bool bold = false, bool newline = true)
        {
            if (SupportsAnsi)
            {
                var prefix = bold ? "bold " : "";
                var content = $" [{prefix}{color}]{Safe(text)}[/]";
                if (newline) ConsoleAnsi.MarkupLine(content);
                else ConsoleAnsi.Markup(content);
            }
            else
            {
                if (newline) Console.WriteLine(text);
                else Console.Write(text);
            }
            if (newline) ConsoleAnsi.WriteLine();
        }

        public void Afficher(string text) => Print(text, "white", false);

        public void AffichageBasicInteractif(string text, string couleur) => Print(text, ColorTag(couleur), false, newline: false);

        public void AfficherNonSelectionnable(string text) => Print(text, "grey", false);

        public void Degats(string text) => Print(text, "red", true);

        public void Effet(string text) => Print(text, "cyan", false);

        public string QuestionReponse(string question = "")
        {
            if (!SupportsAnsi)
            {
                while (true)
                {
                    var prefix = string.IsNullOrWhiteSpace(question) ? ">" : question;
                    Console.Write($"{prefix} ");
                    var inputRaw = Console.ReadLine();
                    if (!string.IsNullOrEmpty(inputRaw) && _commandHook != null && _commandHook.Invoke(inputRaw))
                        continue;
                    return inputRaw ?? string.Empty;
                }
            }

            while (true)
            {
                var prefix = string.IsNullOrWhiteSpace(question) ? ">" : Safe(question);
                var prompt = new TextPrompt<string>($" [yellow]{prefix}[/] ")
                    .AllowEmpty()
                    .PromptStyle("yellow");
                var input = ConsoleAnsi.Prompt(prompt);
                if (!string.IsNullOrEmpty(input) && _commandHook != null && _commandHook.Invoke(input))
                    continue;
                return input ?? "";
            }
        }

        public (string, string) ChoisirOption(string titre, Dictionary<string, string> options)
        {
            var optionsUpper = options.ToDictionary(k => k.Key.ToUpperInvariant(), v => v.Value);
            if (optionsUpper.Count == 0)
            {
                Afficher("Aucune option disponible.");
                return (string.Empty, string.Empty);
            }

            while (true)
            {
                Print($"\n{titre}", "cyan", true);
                foreach (var option in options)
                {
                    if (SupportsAnsi)
                        ConsoleAnsi.MarkupLine($" [white]{option.Key}[/]. {Safe(option.Value)}");
                    else
                        Console.WriteLine($"{option.Key}. {option.Value}");
                }
                var rep = QuestionReponse("Votre choix :").ToUpperInvariant();
                if (optionsUpper.TryGetValue(rep, out var val))
                {
                    return (rep, val);
                }
                Afficher("Choix invalide. Veuillez reessayer.");
            }
        }

        public (string, string) ChoisirOption(string titre, Dictionary<string, string> options, List<string> nonSelectableKeys)
        {
            var optionsUpper = options.ToDictionary(k => k.Key.ToUpperInvariant(), v => v.Value);
            var nonSelectableUpper = nonSelectableKeys.Select(k => k.ToUpperInvariant()).ToHashSet();
            if (optionsUpper.Count == 0)
            {
                Afficher("Aucune option disponible.");
                return (string.Empty, string.Empty);
            }
            while (true)
            {
                Print($"\n{titre}", "cyan", true);
                foreach (var option in options)
                {
                    if (nonSelectableUpper.Contains(option.Key.ToUpperInvariant()))
                        Print($"{option.Key}. {option.Value}", "red", false);
                    else
                        Print($"{option.Key}. {option.Value}", "white", false);
                }
                var rep = QuestionReponse("Votre choix :").ToUpperInvariant();
                if (!optionsUpper.ContainsKey(rep))
                {
                    Afficher("Choix invalide. Veuillez reessayer.");
                    continue;
                }
                if (nonSelectableUpper.Contains(rep))
                {
                    Afficher("Cette option n'est pas selectionnable. Veuillez reessayer.");
                    continue;
                }
                return (rep, optionsUpper[rep]);
            }
        }

        public string ChoisirOptionAvecConfirmation(string titre, Dictionary<string, string> options)
        {
            string selection;
            do
            {
                selection = ChoisirOption(titre, options).Item2;
            } while (!QuestionReponse($"Vous avez choisi {selection}. Confirmez-vous ? (oui/non)").Equals("oui", StringComparison.OrdinalIgnoreCase));
            return selection;
        }

        public T ChoisirOption<T>(string titre, List<T> options, Func<T, string> optionToString)
        {
            if (options.Count == 0)
            {
                Afficher("Aucune option disponible.");
                return default!;
            }
            while (true)
            {
                Afficher(titre);
                for (int i = 0; i < options.Count; i++)
                {
                    Afficher($"{i + 1}. {optionToString(options[i])}");
                }
                var rep = QuestionReponse("Votre choix (numero) :");
                if (int.TryParse(rep, out var idx) && idx > 0 && idx <= options.Count)
                    return options[idx - 1];
                Afficher("Choix invalide. Veuillez reessayer.");
            }
        }

        public T ChoisirOptionAvecConfirmation<T>(string titre, List<T> options, Func<T, string> optionToString)
        {
            T selection;
            do
            {
                selection = ChoisirOption(titre, options, optionToString);
            } while (!QuestionReponse($"Vous avez choisi : {optionToString(selection)}. Confirmez-vous ? (oui/non)").Equals("oui", StringComparison.OrdinalIgnoreCase));
            return selection;
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
            // Console UI n'affiche pas le HUD detaille
        }

        public void TerminerLigne()
        {
            AnsiConsole.WriteLine();
        }

        // Petite barre/progress pour les chargements rapides
        public void QuickProgress(string message, int steps = 20, int delayMs = 15)
        {
            AnsiConsole.Progress()
                .HideCompleted(true)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn()
                })
                .Start(ctx =>
                {
                    var task = ctx.AddTask($"[yellow]{Safe(message)}[/]", maxValue: steps);
                    for (int i = 0; i < steps; i++)
                    {
                        task.Increment(1);
                        System.Threading.Thread.Sleep(delayMs);
                    }
                });
            AnsiConsole.WriteLine();
        }
    }
}
