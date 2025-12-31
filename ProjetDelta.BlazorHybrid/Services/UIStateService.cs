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
        public event Action? Changed;

        private TaskCompletionSource<string>? _choiceTcs;

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
            });
            NotifyChanged();
        }

        public void AddMessage(string msg, string color = "white")
        {
            AddLine(new[] { new MessageSegment(msg, color) });
        }

        public void SetPlayerState(UIPlayerState state)
        {
            App.Current?.Dispatcher.Dispatch(() =>
            {
                Player = state;
                IsLoading = false;
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
            if (_choiceTcs == null)
            {
                return string.Empty;
            }
            return _choiceTcs.Task.GetAwaiter().GetResult();
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
    }
}
