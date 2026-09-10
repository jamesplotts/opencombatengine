// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.CharacterCreation;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Implementation.CharacterCreation;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Spells;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.CharacterCreation
{
    /// <summary>
    /// Tests for <see cref="StandardCharacterCreationService"/> — walks the
    /// real prompt/answer sequence end to end (detailed mode) and confirms
    /// quick mode's "race/class/gender, the rest is rolled" contract, per
    /// design doc's join-time "roll a new character" path.
    /// </summary>
    public class StandardCharacterCreationServiceTests
    {
        private static InMemorySpellRepository SeededSpellRepository()
        {
            var repo = new InMemorySpellRepository();
            var roller = new StandardDiceRoller();

            void Add(string name, int level, params string[] classes) =>
                repo.AddSpell(new Spell(name, level, SpellSchool.Evocation, "1 Action", "60 feet", "V, S", "Instantaneous", "Test spell.", roller, classes: classes));

            // Real SRD spell names, minimal fake mechanical data — enough
            // for these tests to verify class/level filtering, not real
            // damage resolution.
            Add("Fire Bolt", 0, "Wizard", "Sorcerer");
            Add("Mage Hand", 0, "Wizard", "Sorcerer", "Bard", "Warlock");
            Add("Light", 0, "Wizard", "Cleric", "Bard", "Sorcerer");
            Add("Ray of Frost", 0, "Wizard", "Sorcerer");
            Add("Magic Missile", 1, "Wizard", "Sorcerer");
            Add("Shield", 1, "Wizard", "Sorcerer");
            Add("Burning Hands", 1, "Wizard", "Sorcerer");
            Add("Guidance", 0, "Cleric", "Druid");
            Add("Sacred Flame", 0, "Cleric");
            Add("Thaumaturgy", 0, "Cleric");
            Add("Cure Wounds", 1, "Cleric", "Druid", "Bard", "Ranger", "Paladin");
            Add("Bless", 1, "Cleric", "Paladin");
            Add("Guiding Bolt", 1, "Cleric");
            return repo;
        }

        private static StandardCharacterCreationService NewService(out InMemorySpellRepository spellRepository)
        {
            spellRepository = SeededSpellRepository();
            IDiceRoller roller = new StandardDiceRoller();
            return new StandardCharacterCreationService(roller, spellRepository);
        }

        private static CharacterCreationPrompt WalkDetailedNonCaster(StandardCharacterCreationService service, string sessionId, string race, string className, string gender, string background, string abilityMethod, IReadOnlyList<string> abilityAssignmentOrder)
        {
            service.Start(sessionId, CharacterCreationMode.Detailed, "Kestrel");
            service.Answer(sessionId, race);
            service.Answer(sessionId, className);
            service.Answer(sessionId, gender);
            service.Answer(sessionId, background);
            var prompt = service.Answer(sessionId, abilityMethod);
            foreach (var ability in abilityAssignmentOrder)
            {
                prompt.Done.Should().BeFalse();
                prompt = service.Answer(sessionId, ability);
            }
            return prompt;
        }

        [Fact]
        public void Start_UnspecifiedMode_ReturnsFailure()
        {
            var service = NewService(out _);
            var result = service.Start("s1", CharacterCreationMode.Unspecified, "Kestrel");
            result.Success.Should().BeFalse();
        }

        [Fact]
        public void Start_ReturnsRacePromptWithAllFourRaces()
        {
            var service = NewService(out _);
            var result = service.Start("s1", CharacterCreationMode.Detailed, "Kestrel");

            result.Success.Should().BeTrue();
            result.Done.Should().BeFalse();
            result.Choices.Should().BeEquivalentTo(new[] { "Human", "Elf", "Dwarf", "Halfling" });
        }

        [Fact]
        public void Answer_UnknownSessionId_ReturnsFailure()
        {
            var service = NewService(out _);
            var result = service.Answer("does-not-exist", "Human");
            result.Success.Should().BeFalse();
        }

        [Theory]
        [InlineData("NotARealRace")]
        [InlineData("")]
        public void Answer_InvalidRace_ReturnsFailureAndDoesNotAdvance(string badAnswer)
        {
            var service = NewService(out _);
            service.Start("s1", CharacterCreationMode.Detailed, "Kestrel");

            var result = service.Answer("s1", badAnswer);

            result.Success.Should().BeFalse();
            // Session is still on the race prompt — a subsequent real
            // answer must still be accepted as a race, not skipped past.
            var retry = service.Answer("s1", "Human");
            retry.Success.Should().BeTrue();
            retry.PromptText.Should().Contain("class");
        }

        [Fact]
        public void DetailedMode_Fighter_FullSequence_ProducesCorrectCharacter()
        {
            var service = NewService(out _);
            var abilityOrder = new[] { "Strength", "Constitution", "Dexterity", "Intelligence", "Wisdom", "Charisma" };

            var result = WalkDetailedNonCaster(service, "fighter-session", "Human", "Fighter", "Male", "Soldier", "standard_array", abilityOrder);

            result.Success.Should().BeTrue();
            result.Done.Should().BeTrue();
            result.Character.Should().NotBeNull();
            var character = result.Character!;

            character.Name.Should().Be("Kestrel");
            character.Gender.Should().Be("Male");
            character.Team.Should().Be("Player");
            // Human: +1 to every ability. Standard array assigned in order
            // Str,Con,Dex,Int,Wis,Cha -> 15,14,13,12,10,8 respectively.
            character.AbilityScores.Strength.Should().Be(16);
            character.AbilityScores.Constitution.Should().Be(15);
            character.AbilityScores.Dexterity.Should().Be(14);
            // Fighter hit die 10, CON 15 -> modifier +2 -> HP 12.
            character.HitPoints.Max.Should().Be(12);
            character.HitPoints.Current.Should().Be(12);
            // AC = 10 + DEX modifier. DEX 14 -> modifier +2 -> AC 12.
            character.CombatStats!.ArmorClass.Should().Be(12);
            character.Inventory!.Gold.Should().Be(10); // Soldier's starting gold
            character.Spellcasting.Should().BeNull("Fighter is not a spellcaster");
        }

        [Theory]
        [InlineData("Fighter")]
        [InlineData("Rogue")]
        public void DetailedMode_NonCasterClasses_NeverPromptForSpells(string className)
        {
            var service = NewService(out _);
            var abilityOrder = new[] { "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma" };
            var result = WalkDetailedNonCaster(service, "session-" + className, "Human", className, "Male", "Criminal", "standard_array", abilityOrder);

            result.Done.Should().BeTrue("a non-caster has nothing left to ask after ability scores");
            result.PromptText.Should().BeNullOrEmpty();
        }

        [Fact]
        public void DetailedMode_Wizard_PromptsForThreeCantripsThenLeveledSpells_UsingRealRepositorySpells()
        {
            var service = NewService(out var spellRepository);
            service.Start("wizard-session", CharacterCreationMode.Detailed, "Elowen");
            service.Answer("wizard-session", "Elf");
            service.Answer("wizard-session", "Wizard");
            service.Answer("wizard-session", "Female");
            service.Answer("wizard-session", "Acolyte");
            var prompt = service.Answer("wizard-session", "standard_array");

            // Assign scores: Intelligence gets the highest (15) so the
            // prepared-spell-count formula below is exercised at its
            // real, non-trivial value.
            foreach (var ability in new[] { "Intelligence", "Constitution", "Dexterity", "Strength", "Wisdom", "Charisma" })
            {
                prompt.Done.Should().BeFalse();
                prompt = service.Answer("wizard-session", ability);
            }

            // Now three cantrip prompts.
            var realCantrips = spellRepository.GetAllSpells().Where(s => s.Level == 0 && s.Classes.Contains("Wizard")).Select(s => s.Name).ToList();
            for (var i = 0; i < 3; i++)
            {
                prompt.Success.Should().BeTrue();
                prompt.Done.Should().BeFalse();
                prompt.Choices.Should().OnlyContain(c => realCantrips.Contains(c), "every offered cantrip must be real per the spell repository");
                prompt = service.Answer("wizard-session", prompt.Choices![0]);
            }

            // Intelligence 15 -> modifier +2 -> prepared = 2 + 1 (level) = 3.
            var realLeveled = spellRepository.GetAllSpells().Where(s => s.Level == 1 && s.Classes.Contains("Wizard")).Select(s => s.Name).ToList();
            var leveledChosen = 0;
            while (!prompt.Done)
            {
                prompt.Choices.Should().OnlyContain(c => realLeveled.Contains(c));
                prompt = service.Answer("wizard-session", prompt.Choices![0]);
                leveledChosen++;
            }

            leveledChosen.Should().Be(3);
            prompt.Character!.Spellcasting.Should().NotBeNull();
            prompt.Character.Spellcasting!.PreparedSpellNames.Should().HaveCount(3 + 3); // 3 cantrips + 3 leveled
            foreach (var name in prompt.Character.Spellcasting.PreparedSpellNames)
            {
                (realCantrips.Contains(name) || realLeveled.Contains(name)).Should().BeTrue($"'{name}' must be a real spell from the repository, never invented");
            }
            prompt.Character.Spellcasting.Slots.Should().ContainSingle(s => s.Level == 1 && s.Max == 2 && s.Current == 2);
            prompt.Character.Spellcasting.CastingAbility.Should().Be(Ability.Intelligence);
        }

        [Fact]
        public void DetailedMode_Cleric_MinimumOneLeveledSpell_EvenWithNegativeModifier()
        {
            var service = NewService(out var spellRepository);
            service.Start("cleric-session", CharacterCreationMode.Detailed, "Bram");
            service.Answer("cleric-session", "Dwarf");
            service.Answer("cleric-session", "Cleric");
            service.Answer("cleric-session", "Male");
            service.Answer("cleric-session", "Acolyte");
            var prompt = service.Answer("cleric-session", "standard_array");

            // Wisdom deliberately gets the lowest standard-array value (8)
            // so its modifier is negative, to prove the "minimum of one"
            // floor from the SRD formula.
            foreach (var ability in new[] { "Strength", "Constitution", "Dexterity", "Intelligence", "Charisma", "Wisdom" })
                prompt = service.Answer("cleric-session", ability);

            for (var i = 0; i < 3; i++)
            {
                prompt.Choices.Should().NotBeEmpty();
                prompt = service.Answer("cleric-session", prompt.Choices![0]);
            }

            var leveledChosen = 0;
            while (!prompt.Done)
            {
                prompt = service.Answer("cleric-session", prompt.Choices![0]);
                leveledChosen++;
            }

            leveledChosen.Should().Be(1, "Wisdom 8 gives a -1 modifier, but the SRD formula floors prepared spells at 1");
        }

        [Fact]
        public void QuickMode_SurfacesExactlyThreePromptsThenDone_WithValidAutoRolledContent()
        {
            var service = NewService(out var spellRepository);
            var start = service.Start("quick-1", CharacterCreationMode.Quick, "Rin");
            start.Done.Should().BeFalse();
            start.PromptText.Should().Contain("race");

            var afterRace = service.Answer("quick-1", "Elf");
            afterRace.Done.Should().BeFalse();
            afterRace.PromptText.Should().Contain("class");

            var afterClass = service.Answer("quick-1", "Wizard");
            afterClass.Done.Should().BeFalse();
            afterClass.PromptText.Should().Contain("gender");
            afterClass.Choices.Should().BeEquivalentTo(new[] { "Male", "Female" },
                "gender is a fixed choice list, never a free-text box");

            var afterGender = service.Answer("quick-1", "Female");

            afterGender.Success.Should().BeTrue();
            afterGender.Done.Should().BeTrue("quick mode has nothing left to ask after race/class/gender");
            var character = afterGender.Character!;
            character.Gender.Should().Be("Female");
            character.Spellcasting.Should().NotBeNull("Wizard is always a spellcaster, even when auto-rolled");
            var realSpellNames = spellRepository.GetAllSpells().Select(s => s.Name).ToList();
            foreach (var name in character.Spellcasting!.PreparedSpellNames)
                realSpellNames.Should().Contain(name, "quick mode must never invent a spell name");
        }

        [Fact]
        public void QuickMode_NonCasterClass_HasNoSpellcasting()
        {
            var service = NewService(out _);
            service.Start("quick-2", CharacterCreationMode.Quick, "Doran");
            service.Answer("quick-2", "Dwarf");
            service.Answer("quick-2", "Fighter");
            var result = service.Answer("quick-2", "Male");

            result.Done.Should().BeTrue();
            result.Character!.Spellcasting.Should().BeNull();
        }

        [Fact]
        public void Gender_RejectsAnyValueNotInTheChoiceList_ThenAcceptsCanonicalCasing()
        {
            var service = NewService(out _);
            service.Start("g-1", CharacterCreationMode.Quick, "Ash");
            service.Answer("g-1", "Human");
            service.Answer("g-1", "Rogue");

            var rejected = service.Answer("g-1", "Attack Helicopter");
            rejected.Success.Should().BeFalse();
            rejected.Error.Should().Contain("not a valid gender");

            // Case-insensitive, normalized to the canonical option.
            var accepted = service.Answer("g-1", "female");
            accepted.Success.Should().BeTrue();
            accepted.Character!.Gender.Should().Be("Female");
        }

        [Fact]
        public void ListClassSpells_ReturnsOnlyThatClasssRealSpells_SeparatedByLevel()
        {
            var service = NewService(out _);
            var (cantrips, leveled) = service.ListClassSpells("Cleric");

            cantrips.Should().BeEquivalentTo(new[] { "Guidance", "Light", "Sacred Flame", "Thaumaturgy" });
            leveled.Should().BeEquivalentTo(new[] { "Cure Wounds", "Bless", "Guiding Bolt" });
            cantrips.Should().NotContain("Fire Bolt", "Fire Bolt is Wizard-only, not Cleric");
        }
    }
}
