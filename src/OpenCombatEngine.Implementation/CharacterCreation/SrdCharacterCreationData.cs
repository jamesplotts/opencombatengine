// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System.Collections.Generic;
using OpenCombatEngine.Core.Enums;

namespace OpenCombatEngine.Implementation.CharacterCreation
{
    /// <summary>
    /// A curated SRD 5.1 race: per-ability score bonuses and base speed.
    /// </summary>
    public sealed record SrdRace(
        string Name,
        int StrengthBonus,
        int DexterityBonus,
        int ConstitutionBonus,
        int IntelligenceBonus,
        int WisdomBonus,
        int CharismaBonus,
        int Speed);

    /// <summary>
    /// A curated SRD 5.1 class: hit die, spellcasting (if any), and a
    /// fixed starting-equipment kit (this scope uses one fixed kit per
    /// class rather than the SRD's "choose A or B" equipment tables).
    /// </summary>
    public sealed record SrdClass(
        string Name,
        int HitDie,
        bool IsSpellcaster,
        Ability SpellcastingAbility,
        IReadOnlyList<string> StartingEquipment);

    /// <summary>
    /// A curated SRD 5.1 background: starting equipment and gold (this
    /// scope omits skill/tool proficiencies — see
    /// StandardCharacterCreationService's own doc comment for why: this
    /// engine's CreatureState has no field to persist them on today).
    /// </summary>
    public sealed record SrdBackground(
        string Name,
        IReadOnlyList<string> StartingEquipment,
        int StartingGoldPieces);

    /// <summary>
    /// The fixed, hand-authored SRD 5.1 content set character creation
    /// offers at this scope — 4 races, 4 classes, 4 backgrounds. Real SRD
    /// values, not invented ones; not sourced from Open5e (its REST API
    /// has no races/classes/backgrounds endpoints) and not built on
    /// JsonRaceImporter/JsonClassImporter (they expect a bulk 5e.tools
    /// compendium format that would be overkill for four curated entries
    /// each).
    /// </summary>
    public static class SrdCharacterCreationData
    {
        public static readonly IReadOnlyList<SrdRace> Races = new List<SrdRace>
        {
            // Human: +1 to every ability score (SRD 5.1 standard human).
            new("Human", 1, 1, 1, 1, 1, 1, 30),
            // Elf: +2 Dexterity.
            new("Elf", 0, 2, 0, 0, 0, 0, 30),
            // Dwarf: +2 Constitution.
            new("Dwarf", 0, 0, 2, 0, 0, 0, 25),
            // Halfling: +2 Dexterity.
            new("Halfling", 0, 2, 0, 0, 0, 0, 25),
        };

        public static readonly IReadOnlyList<SrdClass> Classes = new List<SrdClass>
        {
            new("Fighter", HitDie: 10, IsSpellcaster: false, SpellcastingAbility: Ability.Unspecified,
                StartingEquipment: new List<string> { "Chain Mail", "Longsword", "Shield", "Light Crossbow", "Explorer's Pack" }),
            new("Wizard", HitDie: 6, IsSpellcaster: true, SpellcastingAbility: Ability.Intelligence,
                StartingEquipment: new List<string> { "Quarterstaff", "Spellbook", "Scholar's Pack" }),
            new("Cleric", HitDie: 8, IsSpellcaster: true, SpellcastingAbility: Ability.Wisdom,
                StartingEquipment: new List<string> { "Mace", "Scale Mail", "Shield", "Priest's Pack" }),
            new("Rogue", HitDie: 8, IsSpellcaster: false, SpellcastingAbility: Ability.Unspecified,
                StartingEquipment: new List<string> { "Rapier", "Shortbow", "Leather Armor", "Thieves' Tools", "Burglar's Pack" }),
        };

        public static readonly IReadOnlyList<SrdBackground> Backgrounds = new List<SrdBackground>
        {
            new("Acolyte", new List<string> { "Holy Symbol", "Prayer Book", "Incense", "Vestments" }, StartingGoldPieces: 15),
            new("Criminal", new List<string> { "Crowbar", "Dark Common Clothes" }, StartingGoldPieces: 15),
            new("Folk Hero", new List<string> { "Artisan's Tools", "Shovel", "Common Clothes" }, StartingGoldPieces: 10),
            new("Soldier", new List<string> { "Insignia of Rank", "Common Clothes" }, StartingGoldPieces: 10),
        };
    }
}
