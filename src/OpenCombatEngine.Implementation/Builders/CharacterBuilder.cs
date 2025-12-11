using System;
using System.Collections.Generic;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Classes;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Races;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;

namespace OpenCombatEngine.Implementation.Builders
{
    public class CharacterBuilder
    {
        private string _name = "Unnamed Character";
        private IRaceDefinition? _race;
        private IClassDefinition? _class;
        private StandardAbilityScores _abilityScores = new();
        private bool _takeAverageHp = true;

        public CharacterBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public CharacterBuilder WithRace(IRaceDefinition race)
        {
            _race = race;
            return this;
        }

        public CharacterBuilder WithClass(IClassDefinition classDefinition)
        {
            _class = classDefinition;
            return this;
        }

        public CharacterBuilder WithAbilityScores(int str, int dex, int con, int intel, int wis, int cha)
        {
            _abilityScores = new StandardAbilityScores(str, dex, con, intel, wis, cha);
            return this;
        }

        public CharacterBuilder WithAverageHp(bool takeAverage)
        {
            _takeAverageHp = takeAverage;
            return this;
        }

        private readonly List<OpenCombatEngine.Core.Interfaces.Items.IItem> _startingEquipment = new();

        public CharacterBuilder WithEquipment(IEnumerable<OpenCombatEngine.Core.Interfaces.Items.IItem> items)
        {
            if (items != null)
            {
                _startingEquipment.AddRange(items);
            }
            return this;
        }

        public CharacterBuilder WithEquipment(params OpenCombatEngine.Core.Interfaces.Items.IItem[] items)
        {
            return WithEquipment((IEnumerable<OpenCombatEngine.Core.Interfaces.Items.IItem>)items);
        }

        public ICreature Build()
        {
            var id = Guid.NewGuid().ToString();
            
            // Create base components
            // We need a temporary CombatStats for HitPoints, but CombatStats needs Creature.
            // Circular dependency if we strictly require Creature in CombatStats.
            // But StandardCombatStats allows null creature (it handles it).
            // However, the constructor signature changed to (ICreature creature, ...).
            // We can pass null!
            var hitPoints = new StandardHitPoints(10, 10, 1, new StandardCombatStats(creature: null!)); // Temporary HP
            var inventory = new StandardInventory();
            var turnManager = new StandardTurnManager(new StandardDiceRoller());

            var creature = new StandardCreature(
                id,
                _name,
                _abilityScores,
                hitPoints,
                inventory,
                turnManager,
                race: _race
            );

            // Apply Class (Level 1)
            if (_class != null)
            {
                // Reset HP to 0 before leveling up so Level 1 HP is calculated correctly
                // Actually, StandardHitPoints constructor sets Max and Current.
                // LevelUp increases Max.
                // We should probably start with 0 Max HP if we rely on LevelUp?
                // Or we can set it after.
                // StandardLevelManager.LevelUp adds to Max.
                // So if we start with 10, we get 10 + Level 1 HP.
                // We need to reset it.
                
                creature.LevelManager.LevelUp(_class, _takeAverageHp);
                
                // Fix the initial dummy HP
                if (creature.HitPoints is StandardHitPoints stdHp)
                {
                    // We started with 10 (dummy).
                    // LevelUp added Level 1 HP (e.g. 10 + Con).
                    // So current Max is 10 + Level 1 HP.
                    // We want just Level 1 HP.
                    // So we subtract 10.
                    
                    int currentMax = stdHp.Max;
                    int correctMax = currentMax - 10;
                    
                    // Ensure it's at least 1
                    if (correctMax < 1) correctMax = 1;
                    
                    stdHp.SetMax(correctMax);
                    stdHp.Heal(correctMax); // Ensure current is full
                }
            }

            // Add and Equip Starting Gear
            foreach (var item in _startingEquipment)
            {
                creature.Inventory.AddItem(item);
                
                if (item is OpenCombatEngine.Core.Interfaces.Items.IWeapon weapon)
                {
                    creature.Equipment.EquipMainHand(weapon);
                }
                else if (item is OpenCombatEngine.Core.Interfaces.Items.IArmor armor)
                {
                    if (armor.Category == OpenCombatEngine.Core.Interfaces.Items.ArmorCategory.Shield)
                    {
                        creature.Equipment.EquipShield(armor);
                    }
                    else
                    {
                        creature.Equipment.EquipArmor(armor);
                    }
                }
            }
            
            return creature;
        }
    }
}
