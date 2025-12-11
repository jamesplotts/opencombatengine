using System;
using OpenCombatEngine.Core.Enums;

namespace OpenCombatEngine.Core.Models.Spells
{
    public class SpellConditionDefinition
    {
        public string ConditionName { get; }
        public string Duration { get; } // e.g. "1 minute", "Instantaneous"
        public SaveEffect SaveEffectType { get; } // When to apply? Usually OnFail.
        
        // Maybe we need a specific enum for ConditionApplicationType? 
        // SaveEffect enum has: None, Negate, HalfDamage.
        // It describes what happens "On Save".
        // If SaveEffect is "Negate", it implies "On Save: Nothing happens". So "On Fail: Effect happens".
        // If SaveEffect is "HalfDamage", it implies "On Save: Half Damage". But what about conditions?
        // Usually, conditions are negated on save.
        // Let's use SaveEffect for now. If Negate, condition is verified only on fail.
        
        public SpellConditionDefinition(string conditionName, string duration, SaveEffect saveEffectType = SaveEffect.Negate)
        {
            if (string.IsNullOrWhiteSpace(conditionName)) throw new ArgumentException("Condition name cannot be empty", nameof(conditionName));
            ConditionName = conditionName;
            Duration = duration;
            SaveEffectType = saveEffectType;
        }
    }
}
