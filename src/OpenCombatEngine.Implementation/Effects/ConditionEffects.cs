using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Effects;
using OpenCombatEngine.Implementation.Effects;

namespace OpenCombatEngine.Implementation.Effects
{
    public abstract class ConditionEffectBase : IActiveEffect
    {
        public string Name { get; }
        public string Description { get; }
        public int DurationRounds => -1; // Managed by Condition
        public DurationType DurationType => DurationType.Permanent;

        protected ConditionEffectBase(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public virtual void OnApplied(ICreature target) { }
        public virtual void OnRemoved(ICreature target) { }
        public virtual void OnTurnStart(ICreature target) { }
        public virtual void OnTurnEnd(ICreature target) { }
        public virtual int ModifyStat(StatType stat, int currentValue) => currentValue;
    }

    public class AdvantageOnIncomingAttacksEffect : ConditionEffectBase
    {
        public AdvantageOnIncomingAttacksEffect(string sourceCondition) 
            : base($"{sourceCondition}_AdvantageIncoming", $"Attacks against target have advantage due to {sourceCondition}.") { }

        public override int ModifyStat(StatType stat, int currentValue)
        {
            if (stat == StatType.IncomingAttackAdvantage) return 1;
            return currentValue;
        }
    }

    public class DisadvantageOnOutgoingAttacksEffect : ConditionEffectBase
    {
        public DisadvantageOnOutgoingAttacksEffect(string sourceCondition) 
            : base($"{sourceCondition}_DisadvantageOutgoing", $"Attacks by target have disadvantage due to {sourceCondition}.") { }

        public override int ModifyStat(StatType stat, int currentValue)
        {
            if (stat == StatType.AttackDisadvantage) return 1;
            return currentValue;
        }
    }
}
