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
        public ItemType ItemType { get; }
        public bool RequiresAttunement { get; }
        public ICreature? AttunedCreature { get; private set; }
        
        private readonly List<OpenCombatEngine.Core.Interfaces.Features.IFeature> _features = new();
        public IEnumerable<OpenCombatEngine.Core.Interfaces.Features.IFeature> Features => _features;

        private readonly List<OpenCombatEngine.Core.Interfaces.Conditions.ICondition> _conditions = new();
        public IEnumerable<OpenCombatEngine.Core.Interfaces.Conditions.ICondition> Conditions => _conditions;

        public MagicItem(
            string name, 
            string description, 
            double weight, 
            int value, 
            ItemType itemType, 
            bool requiresAttunement,
            IEnumerable<OpenCombatEngine.Core.Interfaces.Features.IFeature>? features = null,
            IEnumerable<OpenCombatEngine.Core.Interfaces.Conditions.ICondition>? conditions = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty", nameof(name));
            Name = name;
            Description = description;
            Weight = weight;
            Value = value;
            ItemType = itemType;
            RequiresAttunement = requiresAttunement;
            
            if (features != null) _features.AddRange(features);
            if (conditions != null) _conditions.AddRange(conditions);
        }

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
    }
}
