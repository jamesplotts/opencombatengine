// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using OpenCombatEngine.Core.Interfaces.Dice;

namespace OpenCombatEngine.Core.Models.Creatures
{
    /// <summary>
    /// Result of a creature starting its turn (see <see cref="Interfaces.Creatures.ICreature.StartTurn"/>).
    /// </summary>
    public class TurnStartResult
    {
        /// <summary>
        /// Gets the automatically-rolled death saving throw, if the creature
        /// was at 0 HP, not dead, and not stable when its turn began — SRD
        /// death saves are automatic, not a choice a caller opts into, so
        /// StartTurn rolls one itself rather than requiring a second call.
        /// Null when no death save applied this turn.
        /// </summary>
        public DiceRollResult? DeathSaveRoll { get; }

        /// <summary>
        /// Gets a value indicating whether the death save (if any) was a
        /// natural 20 — the creature regains 1 HP and wakes up, per SRD
        /// death saving throw rules.
        /// </summary>
        public bool WokeUp { get; }

        public TurnStartResult(DiceRollResult? deathSaveRoll = null, bool wokeUp = false)
        {
            DeathSaveRoll = deathSaveRoll;
            WokeUp = wokeUp;
        }
    }
}
