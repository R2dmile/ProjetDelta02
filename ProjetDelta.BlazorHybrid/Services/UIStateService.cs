using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ProjetDelta.UI;

namespace ProjetDelta.BlazorHybrid.Services
{
    public class UIStateService
    {
        public record OptionVm(string Key, string Label, bool Disabled);
        public record MessageSegment(string Text, string Color, string? Artefact = null, string? Owner = null);
        public record MessageLine(IReadOnlyList<MessageSegment> Segments);

        public ObservableCollection<MessageLine> Messages { get; } = new();
        private const int MaxMessages = 120;
        public ObservableCollection<OptionVm> Options { get; } = new();
        public UIPlayerState? Player { get; private set; }
        public bool CommandsAvailable => Player != null;
        public string? Prompt { get; private set; }
        public bool AwaitingText { get; private set; }
        public bool AwaitingNext { get; private set; }
        public string NextLabel { get; private set; } = "Continuer";
        public bool BackEnabled { get; private set; }
        public string BackLabel { get; private set; } = "Retour";
        public bool IsLoading { get; private set; } = true;
        public bool CanReorder { get; private set; } = true;
        public bool DarkModeUnlocked { get; private set; } = false;
        public bool DarkModeEnabled { get; private set; } = false;
        public event Action? Changed;

        private TaskCompletionSource<string>? _choiceTcs;
        private readonly Queue<string> _injectedCommands = new();

        private void NotifyChanged()
        {
            App.Current?.Dispatcher.Dispatch(() => Changed?.Invoke());
        }

        private void ClearInteraction()
        {
            App.Current?.Dispatcher.Dispatch(() =>
            {
                AwaitingNext = false;
                AwaitingText = false;
                Prompt = null;
                BackEnabled = false;
                Options.Clear();
            });
        }

        public void AddLine(IEnumerable<MessageSegment> segments)
        {
            var line = new MessageLine(segments.ToList());
            App.Current?.Dispatcher.Dispatch(() =>
            {
                IsLoading = false;
                Messages.Add(line);
                // Trim pour limiter le lag UI : garde seulement les dernières lignes
                while (Messages.Count > MaxMessages)
                {
                    Messages.RemoveAt(0);
                }
            });
            NotifyChanged();
        }

        public void AddMessage(string msg, string color = "white")
        {
            AddLine(new[] { new MessageSegment(msg, color) });
        }

        public void SetPlayerState(UIPlayerState state)
        {
            // Conserver l'ordre actuel des artefacts si on recoit un etat equivalent (evite de perdre le reorder utilisateur)
            state = AlignArtefactOrder(state);
            App.Current?.Dispatcher.Dispatch(() =>
            {
                Player = state;
                CanReorder = state.EnVie;
                IsLoading = false;
            });
            NotifyChanged();
        }

        public void ApplyLocalReorder(int from, int to)
        {
            if (Player == null) return;
            var list = Player.Artefacts.ToList();
            if (from < 0 || to < 0 || from >= list.Count || to >= list.Count || from == to) return;
            var item = list[from];
            list.RemoveAt(from);
            list.Insert(to, item);
            var updated = Player with { Artefacts = list };
            App.Current?.Dispatcher.Dispatch(() =>
            {
                Player = updated;
            });
            NotifyChanged();
        }

        public void SetLoading(bool value)
        {
            App.Current?.Dispatcher.Dispatch(() =>
            {
                IsLoading = value;
            });
            NotifyChanged();
        }

