// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System.Globalization;

namespace OpenCombatEngine.Implementation.Dice
{
    /// <summary>
    /// Builds a valid dice-notation string for <see cref="OpenCombatEngine.Core.Interfaces.Dice.IDiceRoller.Roll"/>
    /// out of a base expression and a signed modifier — exists specifically
    /// to close a real, live-observed bug: every one of this engine's
    /// modifier-carrying rolls (ability checks, saving throws, attack
    /// rolls, initiative) used to build its notation by naively string-
    /// interpolating the modifier directly (<c>$"1d20+{modifier}"</c>).
    /// For a negative modifier that produces a double sign — <c>"1d20+-1"</c>
    /// — which <see cref="StandardDiceRoller"/>'s own notation regex (a
    /// single leading <c>+</c>/<c>-</c> before the digits) rejects
    /// outright, failing the roll entirely rather than actually rolling
    /// with the penalty as the SRD expects. Confirmed live: a Charisma 8
    /// character (-1 modifier, no relevant proficiency) attempting a
    /// Persuasion check got "resolution_failed" with no roll made at all.
    /// </summary>
    public static class DiceNotation
    {
        /// <summary>
        /// Combines diceExpression (e.g. "1d20") with modifier into one
        /// valid notation string — "1d20+3" for a positive modifier,
        /// "1d20-1" for a negative one, "1d20+0" for zero. Never produces
        /// a double sign, unlike naively interpolating a signed int
        /// directly after a literal "+".
        /// </summary>
        /// <param name="diceExpression">The dice portion, e.g. "1d20" — no modifier of its own.</param>
        /// <param name="modifier">The signed modifier to append.</param>
        public static string WithModifier(string diceExpression, int modifier)
        {
            return modifier >= 0
                ? diceExpression + "+" + modifier.ToString(CultureInfo.InvariantCulture)
                : diceExpression + modifier.ToString(CultureInfo.InvariantCulture);
        }
    }
}
