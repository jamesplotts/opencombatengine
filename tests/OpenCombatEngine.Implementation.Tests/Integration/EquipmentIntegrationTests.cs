using FluentAssertions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Items;
using System;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Integration
{
    public class EquipmentIntegrationTests
    {
        [Fact]
        public void AC_Should_Update_With_Armor_And_Dex()
        {
            // Arrange
            var scores = new StandardAbilityScores(10, 14, 10, 10, 10, 10); // Dex +2
            var hp = new StandardHitPoints(10, 10, 0);
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Test", scores, hp); // Default AC 10 + Dex? No, default AC 10.
            
            // Act - Default
            // StandardCombatStats default base is 10. With Dex +2, it should be 10 (if using base) or 12 (if using unarmored logic).
            // Current implementation: Fallback returns _baseArmorClass (10). It ignores Dex if no armor.
            // Let's verify current behavior first.
            // UPDATE: Cycle 28 fixed this to correctly add Dex to unarmored AC (10 + Dex).
            creature.CombatStats.ArmorClass.Should().Be(12);

            // Act - Equip Leather Armor (11 + Dex)
            var leather = new Armor("Leather", 11, ArmorCategory.Light);
            creature.Equipment.EquipArmor(leather);

            // Assert
            creature.CombatStats.ArmorClass.Should().Be(13); // 11 + 2

            // Act - Equip Shield (+2)
            var shield = new Armor("Shield", 2, ArmorCategory.Shield);
            creature.Equipment.EquipShield(shield);

            // Assert
            creature.CombatStats.ArmorClass.Should().Be(15); // 13 + 2
        }

        [Fact]
        public void AC_Should_Respect_Dex_Cap()
        {
            // Arrange
            var scores = new StandardAbilityScores(10, 18, 10, 10, 10, 10); // Dex +4
            var hp = new StandardHitPoints(10, 10, 0);
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Test", scores, hp);

            // Act - Equip Medium Armor (14 + Dex max 2)
            var scaleMail = new Armor("Scale Mail", 14, ArmorCategory.Medium, dexterityCap: 2);
            creature.Equipment.EquipArmor(scaleMail);

            // Assert
            creature.CombatStats.ArmorClass.Should().Be(16); // 14 + 2 (capped)
        }
    }
}
