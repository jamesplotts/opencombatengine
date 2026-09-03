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
    }
#pragma warning restore CA1002
}
