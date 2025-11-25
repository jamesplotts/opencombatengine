using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Effects;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Models.States;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenCombatEngine.Implementation.Creatures
{
    /// <summary>
    /// Standard implementation of combat statistics.
    /// </summary>
    public record StandardCombatStats : ICombatStats, OpenCombatEngine.Core.Interfaces.IStateful<CombatStatsState>
    {
        private readonly int _baseArmorClass;
        private readonly IEquipmentManager? _equipment;
        private readonly IAbilityScores? _abilities;
        private IEffectManager? _effects;

        public int ArmorClass
        {
            get
            {
                int ac = _baseArmorClass;

                if (_equipment?.Armor != null && _abilities != null)
                {
                    int dexMod = _abilities.GetModifier(Ability.Dexterity);
                    ac = _equipment.Armor.ArmorClass;
                    
                    if (_equipment.Armor.DexterityCap.HasValue)
                    {
                        dexMod = System.Math.Min(dexMod, _equipment.Armor.DexterityCap.Value);
                    }
                    
                    ac += dexMod;
                    
                    if (_equipment.Shield != null)
                    {
                        ac += _equipment.Shield.ArmorClass;
                    }
                }
                else if (_abilities != null)
                {
                    // Unarmored: 10 + Dex (if base is 10)
                    // If base is custom (e.g. natural armor), use that + Dex?
                    // For simplicity, if no armor, use base + Dex if base is 10, else just base.
                    // Actually, let's assume _baseArmorClass is the "natural" AC.
                    // If it's 10 (default), add Dex.
                    if (_baseArmorClass == 10)
                    {
                        ac += _abilities.GetModifier(Ability.Dexterity);
                    }
                }

                if (_effects != null)
                {
                    ac = _effects.ApplyStatBonuses(StatType.ArmorClass, ac);
                }

                return ac;
            }
        }

        public int InitiativeBonus
        {
            get
            {
                int bonus = _baseInitiativeBonus;
                if (_effects != null)
                {
                    bonus = _effects.ApplyStatBonuses(StatType.Initiative, bonus);
                }
                return bonus;
            }
        }
        private readonly int _baseInitiativeBonus;
        
        public int Speed 
        { 
            get
            {
                int speed = _baseSpeed;
                if (_effects != null)
                {
                    speed = _effects.ApplyStatBonuses(StatType.Speed, speed);
                }
                return speed;
            }
        }
        private readonly int _baseSpeed;
        
        private readonly HashSet<DamageType> _resistances;
        public IReadOnlySet<DamageType> Resistances => _resistances;
        
        private readonly HashSet<DamageType> _vulnerabilities;
        public IReadOnlySet<DamageType> Vulnerabilities => _vulnerabilities;
        
        private readonly HashSet<DamageType> _immunities;
        public IReadOnlySet<DamageType> Immunities => _immunities;

        public StandardCombatStats(
            int armorClass = 10, 
            int initiativeBonus = 0, 
            int speed = 30,
            IEnumerable<DamageType>? resistances = null,
            IEnumerable<DamageType>? vulnerabilities = null,
            IEnumerable<DamageType>? immunities = null,
            IEquipmentManager? equipment = null,
            IAbilityScores? abilities = null)
        {
            _baseArmorClass = armorClass;
            _baseInitiativeBonus = initiativeBonus;
            _baseSpeed = speed;
            _resistances = (resistances ?? Enumerable.Empty<DamageType>()).ToHashSet();
            _vulnerabilities = (vulnerabilities ?? Enumerable.Empty<DamageType>()).ToHashSet();
            _immunities = (immunities ?? Enumerable.Empty<DamageType>()).ToHashSet();
            _equipment = equipment;
            _abilities = abilities;
        }

        public StandardCombatStats(CombatStatsState state)
        {
            System.ArgumentNullException.ThrowIfNull(state);
            _baseArmorClass = state.ArmorClass;
            _baseInitiativeBonus = state.InitiativeBonus;
            _baseSpeed = state.Speed;
            _resistances = (state.Resistances ?? Enumerable.Empty<DamageType>()).ToHashSet();
            _vulnerabilities = (state.Vulnerabilities ?? Enumerable.Empty<DamageType>()).ToHashSet();
            _immunities = (state.Immunities ?? Enumerable.Empty<DamageType>()).ToHashSet();
        }

        public void SetEffectManager(IEffectManager effects)
        {
            _effects = effects;
        }

        public void AddResistance(DamageType type)
        {
            _resistances.Add(type);
        }

        public void RemoveResistance(DamageType type)
        {
            _resistances.Remove(type);
        }

        public CombatStatsState GetState()
        {
            // Note: We serialize the CALCULATED AC/Speed or the BASE?
            // Usually state should persist base values.
            // But ArmorClass property returns calculated.
            // For serialization, we probably want to save what was passed in constructor (base).
            // But we don't expose base AC publicly.
            // Let's save the current property value for now, but this might bake in bonuses.
            // Ideally we'd save _baseArmorClass.
            // Given the interface, let's just save _baseArmorClass if we can, but we can't access it easily here without changing interface or state.
            // Actually, let's save _baseArmorClass and _baseSpeed.
            return new CombatStatsState(
                _baseArmorClass, 
                InitiativeBonus, 
                _baseSpeed, 
                new Collection<DamageType>(_resistances.ToList()), 
                new Collection<DamageType>(_vulnerabilities.ToList()), 
                new Collection<DamageType>(_immunities.ToList())
            );
        }
    }
}
