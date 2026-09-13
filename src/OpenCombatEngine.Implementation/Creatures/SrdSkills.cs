// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System.Collections.Generic;
using OpenCombatEngine.Core.Enums;

namespace OpenCombatEngine.Implementation.Creatures
{
    /// <summary>
    /// One SRD 5.1 skill: its name and the ability score it's governed
    /// by. Skill names are open SRD content (not proprietary D&amp;D
    /// flavor) — see CLAUDE.md's "SRD mechanics only" rule.
    /// </summary>
    public sealed record SrdSkill(string Name, Ability Ability);

    /// <summary>
    /// The canonical list of all 18 SRD 5.1 skills, in the SRD's own
    /// presentation order. No such list existed anywhere in this engine
    /// before this — skill names were only ever free-form strings a
    /// caller happened to pass to <see cref="OpenCombatEngine.Core.Interfaces.Creatures.ICheckManager.AddSkillProficiency"/>.
    /// Built specifically so a computed, complete skills list (proficient
    /// or not) can be reported for a character sheet — see
    /// <see cref="StandardCreature.GetState"/> for where this is used.
    /// </summary>
    public static class SrdSkills
    {
        public static readonly IReadOnlyList<SrdSkill> All = new[]
        {
            new SrdSkill("Acrobatics", Ability.Dexterity),
            new SrdSkill("Animal Handling", Ability.Wisdom),
            new SrdSkill("Arcana", Ability.Intelligence),
            new SrdSkill("Athletics", Ability.Strength),
            new SrdSkill("Deception", Ability.Charisma),
            new SrdSkill("History", Ability.Intelligence),
            new SrdSkill("Insight", Ability.Wisdom),
            new SrdSkill("Intimidation", Ability.Charisma),
            new SrdSkill("Investigation", Ability.Intelligence),
            new SrdSkill("Medicine", Ability.Wisdom),
            new SrdSkill("Nature", Ability.Intelligence),
            new SrdSkill("Perception", Ability.Wisdom),
            new SrdSkill("Performance", Ability.Charisma),
            new SrdSkill("Persuasion", Ability.Charisma),
            new SrdSkill("Religion", Ability.Intelligence),
            new SrdSkill("Sleight of Hand", Ability.Dexterity),
            new SrdSkill("Stealth", Ability.Dexterity),
            new SrdSkill("Survival", Ability.Wisdom),
        };

        /// <summary>
        /// Maps an <see cref="Ability"/> to its conventional 3-letter
        /// abbreviation ("Strength" -&gt; "STR") — compact enough for a
        /// narrow character-sheet column. <see cref="Ability.Unspecified"/>/
        /// <see cref="Ability.LastValue"/> return an empty string; neither
        /// should ever reach here in practice.
        /// </summary>
        public static string Abbreviate(Ability ability) => ability switch
        {
            Ability.Strength => "STR",
            Ability.Dexterity => "DEX",
            Ability.Constitution => "CON",
            Ability.Intelligence => "INT",
            Ability.Wisdom => "WIS",
            Ability.Charisma => "CHA",
            _ => "",
        };
    }
}
