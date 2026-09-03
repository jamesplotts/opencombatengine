// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;

namespace OpenCombatEngine.Implementation.Actions
{
    /// <summary>
    /// Real source-side gate against a creature attempting to act while
    /// incapacitated (SRD: Paralyzed, Stunned, Petrified, and
    /// Incapacitated itself all mean "can't take actions or reactions").
    /// Nothing checked this before — <see cref="AttackAction"/> and
    /// <see cref="CastSpellAction"/> only ever checked a *target's*
    /// incapacitating conditions, for advantage.
    /// </summary>
    /// <remarks>
    /// Deliberately independent of the HP-based Unconscious/Dying/Dead
    /// status (<c>StandardHitPoints</c>, mapped to the gRPC contract by
    /// <c>CharacterStatusMapper</c>) — that is a separate, already-correct
    /// mechanism: a downed character still gets a turn to roll a death
    /// saving throw (<c>StartTurn</c>'s automatic roll), which this check
    /// must not block. Only a real applied <see cref="ConditionType"/>
    /// blocks acting here.
    /// </remarks>
    public static class IncapacitationCheck
    {
        private static readonly ConditionType[] BlockingConditions =
        {
            ConditionType.Paralyzed,
            ConditionType.Stunned,
            ConditionType.Petrified,
            ConditionType.Incapacitated,
        };

        /// <summary>
        /// Returns the name of the first blocking condition found on
        /// source's own <see cref="ICreature.Conditions"/>, or
        /// <see langword="null"/> if source can act.
        /// </summary>
        public static string? BlockingCondition(ICreature source)
        {
            if (source?.Conditions == null) return null;
            foreach (var condition in BlockingConditions)
            {
                if (source.Conditions.HasCondition(condition)) return condition.ToString();
            }
            return null;
        }
    }
}
