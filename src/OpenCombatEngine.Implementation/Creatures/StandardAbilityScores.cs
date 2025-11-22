using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.States;

namespace OpenCombatEngine.Implementation.Creatures
{
    /// <summary>
    /// Standard implementation of ability scores following SRD 5.1 rules.
    /// Immutable record to ensure thread safety and predictability.
    /// </summary>
    public record StandardAbilityScores : IAbilityScores, IStateful<AbilityScoresState>
    {
        public int Strength { get; }
        public int Dexterity { get; }
        public int Constitution { get; }
        public int Intelligence { get; }
        public int Wisdom { get; }
        public int Charisma { get; }

        /// <summary>
        /// Initializes a new instance of StandardAbilityScores with the specified values.
        /// </summary>
        public StandardAbilityScores(
            int strength = 10,
            int dexterity = 10,
            int constitution = 10,
            int intelligence = 10,
            int wisdom = 10,
            int charisma = 10)
        {
            Strength = ValidateScore(strength, nameof(strength));
            Dexterity = ValidateScore(dexterity, nameof(dexterity));
            Constitution = ValidateScore(constitution, nameof(constitution));
            Intelligence = ValidateScore(intelligence, nameof(intelligence));
            Wisdom = ValidateScore(wisdom, nameof(wisdom));
            Charisma = ValidateScore(charisma, nameof(charisma));
        }

        /// <summary>
        /// Initializes a new instance of StandardAbilityScores from a state object.
        /// </summary>
        /// <param name="state">The state to restore from.</param>
        public StandardAbilityScores(AbilityScoresState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            Strength = ValidateScore(state.Strength, nameof(state.Strength));
            Dexterity = ValidateScore(state.Dexterity, nameof(state.Dexterity));
            Constitution = ValidateScore(state.Constitution, nameof(state.Constitution));
            Intelligence = ValidateScore(state.Intelligence, nameof(state.Intelligence));
            Wisdom = ValidateScore(state.Wisdom, nameof(state.Wisdom));
            Charisma = ValidateScore(state.Charisma, nameof(state.Charisma));
        }

        /// <inheritdoc />
        public int GetModifier(Ability ability)
        {
            int score = ability switch
            {
                Ability.Strength => Strength,
                Ability.Dexterity => Dexterity,
                Ability.Constitution => Constitution,
                Ability.Intelligence => Intelligence,
                Ability.Wisdom => Wisdom,
                Ability.Charisma => Charisma,
                _ => throw new ArgumentOutOfRangeException(nameof(ability), ability, "Invalid ability specified")
            };

            // SRD Rule: Modifier is (Score - 10) / 2, rounded down.
            // Integer division in C# rounds toward zero, so -1/2 = 0, but we need -1.
            // Math.Floor((double)(score - 10) / 2) handles this correctly.
            return (int)Math.Floor((score - 10) / 2.0);
        }

        /// <inheritdoc />
        public AbilityScoresState GetState()
        {
            return new AbilityScoresState(Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma);
        }

        private static int ValidateScore(int score, string paramName)
        {
            if (score < 0)
                throw new ArgumentOutOfRangeException(paramName, score, "Ability score cannot be negative");
            return score;
        }
    }
}
