using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class LevelingTests
    {
        private readonly StandardCreature _creature;

        public LevelingTests()
        {
            _creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Test Creature",
                new StandardAbilityScores(),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );
        }

        [Fact]
        public void Should_Start_With_Zero_XP_And_Level_Zero()
        {
            _creature.LevelManager.ExperiencePoints.Should().Be(0);
            _creature.LevelManager.TotalLevel.Should().Be(0);
            _creature.LevelManager.Classes.Should().BeEmpty();
        }

        [Fact]
        public void AddExperience_Should_Increase_XP()
        {
            _creature.LevelManager.AddExperience(100);
            _creature.LevelManager.ExperiencePoints.Should().Be(100);
        }

        [Fact]
        public void LevelUp_Should_Increase_Level_And_Add_Class()
        {
            _creature.LevelManager.LevelUp("Fighter", 10);

            _creature.LevelManager.TotalLevel.Should().Be(1);
            _creature.LevelManager.Classes.Should().ContainKey("Fighter").WhoseValue.Should().Be(1);
        }

        [Fact]
        public void LevelUp_Should_Increase_Existing_Class_Level()
        {
            _creature.LevelManager.LevelUp("Fighter", 10);
            _creature.LevelManager.LevelUp("Fighter", 10);

            _creature.LevelManager.TotalLevel.Should().Be(2);
            _creature.LevelManager.Classes["Fighter"].Should().Be(2);
        }

        [Fact]
        public void LevelUp_Should_Support_Multiclassing()
        {
            _creature.LevelManager.LevelUp("Fighter", 10);
            _creature.LevelManager.LevelUp("Rogue", 8);

            _creature.LevelManager.TotalLevel.Should().Be(2);
            _creature.LevelManager.Classes["Fighter"].Should().Be(1);
            _creature.LevelManager.Classes["Rogue"].Should().Be(1);
        }

        [Fact]
        public void ProficiencyBonus_Should_Scale_With_Level()
        {
            // Level 1-4: +2
            _creature.LevelManager.LevelUp("Fighter", 10); // Level 1
            _creature.ProficiencyBonus.Should().Be(2);

            _creature.LevelManager.LevelUp("Fighter", 10); // Level 2
            _creature.LevelManager.LevelUp("Fighter", 10); // Level 3
            _creature.LevelManager.LevelUp("Fighter", 10); // Level 4
            _creature.ProficiencyBonus.Should().Be(2);

            // Level 5: +3
            _creature.LevelManager.LevelUp("Fighter", 10); // Level 5
            _creature.ProficiencyBonus.Should().Be(3);
        }

        [Fact]
        public void LevelUp_Should_Increase_Max_HP()
        {
            // Initial HP is 10
            // Con Mod is 0 (default 10)
            // Fighter d10 average is 6 (10/2 + 1)
            
            _creature.LevelManager.LevelUp("Fighter", 10);

            _creature.HitPoints.Max.Should().Be(16); // 10 + 6
            _creature.HitPoints.Current.Should().Be(16); // Assuming current increases too
        }

        [Fact]
        public void LevelUp_Should_Add_Hit_Die()
        {
            // Initial HitDiceTotal is 1 (from StandardHitPoints constructor default)
            
            _creature.LevelManager.LevelUp("Fighter", 10);

            _creature.HitPoints.HitDiceTotal.Should().Be(2); // 1 (initial) + 1 (added)
            // StandardLevelManager calls AddHitDie(hitDieSize).
            // StandardHitPoints.AddHitDie(count) adds count to Total.
            // Ah, StandardLevelManager passes hitDieSize (e.g. 10) to AddHitDie?
            // Let's check StandardLevelManager implementation.
        }
    }
}
