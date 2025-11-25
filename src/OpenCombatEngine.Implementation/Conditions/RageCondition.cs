using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.Events;

namespace OpenCombatEngine.Implementation.Conditions
{
    public class RageCondition : ICondition
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name => "Rage";
        public string Description => "You have resistance to bludgeoning, piercing, and slashing damage.";
        public ConditionType Type => ConditionType.Custom; // Or define a Rage type if needed
        public int DurationRounds { get; private set; } = 10; // 1 minute
        public System.Collections.Generic.IEnumerable<OpenCombatEngine.Core.Interfaces.Effects.IActiveEffect> Effects => System.Linq.Enumerable.Empty<OpenCombatEngine.Core.Interfaces.Effects.IActiveEffect>();
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Interface implementation")]
        public bool IsPermanent => false;

        private ICreature? _target;

        public void OnApplied(ICreature target)
        {
            ArgumentNullException.ThrowIfNull(target);
            _target = target;
            target.CombatStats.AddResistance(DamageType.Bludgeoning);
            target.CombatStats.AddResistance(DamageType.Piercing);
            target.CombatStats.AddResistance(DamageType.Slashing);
        }

        public void OnRemoved(ICreature target)
        {
            ArgumentNullException.ThrowIfNull(target);
            target.CombatStats.RemoveResistance(DamageType.Bludgeoning);
            target.CombatStats.RemoveResistance(DamageType.Piercing);
            target.CombatStats.RemoveResistance(DamageType.Slashing);
            _target = null;
        }

        public void OnTurnStart(ICreature target)
        {
            if (DurationRounds > 0)
            {
                DurationRounds--;
            }
        }
    }
}
