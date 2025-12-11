using System;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Features;
using OpenCombatEngine.Core.Models.Combat;
using OpenCombatEngine.Implementation.Actions;

namespace OpenCombatEngine.Implementation.Features
{
    public class RechargeableFeature : IFeature
    {
        public string Name { get; }
        private readonly RechargeableAction _rechargeableAction;

        public RechargeableFeature(string name, RechargeableAction rechargeableAction)
        {
            Name = name;
            _rechargeableAction = rechargeableAction ?? throw new ArgumentNullException(nameof(rechargeableAction));
        }

        public void OnApplied(ICreature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);
            // We assume the creature has a way to add actions dynamically. 
            // StandardCreature has AddAction.
             if (creature is OpenCombatEngine.Implementation.Creatures.StandardCreature std)
             {
                 std.AddAction(_rechargeableAction);
             }
        }

        public void OnRemoved(ICreature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);
             if (creature is OpenCombatEngine.Implementation.Creatures.StandardCreature std)
             {
                 std.RemoveAction(_rechargeableAction);
             }
        }

        public void OnStartTurn(ICreature creature)
        {
            if (!_rechargeableAction.IsAvailable)
            {
                // Roll d6
                // We need dice access. 
                // We can use creature.Checks (ICheckManager) if it exposes raw rolls, or use a local roller?
                // Creature usually has a CheckManager that uses a DiceRoller.
                // StandardCheckManager doesn't expose generic roll.
                // But StandardCreature creates a StandardDiceRoller for itself usually?
                // We shouldn't instantiate new Random every time if possible.
                // Let's us creature.Checks to roll a "Recharge Check"? 
                // Or just use a new StandardDiceRoller since we are in Implementation.
                
                var roller = new OpenCombatEngine.Implementation.Dice.StandardDiceRoller();
                var roll = roller.Roll("1d6");
                if (roll.IsSuccess)
                {
                    if (_rechargeableAction.TryRecharge(roll.Value.Total))
                    {
                        // Maybe log it?
                        // "Recharged!"
                    }
                }
            }
        }

        public void OnOutgoingAttack(ICreature source, AttackResult attack) { }
    }
}
