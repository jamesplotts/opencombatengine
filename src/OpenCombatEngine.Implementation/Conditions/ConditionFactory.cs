using System.Collections.Generic;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Interfaces.Effects;
using OpenCombatEngine.Implementation.Effects;

namespace OpenCombatEngine.Implementation.Conditions
{
    public static class ConditionFactory
    {
        public static ICondition Create(ConditionType type, int durationRounds = -1)
        {
            var effects = new List<IActiveEffect>();

            switch (type)
            {
                case ConditionType.Blinded:
                    effects.Add(new DisadvantageOnOutgoingAttacksEffect("Blinded"));
                    effects.Add(new AdvantageOnIncomingAttacksEffect("Blinded"));
                    return new Condition("Blinded", "Can't see. Disadvantage on attacks. Enemy has Advantage.", durationRounds, type, effects);

                case ConditionType.Prone:
                    // Prone is tricky: Disadvantage on attacks.
                    // Incoming attacks: Advantage if within 5ft, Disadvantage if > 5ft.
                    // Current Effect system is simple (Advantage/Disadvantage).
                    // We might need a specific ProneEffect that checks distance in ModifyStat?
                    // But ModifyStat takes StatType and int value. It doesn't know about source/target distance.
                    // AttackAction checks distance.
                    // AttackAction could check "IncomingAttackAdvantage" stat.
                    // If we want distance logic, the Effect needs context.
                    // But ModifyStat doesn't provide context (ActionContext).
                    // This is a limitation.
                    // For now, let's implement the simpler parts: Disadvantage on outgoing.
                    // And maybe "IncomingAttackAdvantage" generally, noting the limitation?
                    // Or we skip the distance check for now and just say "Advantage on incoming melee"?
                    // 5e: "An attack roll against the creature has advantage if the attacker is within 5 feet of the creature. Otherwise, the attack roll has disadvantage."
                    // This is complex for a simple stat modifier.
                    // We'll implement Disadvantage on Outgoing.
                    // For incoming, we might need a custom effect that we handle specially in AttackAction?
                    // Or we just leave it for now.
                    effects.Add(new DisadvantageOnOutgoingAttacksEffect("Prone"));
                    return new Condition("Prone", "Disadvantage on attacks.", durationRounds, type, effects);

                case ConditionType.Restrained:
                    effects.Add(new DisadvantageOnOutgoingAttacksEffect("Restrained"));
                    effects.Add(new AdvantageOnIncomingAttacksEffect("Restrained"));
                    effects.Add(new StatBonusEffect("Restrained_DexSave", "Disadvantage on Dex saves", -1, StatType.SaveDisadvantage, 1)); // 1 = Disadvantage flag
                    return new Condition("Restrained", "Speed 0. Disadvantage on attacks. Enemy has Advantage. Disadv on Dex saves.", durationRounds, type, effects);

                case ConditionType.Poisoned:
                    effects.Add(new DisadvantageOnOutgoingAttacksEffect("Poisoned"));
                    effects.Add(new StatBonusEffect("Poisoned_Checks", "Disadvantage on Ability Checks", -1, StatType.CheckDisadvantage, 1));
                    return new Condition("Poisoned", "Disadvantage on attacks and checks.", durationRounds, type, effects);

                default:
                    return new Condition(type.ToString(), "Standard condition.", durationRounds, type);
            }
        }
    }
}
