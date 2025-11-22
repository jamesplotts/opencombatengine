using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Creatures;
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
    public record StandardCombatStats : ICombatStats, IStateful<CombatStatsState>
    {
        private readonly int _baseArmorClass;
        private readonly IEquipmentManager? _equipment;
        private readonly IAbilityScores? _abilities;

        public int ArmorClass
        {
            get
            {
                if (_equipment?.Armor != null && _abilities != null)
                {
                    int dexMod = _abilities.GetModifier(OpenCombatEngine.Core.Enums.Ability.Dexterity);
                    int ac = _equipment.Armor.ArmorClass;
                    
                    if (_equipment.Armor.DexterityCap.HasValue)
                    {
                        dexMod = System.Math.Min(dexMod, _equipment.Armor.DexterityCap.Value);
                    }
                    
                    ac += dexMod;
                    
                    if (_equipment.Shield != null)
                    {
                        ac += _equipment.Shield.ArmorClass;
                    }
                    
                    return ac;
                }
                // Fallback to base AC + Dex if no armor but abilities present? 
                // Or just base AC provided in constructor?
                // 5e rules: Unarmored = 10 + Dex.
                // If _baseArmorClass is provided, use it.
                return _baseArmorClass;
            }
        }

        public int InitiativeBonus { get; }
        public int Speed { get; }
        public IReadOnlySet<OpenCombatEngine.Core.Enums.DamageType> Resistances { get; }
        public IReadOnlySet<OpenCombatEngine.Core.Enums.DamageType> Vulnerabilities { get; }
        public IReadOnlySet<OpenCombatEngine.Core.Enums.DamageType> Immunities { get; }

        public StandardCombatStats(
            int armorClass = 10, 
            int initiativeBonus = 0, 
            int speed = 30,
            IEnumerable<OpenCombatEngine.Core.Enums.DamageType>? resistances = null,
            IEnumerable<OpenCombatEngine.Core.Enums.DamageType>? vulnerabilities = null,
            IEnumerable<OpenCombatEngine.Core.Enums.DamageType>? immunities = null,
            IEquipmentManager? equipment = null,
            IAbilityScores? abilities = null)
        {
            _baseArmorClass = armorClass;
            InitiativeBonus = initiativeBonus;
            Speed = speed;
            Resistances = (resistances ?? Enumerable.Empty<OpenCombatEngine.Core.Enums.DamageType>()).ToHashSet();
            Vulnerabilities = (vulnerabilities ?? Enumerable.Empty<OpenCombatEngine.Core.Enums.DamageType>()).ToHashSet();
            Immunities = (immunities ?? Enumerable.Empty<OpenCombatEngine.Core.Enums.DamageType>()).ToHashSet();
            _equipment = equipment;
            _abilities = abilities;
        }

        public StandardCombatStats(CombatStatsState state)
        {
            System.ArgumentNullException.ThrowIfNull(state);
            _baseArmorClass = state.ArmorClass;
            InitiativeBonus = state.InitiativeBonus;
            Speed = state.Speed;
            Resistances = (state.Resistances ?? Enumerable.Empty<OpenCombatEngine.Core.Enums.DamageType>()).ToHashSet();
            Vulnerabilities = (state.Vulnerabilities ?? Enumerable.Empty<OpenCombatEngine.Core.Enums.DamageType>()).ToHashSet();
            Immunities = (state.Immunities ?? Enumerable.Empty<OpenCombatEngine.Core.Enums.DamageType>()).ToHashSet();
        }

        public CombatStatsState GetState()
        {
            return new CombatStatsState(
                ArmorClass, // Serialize the *current* AC? Or base? 
                // If we serialize current, and then deserialize without equipment context, it becomes the new base.
                // That seems correct for a snapshot.
                InitiativeBonus, 
                Speed, 
                new Collection<OpenCombatEngine.Core.Enums.DamageType>(Resistances.ToList()), 
                new Collection<OpenCombatEngine.Core.Enums.DamageType>(Vulnerabilities.ToList()), 
                new Collection<OpenCombatEngine.Core.Enums.DamageType>(Immunities.ToList())
            );
        }
    }
}
