using System;
using System.Collections.Generic;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Items
{
    public class MagicItem : IMagicItem
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; }
        public string Description { get; }
        public double Weight { get; }
        public int Value { get; }
        public ItemType Type { get; }
        public bool RequiresAttunement { get; }
        public ICreature? AttunedCreature { get; private set; }
        
        private readonly List<OpenCombatEngine.Core.Interfaces.Features.IFeature> _features = new();
        public IEnumerable<OpenCombatEngine.Core.Interfaces.Features.IFeature> Features => _features;

        private readonly List<OpenCombatEngine.Core.Interfaces.Conditions.ICondition> _conditions = new();
        public IEnumerable<OpenCombatEngine.Core.Interfaces.Conditions.ICondition> Conditions => _conditions;

        public int Charges { get; private set; }
        public int MaxCharges { get; }
        public string RechargeRate { get; }
        public RechargeFrequency RechargeFrequency { get; }
        public string RechargeFormula { get; }

        public IWeapon? WeaponProperties { get; }
        public IArmor? ArmorProperties { get; }
        public IContainer? ContainerProperties { get; }
        public OpenCombatEngine.Core.Enums.EquipmentSlot? DefaultSlot { get; }

        public MagicItem(
            string name, 
            string description, 
            double weight, 
            int value, 
            ItemType itemType, 
            bool requiresAttunement,
            IEnumerable<OpenCombatEngine.Core.Interfaces.Features.IFeature>? features = null,
            IEnumerable<OpenCombatEngine.Core.Interfaces.Conditions.ICondition>? conditions = null,
            int maxCharges = 0,
            string rechargeRate = "",
            RechargeFrequency rechargeFrequency = RechargeFrequency.Unspecified,
            string rechargeFormula = "",
            IWeapon? weaponProperties = null,
            IArmor? armorProperties = null,
            IContainer? containerProperties = null,
            OpenCombatEngine.Core.Enums.EquipmentSlot? defaultSlot = null,
            IEnumerable<IMagicItemAbility>? abilities = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty", nameof(name));
            Name = name;
            Description = description;
            Weight = weight;
            Value = value;
            Type = itemType;
            RequiresAttunement = requiresAttunement;
            MaxCharges = maxCharges;
            Charges = maxCharges; // Start full
            RechargeRate = rechargeRate;
            RechargeFrequency = rechargeFrequency;
            RechargeFormula = rechargeFormula;
            WeaponProperties = weaponProperties;
            ArmorProperties = armorProperties;
            ContainerProperties = containerProperties;
            DefaultSlot = defaultSlot;
            
            if (features != null) _features.AddRange(features);
            if (conditions != null) _conditions.AddRange(conditions);
            if (abilities != null) _abilities.AddRange(abilities);
        }

        private readonly List<IMagicItemAbility> _abilities = new();
        public IEnumerable<IMagicItemAbility> Abilities => _abilities;

        public Result<bool> Attune(ICreature creature)
        {
            if (creature == null) return Result<bool>.Failure("Creature cannot be null.");
            if (!RequiresAttunement) return Result<bool>.Failure("Item does not require attunement.");
            if (AttunedCreature != null) return Result<bool>.Failure("Item is already attuned.");

            AttunedCreature = creature;
            return Result<bool>.Success(true);
        }

        public Result<bool> Unattune()
        {
            if (AttunedCreature == null) return Result<bool>.Failure("Item is not attuned.");
            
            AttunedCreature = null;
            return Result<bool>.Success(true);
        }

        public Result<int> ConsumeCharges(int amount)
        {
            if (amount <= 0) return Result<int>.Failure("Amount must be positive.");
            if (Charges < amount) return Result<int>.Failure($"Not enough charges. Has {Charges}, needs {amount}.");

            Charges -= amount;
            return Result<int>.Success(Charges);
        }

        public Result<int> Recharge(int amount)
        {
            if (amount < 0) return Result<int>.Failure("Amount cannot be negative.");
            
            Charges = Math.Min(MaxCharges, Charges + amount);
            return Result<int>.Success(Charges);
        }
    }
}