        public void BeginChoice(string prompt, Dictionary<string, string> options, IEnumerable<string>? disabled = null)
        {
            var disabledSet = new HashSet<string>(disabled ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            ResetAwaiters();
            App.Current?.Dispatcher.Dispatch(() =>
            {
                Prompt = prompt;
                AwaitingText = false;
                AwaitingNext = false;
                BackEnabled = false;
                Options.Clear();
                foreach (var kv in options)
                {
                    var isDisabled = disabledSet.Contains(kv.Key);
                    Options.Add(new OptionVm(kv.Key, kv.Value, isDisabled));
                }
            });
            _choiceTcs = new TaskCompletionSource<string>();
            NotifyChanged();
        }

        public void BeginText(string prompt, bool allowBack = true, string? backLabel = null)
        {
            ResetAwaiters();
            App.Current?.Dispatcher.Dispatch(() =>
            {
                Prompt = prompt;
                AwaitingText = true;
                AwaitingNext = false;
                BackEnabled = allowBack;
                BackLabel = string.IsNullOrWhiteSpace(backLabel) ? "Retour" : backLabel;
                Options.Clear();
            });
            _choiceTcs = new TaskCompletionSource<string>();
            NotifyChanged();
        }

        public void BeginNext(string prompt, string buttonLabel = "Continuer")
        {
            ResetAwaiters();
            App.Current?.Dispatcher.Dispatch(() =>
            {
                Prompt = prompt;
                AwaitingText = false;
                AwaitingNext = true;
                NextLabel = string.IsNullOrWhiteSpace(buttonLabel) ? "Continuer" : buttonLabel;
                BackEnabled = false;
                Options.Clear();
            });
            _choiceTcs = new TaskCompletionSource<string>();
            NotifyChanged();
        }

        public void CompleteChoice(string key)
        {
            if (_choiceTcs == null) return;
            var opt = Options.FirstOrDefault(o => o.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (opt != null && !opt.Disabled)
            {
                ClearInteraction();
                _choiceTcs.TrySetResult(opt.Key);
                _choiceTcs = null;
            }
            NotifyChanged();
        }

        public void SubmitText(string text)
        {
            if (_choiceTcs == null) return;
            ClearInteraction();
            _choiceTcs.TrySetResult(text ?? string.Empty);
            _choiceTcs = null;
            NotifyChanged();
        }

        public void CompleteNext()
        {
            if (_choiceTcs == null) return;
            ClearInteraction();
            _choiceTcs.TrySetResult("NEXT");
            _choiceTcs = null;
            NotifyChanged();
        }

        public string WaitForChoice()
        {
            if (_injectedCommands.Count > 0)
            {
                return _injectedCommands.Dequeue();
            }
            if (_choiceTcs == null)
            {
                return string.Empty;
            }
            return _choiceTcs.Task.GetAwaiter().GetResult();
        }

        public void EnqueueCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;
            // Si on est sur un écran "Appuie sur Entrée", on ne remplace pas la prochaine action :
            // on garde le prompt et on met la commande en file pour la prochaine vraie interaction.
            if (_choiceTcs != null && AwaitingNext)
            {
                _injectedCommands.Enqueue(command);
                NotifyChanged();
                return;
            }

            if (_choiceTcs != null)
            {
                ClearInteraction();
                _choiceTcs.TrySetResult(command);
                _choiceTcs = null;
                NotifyChanged();
                return;
            }
            _injectedCommands.Enqueue(command);
            NotifyChanged();
        }

        private void ResetAwaiters()
        {
            _choiceTcs?.TrySetCanceled();
            _choiceTcs = null;
            App.Current?.Dispatcher.Dispatch(() =>
            {
                AwaitingNext = false;
                AwaitingText = false;
                Prompt = null;
                BackEnabled = false;
                Options.Clear();
            });
        }

        public void SetDarkMode(bool unlocked, bool enabled)
        {
            App.Current?.Dispatcher.Dispatch(() =>
            {
                DarkModeUnlocked = unlocked;
                DarkModeEnabled = enabled;
            });
            NotifyChanged();
        }

        private UIPlayerState AlignArtefactOrder(UIPlayerState incoming)
        {
            if (Player == null) return incoming;
            var previous = Player.Artefacts;
            if (previous == null || previous.Count == 0) return incoming;

            var map = incoming.Artefacts
                .GroupBy(a => a.Nom, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ordered = new List<UIArtefact>();

            foreach (var art in previous)
            {
                if (map.TryGetValue(art.Nom, out var found) && !used.Contains(found.Nom))
                {
                    ordered.Add(found);
                    used.Add(found.Nom);
                }
            }

            foreach (var art in incoming.Artefacts)
            {
                if (!used.Contains(art.Nom))
                {
                    ordered.Add(art);
                    used.Add(art.Nom);
                }
            }

            return incoming with { Artefacts = ordered };
        }
    }
}
