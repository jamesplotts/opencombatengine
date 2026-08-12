using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Combat;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Implementation.Combat;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
using OpenCombatEngine.Implementation.Serialization;
using OpenCombatEngine.Implementation.Spells;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Serialization
{
    public class CombatSerializationTests
    {
        private StandardTurnManager _turnManager;
        private StandardCombatManager _combatManager;
        private StandardDiceRoller _diceRoller;
        private CombatSerializer _serializer;

        public CombatSerializationTests()
        {
            // Seeded so initiative order (Hero vs Goblin) is deterministic across runs.
            _diceRoller = new StandardDiceRoller { Seed = 3 };
            _turnManager = new StandardTurnManager(_diceRoller);
            _combatManager = new StandardCombatManager(_turnManager);
            _serializer = new CombatSerializer();
        }

        private ICreature CreateCreature(string name, int hp, int dex, string team)
        {
             var abilities = new StandardAbilityScores(10, dex, 10, 10, 10, 10);
             var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                name,
                abilities,
                new StandardHitPoints(hp, hp, 0), 
                new OpenCombatEngine.Implementation.Items.StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller()) // Unused during combat, but required by constructor
            );
            creature.Team = team;
            return creature;
        }

        [Fact]
        public void Should_Save_And_Load_Combat_State()
        {
            // 1. Setup Initial Encounter
            var hero = CreateCreature("Hero", 20, 15, "Heroes");
            var goblin = CreateCreature("Goblin", 10, 12, "Monsters");

            _combatManager.StartEncounter(new[] { hero, goblin });

            // 2. Mutate State
            // Advance turn a few times
            _turnManager.NextTurn(); // Round 1, Turn 0
            _turnManager.NextTurn(); // Round 1, Turn 1
            _turnManager.NextTurn(); // Round 2, Turn 0

            // Deal damage
            hero.HitPoints.TakeDamage(5); // Current: 15
            goblin.HitPoints.TakeDamage(2); // Current: 8

            // Spend Hero's Action and Bonus Action for the turn
            hero.ActionEconomy.UseAction();
            hero.ActionEconomy.UseBonusAction();

            // 3. Serialize
            var json = _serializer.Serialize(_combatManager);

            // 4. Create New Manager and Deserialize
            var newTurnManager = new StandardTurnManager(new StandardDiceRoller());
            var newCombatManager = new StandardCombatManager(newTurnManager);
            
            _serializer.Deserialize(json, newCombatManager);

            // 5. Verify
            newCombatManager.Participants.Should().HaveCount(2);

            var restoredHero = newCombatManager.Participants.First(p => p.Name == "Hero");
            restoredHero.HitPoints.Current.Should().Be(15);
            restoredHero.Team.Should().Be("Heroes");

            var restoredGoblin = newCombatManager.Participants.First(p => p.Name == "Goblin");
            restoredGoblin.HitPoints.Current.Should().Be(8);

            // Action economy must round-trip: a creature that already used its Action/Bonus
            // Action this turn should not come back with a free extra turn's worth of resources.
            restoredHero.ActionEconomy.HasAction.Should().BeFalse();
            restoredHero.ActionEconomy.HasBonusAction.Should().BeFalse();
            restoredHero.ActionEconomy.HasReaction.Should().BeTrue();
            restoredGoblin.ActionEconomy.HasAction.Should().BeTrue();

            // Verify Turn Manager
            newTurnManager.CurrentRound.Should().Be(2, "Should be round 2");
            // Turn order should be restored properly
            // Init: Hero (15 dex) > Goblin (12 dex)
            // Turns: Hero (0) -> Goblin (1) -> Hero (0)
            // State: Round 2, Current Index 0 (Hero)
            // BUT: The loop in test: StartEncounter runs NextTurn() to set index 0.
            // StartEncounter calls StartCombat -> NextTurn() => Index 0.
            // My NextTurn calls: 0 -> 1 -> 0 (Round 2).
            // So Current Turn should be Index 0 (Hero).
            
            // StandardTurnManager.StartCombat sets Round 1, Index -1, then NextTurn() => Index 0.
            // So:
            // Start: R1, T0 (Hero)
            // Next: R1, T1 (Goblin)
            // Next: R2, T0 (Hero)
            
            newTurnManager.CurrentCreature.Name.Should().Be("Hero");
        }

        [Fact]
        public void Should_Save_And_Load_Inventory_Equipment_And_Spellcasting()
        {
            // 1. Setup: item library + spell repository, mirroring how content is loaded in production.
            var itemLibrary = new TestItemLibrary();
            var sword = new Weapon("Longsword", "1d8", DamageType.Slashing);
            var armor = new Armor("Chain Mail", 16, ArmorCategory.Heavy);
            var ring = new MagicItem("Ring of Protection", "A ring", 0, 2000, ItemType.Ring, true, maxCharges: 5);
            itemLibrary.Add(sword);
            itemLibrary.Add(armor);
            itemLibrary.Add(ring);

            var spellRepository = new InMemorySpellRepository();
            var fireball = new Spell(
                "Fireball", 3, SpellSchool.Evocation, "1 Action", "150 feet", "V, S, M", "Instantaneous", "Boom", _diceRoller);
            spellRepository.AddSpell(fireball);

            // 2. Build a Hero with equipped gear, an attuned/charge-consumed magic item, and spellcasting state.
            var hero = (StandardCreature)CreateCreature("Hero", 20, 15, "Heroes");
            hero.Inventory.AddItem(sword);
            hero.Inventory.AddItem(armor);
            hero.Inventory.AddItem(ring);
            hero.Equipment.EquipMainHand(sword);
            hero.Equipment.EquipArmor(armor);
            hero.Equipment.AttuneItem(ring);
            ring.ConsumeCharges(2);

            var spellCaster = new StandardSpellCaster(
                Ability.Intelligence,
                a => hero.AbilityScores.GetModifier(a),
                () => hero.ProficiencyBonus
            );
            spellCaster.LearnSpell(fireball);
            spellCaster.PrepareSpell(fireball);
            spellCaster.SetSlots(3, 2);
            spellCaster.ConsumeSlot(3);
            spellCaster.SetConcentration(fireball);
            hero.SetSpellCaster(spellCaster);

            var goblin = CreateCreature("Goblin", 10, 12, "Monsters");
            _combatManager.StartEncounter(new[] { hero, goblin });

            var json = _serializer.Serialize(_combatManager);

            // 3. Restore WITH the item library/spell repository: full fidelity expected.
            var newCombatManager = new StandardCombatManager(new StandardTurnManager(new StandardDiceRoller()));
            _serializer.Deserialize(json, newCombatManager, spellRepository, itemLibrary);

            var restoredHero = newCombatManager.Participants.First(p => p.Name == "Hero");

            restoredHero.Equipment.MainHand.Should().NotBeNull();
            restoredHero.Equipment.MainHand!.Name.Should().Be("Longsword");
            restoredHero.Equipment.Armor.Should().NotBeNull();
            restoredHero.Equipment.Armor!.Name.Should().Be("Chain Mail");
            restoredHero.CombatStats.ArmorClass.Should().Be(18); // 16 base + 2 Dex mod (15 Dex, no cap on this armor)

            restoredHero.Equipment.AttunedItems.Should().ContainSingle();
            var restoredRing = restoredHero.Equipment.AttunedItems.Single();
            restoredRing.Name.Should().Be("Ring of Protection");
            restoredRing.Charges.Should().Be(3); // 5 max - 2 consumed before save

            restoredHero.Spellcasting.Should().NotBeNull();
            restoredHero.Spellcasting!.KnownSpells.Should().Contain(s => s.Name == "Fireball");
            restoredHero.Spellcasting.PreparedSpells.Should().Contain(s => s.Name == "Fireball");
            restoredHero.Spellcasting.GetMaxSlots(3).Should().Be(2);
            restoredHero.Spellcasting.GetSlots(3).Should().Be(1); // one consumed before save
            restoredHero.Spellcasting.ConcentratingOn.Should().NotBeNull();
            restoredHero.Spellcasting.ConcentratingOn!.Name.Should().Be("Fireball");

            // 4. Restore WITHOUT the item library/spell repository: graceful degradation, no throw.
            var degradedManager = new StandardCombatManager(new StandardTurnManager(new StandardDiceRoller()));
            Action act = () => _serializer.Deserialize(json, degradedManager);
            act.Should().NotThrow();

            var degradedHero = degradedManager.Participants.First(p => p.Name == "Hero");
            degradedHero.Inventory.Items.Should().HaveCount(3);
            degradedHero.Inventory.Items.Select(i => i.Name).Should().Contain("Longsword");
            degradedHero.Spellcasting.Should().BeNull();
        }

        [Fact]
        public void Should_Save_And_Load_Nested_Container_Contents()
        {
            // The library's copy of "Pouch" starts empty - if restore ends up with the Ruby
            // inside it, that proves the nested contents were actually rebuilt from state
            // rather than just reflecting some coincidentally-shared object.
            var itemLibrary = new TestItemLibrary();
            itemLibrary.Add(new ContainerItem("Pouch", baseWeight: 0.5, weightCapacity: 10));

            var heroPouch = new ContainerItem("Pouch", baseWeight: 0.5, weightCapacity: 10);
            heroPouch.AddItem(new Item("Ruby"));

            var hero = (StandardCreature)CreateCreature("Hero", 20, 15, "Heroes");
            hero.Inventory.AddItem(heroPouch);

            var goblin = CreateCreature("Goblin", 10, 12, "Monsters");
            _combatManager.StartEncounter(new[] { hero, goblin });

            var json = _serializer.Serialize(_combatManager);

            var newCombatManager = new StandardCombatManager(new StandardTurnManager(new StandardDiceRoller()));
            _serializer.Deserialize(json, newCombatManager, itemLibrary: itemLibrary);

            var restoredHero = newCombatManager.Participants.First(p => p.Name == "Hero");
            var restoredPouch = restoredHero.Inventory.Items.Single() as IContainer;

            restoredPouch.Should().NotBeNull();
            restoredPouch!.Contents.Should().ContainSingle(i => i.Name == "Ruby");
        }

        private sealed class TestItemLibrary : IItemLibrary
        {
            private readonly Dictionary<string, IItem> _items = new(StringComparer.OrdinalIgnoreCase);

            public void Add(IItem item) => _items[item.Name] = item;

            public IItem? GetItem(string slug) => _items.TryGetValue(slug, out var item) ? item : null;
            public IWeapon? GetWeapon(string slug) => GetItem(slug) as IWeapon;
            public IArmor? GetArmor(string slug) => GetItem(slug) as IArmor;
            public IEnumerable<IItem> GetAllItems() => _items.Values;
            public IEnumerable<IItem> GetItemsByRarity(ItemRarity rarity) => _items.Values.Where(i => i.Rarity == rarity);
            public IItem? GetRandomItem(ItemRarity? rarity = null, ItemType? type = null) => _items.Values.FirstOrDefault();
        }
    }
}
