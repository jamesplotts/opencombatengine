using System;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Features;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Models.Combat;

namespace OpenCombatEngine.Implementation.Features
{
    public class ActionFeature : IFeature
    {
        public string Name { get; }
        public IAction Action { get; }

        public ActionFeature(string name, IAction action)
        {
            Name = name;
            Action = action;
        }

        public void OnApplied(ICreature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);
            creature.AddAction(Action);
        }

        public void OnRemoved(ICreature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);
            creature.RemoveAction(Action);
        }

        public void OnOutgoingAttack(ICreature source, AttackResult attack) { }
        public void OnStartTurn(ICreature creature) { }
    }
}
