using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Items
{
    public class ContainerItem : Item, IContainer
    {
        private readonly List<IItem> _contents = new();
        private readonly double _baseWeight;

        public IEnumerable<IItem> Contents => _contents.AsReadOnly();
        public double WeightCapacity { get; }
        public double WeightMultiplier { get; }

        public override double Weight => _baseWeight + (_contents.Sum(i => i.Weight) * WeightMultiplier);

        public ContainerItem(
            string name, 
            double baseWeight, 
            double weightCapacity, 
            double weightMultiplier = 1.0, 
            string description = "", 
            int value = 0)
            : base(name, description, baseWeight, value)
        {
            _baseWeight = baseWeight;
            WeightCapacity = weightCapacity;
            WeightMultiplier = weightMultiplier;
        }

        public Result<bool> AddItem(IItem item)
        {
            if (item == null) return Result<bool>.Failure("Item cannot be null.");
            if (item == this) return Result<bool>.Failure("Cannot put a container inside itself.");
            
            // Check capacity
            double currentContentWeight = _contents.Sum(i => i.Weight);
            if (currentContentWeight + item.Weight > WeightCapacity)
            {
                return Result<bool>.Failure($"Item too heavy. Capacity: {WeightCapacity}, Current: {currentContentWeight}, Item: {item.Weight}");
            }

            _contents.Add(item);
            return Result<bool>.Success(true);
        }

        public Result<bool> RemoveItem(IItem item)
        {
            if (item == null) return Result<bool>.Failure("Item cannot be null.");
            if (_contents.Remove(item))
            {
                return Result<bool>.Success(true);
            }
            return Result<bool>.Failure("Item not found in container.");
        }
    }
}
