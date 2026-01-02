using ProjetDelta.Player;
using System;

namespace ProjetDelta.Artifacts
{
    public delegate void AttackTrigger(Combattant source, Combattant target, ref float damage);
    public delegate void DamageTrigger(Combattant target, ref float damage);
    public delegate void SimpleTrigger(Combattant bearer);
    public delegate void TurnTrigger(Combattant bearer);
    public interface IArtefact
    {
        string Nom { get; }
        string? Description { get; }
        string Color { get; }
        void OnCombatStart(Combattant bearer);
        void OnCombatEnd(Combattant bearer);
        void EquipTo(Combattant bearer);

        void OnAttack(Combattant bearer, Combattant cible, ref float degats);
        void OnDamageTaken(Combattant bearer, ref float degats);
        void OnTurnStart(Combattant bearer);

        public void Register(Combattant bearer);
    }

    public class TriggerArtefact : IArtefact
    {
        public string Nom { get; }
        public string? Description { get; }
        private readonly AttackTrigger? _onAttack;
        private readonly DamageTrigger? _onDamageTaken;
        private readonly SimpleTrigger? _onCombatStart;
        private readonly SimpleTrigger? _onCombatEnd;
        private readonly SimpleTrigger? _onEquip;
        private readonly TurnTrigger? _onTurnStart;
        public string Color { get; private set; }
        private readonly HashSet<Combattant> _registered = new();
        private readonly HashSet<Combattant> _equipApplied = new();

        private static string NormalizeColor(string color) =>
            string.IsNullOrWhiteSpace(color) ? "green" : color.Trim().ToLowerInvariant();

        public TriggerArtefact(
            string nom,
            string? description = null,
            AttackTrigger? onAttack = null,
            DamageTrigger? onDamageTaken = null,
            SimpleTrigger? onCombatStart = null,
            SimpleTrigger? onCombatEnd = null,
            SimpleTrigger? onEquip = null,
            TurnTrigger? onTurnStart = null,
            string color = "green"
        )
        {
            Nom = nom;
            Description = description;
            _onAttack = onAttack;
            _onDamageTaken = onDamageTaken;
            _onCombatStart = onCombatStart;
            _onCombatEnd = onCombatEnd;
            _onEquip = onEquip;
            _onTurnStart = onTurnStart;
            Color = NormalizeColor(color);
        }

        internal void SetColor(string color)
        {
            Color = NormalizeColor(color);
        }

        public  void Register(Combattant bearer)
        {
            if (_registered.Contains(bearer)) return;
            _registered.Add(bearer);
            if (_onAttack != null)
                bearer.OnAttack += (Combattant cible, ref float degats) =>
                {
                    _onAttack(bearer, cible, ref degats);
                };

            if (_onDamageTaken != null)
                bearer.OnDamageTaken += (ref float degats) =>
                {
                    _onDamageTaken(bearer, ref degats);
                };

            if (_onCombatStart != null)
                bearer.OnCombatStart += () =>
                {
                    _onCombatStart(bearer);
                };

            if (_onCombatEnd != null)
                bearer.OnCombatEnd += () =>
                {
                    _onCombatEnd(bearer);
                };

            if (_onTurnStart != null)
                bearer.OnTurnStart += () =>
                {
                    _onTurnStart(bearer);
                };
        }
        public void OnAttack(Combattant bearer, Combattant cible, ref float degats)
        {
            _onAttack?.Invoke(bearer, cible, ref degats);
        }

        public void OnDamageTaken(Combattant bearer, ref float degats)
        {
            _onDamageTaken?.Invoke(bearer, ref degats);
        }

        public void OnCombatStart(Combattant bearer)
        {
            _onCombatStart?.Invoke(bearer);
        }

        public void OnCombatEnd(Combattant bearer)
        {
            _onCombatEnd?.Invoke(bearer);
        }

        public void OnTurnStart(Combattant bearer)
        {
            _onTurnStart?.Invoke(bearer);
        }

        public void EquipTo(Combattant bearer)
        {
            if (_equipApplied.Contains(bearer)) return;
            _equipApplied.Add(bearer);
            _onEquip?.Invoke(bearer);
        }
    }

}
