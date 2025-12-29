using ProjetDelta.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetDelta.Artifacts
{
    public class ArtefactBuilder
    {
        private string _nom = "Artefact";
        private string? _description;
        private AttackTrigger? _onAttack;
        private DamageTrigger? _onDamageTaken;
        private SimpleTrigger? _onCombatStart;
        private SimpleTrigger? _onCombatEnd;
        private SimpleTrigger? _onEquip;
        private TurnTrigger? _onTurnStart;

        public ArtefactBuilder OnAttack(AttackTrigger action)
        {
            _onAttack = action;
            return this;
        }

        public ArtefactBuilder OnDamageTaken(DamageTrigger action)
        {
            _onDamageTaken = action;
            return this;
        }

        public ArtefactBuilder OnCombatStart(SimpleTrigger action)
        {
            _onCombatStart = action;
            return this;
        }

        public ArtefactBuilder OnCombatEnd(SimpleTrigger action)
        {
            _onCombatEnd = action;
            return this;
        }

        public ArtefactBuilder OnEquip(SimpleTrigger action)
        {
            _onEquip = action;
            return this;
        }

        public ArtefactBuilder OnTurnStart(TurnTrigger action)
        {
            _onTurnStart = action;
            return this;
        }

        public ArtefactBuilder AvecNom(string nom)
        {
            _nom = nom;
            return this;
        }

        public ArtefactBuilder AvecDescription(string? description)
        {
            _description = description;
            return this;
        }

        public IArtefact Build()
        {
            return new TriggerArtefact(_nom, _description, _onAttack, _onDamageTaken, _onCombatStart, _onCombatEnd, _onEquip, _onTurnStart);
        }
    }
}
