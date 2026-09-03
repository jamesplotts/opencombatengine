using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OpenCombatEngine.Implementation.Open5e
{
#pragma warning disable CA1002 // Change List<T> to use Collection<T> — internal parsing helper, not a public DTO surface; SpellDtos.cs disables the same rule for the same reason.
    /// <summary>
    /// Extracts damage dice, damage types, and a saving-throw ability from
    /// an Open5e spell's free-text description. Open5e's REST API exposes
    /// no structured damage/save fields for spells at all (unlike the
    /// 5etools JSON format <see cref="OpenCombatEngine.Implementation.Content.Dtos.SpellDto"/>
    /// otherwise mirrors) — this recovers the same information from prose
    /// using the SRD's own consistent phrasing ("takes XdY &lt;type&gt;
    /// damage", "&lt;ability&gt; saving throw"). Best-effort: a description
    /// that doesn't follow this phrasing (rare, but real — non-standard
    /// wording, multi-stage effects) simply yields no damage/save, the same
    /// as before this existed, rather than throwing.
    /// </summary>
    public static class Open5eSpellTextParser
    {
        private static readonly Regex DamageRegex = new(
            @"(?<dice>\d+d\d+(?:\s*[+-]\s*\d+)?)\s+(?<type>acid|bludgeoning|cold|fire|force|lightning|necrotic|piercing|poison|psychic|radiant|slashing|thunder)\s+damage",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SavingThrowRegex = new(
            @"(?<ability>strength|dexterity|constitution|intelligence|wisdom|charisma)\s+saving throw",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex HealingEqualToRegex = new(
            @"hit points?\s+equal to\s+(?<dice>\d+d\d+(?:\s*[+-]\s*\d+)?)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex HealingFlatRegex = new(
            @"(?:regains?|restores?)\s+(?<dice>\d+d\d+(?:\s*[+-]\s*\d+)?|\d+)\s+hit points?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex AttackRollRegex = new(
            @"make an? (?:melee|ranged) spell attack",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ProjectileCountRegex = new(
            @"\b(?:you create|creates?)\s+(?<count>a|one|two|three|four|five|six|seven|eight|nine|ten)\s+(?:\w+\s+){0,2}(?:darts?|rays?|bolts?|beams?|missiles?|shards?)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex InstanceScalingRegex = new(
            @"one (?:more|additional)\s+(?:\w+\s+){0,2}(?:darts?|rays?|bolts?|beams?|missiles?|shards?)\s+for each (?:slot level|spell slot level) above",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Dictionary<string, int> NumberWords = new(System.StringComparer.OrdinalIgnoreCase)
        {
            ["a"] = 1,
            ["one"] = 1,
            ["two"] = 2,
            ["three"] = 3,
            ["four"] = 4,
            ["five"] = 5,
            ["six"] = 6,
            ["seven"] = 7,
            ["eight"] = 8,
            ["nine"] = 9,
            ["ten"] = 10,
        };

        /// <summary>
        /// Extracts every "XdY [+/-Z] &lt;type&gt; damage" phrase in text,
        /// in the order they appear, as (dice notation, uppercase damage
        /// type) pairs — <c>Enum.TryParse&lt;DamageType&gt;</c> is
        /// case-insensitive, so casing only matters for this pair's own
        /// consistency. Dice notation is normalized to have no internal
        /// whitespace (e.g. "1d4 + 1" becomes "1d4+1"), matching what
        /// <see cref="OpenCombatEngine.Core.Interfaces.Dice.IDiceRoller.Roll"/>
        /// requires. Most spells yield exactly one entry; a few (e.g. a
        /// spell dealing two damage types at once) yield more. Returns an
        /// empty list for null/blank/non-matching text — never null.
        /// </summary>
        public static List<(string Dice, string Type)> ExtractDamageRolls(string? text)
        {
            var results = new List<(string, string)>();
            if (string.IsNullOrWhiteSpace(text))
            {
                return results;
            }

            foreach (Match match in DamageRegex.Matches(text))
            {
                var dice = Regex.Replace(match.Groups["dice"].Value, @"\s+", "");
                results.Add((dice, match.Groups["type"].Value.ToUpperInvariant()));
            }

            return results;
        }

        /// <summary>
        /// Extracts the three-letter SRD ability abbreviation (e.g. "DEX")
        /// for the first "&lt;ability&gt; saving throw" phrase in text —
        /// the same abbreviation format <c>SpellMapper.MapSaveAbility</c>
        /// already expects on <c>SpellDto.SavingThrow</c>. Returns null if
        /// the description names no saving throw (an attack-roll or
        /// no-roll spell).
        /// </summary>
        public static string? ExtractSavingThrowAbility(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var match = SavingThrowRegex.Match(text);
            if (!match.Success)
            {
                return null;
            }

            return match.Groups["ability"].Value.ToUpperInvariant() switch
            {
                "STRENGTH" => "STR",
                "DEXTERITY" => "DEX",
                "CONSTITUTION" => "CON",
                "INTELLIGENCE" => "INT",
                "WISDOM" => "WIS",
                "CHARISMA" => "CHA",
                _ => null,
            };
        }

        /// <summary>
        /// Extracts a healing spell's dice notation from "regain[s]/
        /// restore[s] ... hit points [equal to XdY]" phrasing (e.g. Cure
        /// Wounds' "regains a number of hit points equal to 1d8 + your
        /// spellcasting ability modifier", Goodberry's "restores 1 hit
        /// point"). Any "+ your spellcasting ability modifier" suffix is
        /// dropped — <see cref="OpenCombatEngine.Core.Interfaces.Spells.ISpell.HealingDice"/>
        /// is a fixed dice string with no caster-dependent bonus, the same
        /// simplification <c>ExtractDamageRolls</c> already accepts for
        /// damage. Returns null if the description names no healing.
        /// </summary>
        public static string? ExtractHealingDice(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var equalToMatch = HealingEqualToRegex.Match(text);
            if (equalToMatch.Success)
            {
                return Regex.Replace(equalToMatch.Groups["dice"].Value, @"\s+", "");
            }

            var flatMatch = HealingFlatRegex.Match(text);
            return flatMatch.Success
                ? Regex.Replace(flatMatch.Groups["dice"].Value, @"\s+", "")
                : null;
        }

        /// <summary>
        /// Detects the SRD's own "Make a melee/ranged spell attack ..."
        /// phrasing (e.g. Ray of Frost, Scorching Ray, Inflict Wounds) —
        /// the same signal <c>SpellDto.SpellAttack</c> being non-empty
        /// already conveys for a 5etools-format spell.
        /// </summary>
        public static bool ExtractRequiresAttackRoll(string? text)
        {
            return !string.IsNullOrWhiteSpace(text) && AttackRollRegex.IsMatch(text);
        }

        /// <summary>
        /// Extracts how many times a spell's effect repeats per cast at
        /// its base level, from "you create &lt;count&gt; ... darts/rays/
        /// bolts/beams/missiles/shards" phrasing (Magic Missile's three
        /// darts, Scorching Ray's three rays). Returns 1 — the SRD default
        /// for a spell that only ever affects a target once — when no such
        /// phrasing is found.
        /// </summary>
        public static int ExtractInstanceCount(string? desc)
        {
            if (string.IsNullOrWhiteSpace(desc))
            {
                return 1;
            }

            var match = ProjectileCountRegex.Match(desc);
            if (!match.Success)
            {
                return 1;
            }

            return NumberWords.TryGetValue(match.Groups["count"].Value, out var count) ? count : 1;
        }

        /// <summary>
        /// Detects the SRD's own "one more/additional &lt;projectile&gt;
        /// for each slot level above &lt;Nth&gt;" upcast phrasing (Magic
        /// Missile, Scorching Ray) in a spell's higher_level text. Every
        /// sampled SRD spell using this phrasing scales by exactly one
        /// instance per extra level, so this returns 1 when found, 0
        /// otherwise — not a general count extractor.
        /// </summary>
        public static int ExtractInstanceCountPerUpcastLevel(string? higherLevelText)
        {
            return !string.IsNullOrWhiteSpace(higherLevelText) && InstanceScalingRegex.IsMatch(higherLevelText) ? 1 : 0;
        }
    }
#pragma warning restore CA1002
}
