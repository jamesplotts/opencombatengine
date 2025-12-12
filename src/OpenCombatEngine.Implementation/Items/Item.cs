using System;
using OpenCombatEngine.Core.Interfaces.Items;

namespace OpenCombatEngine.Implementation.Items
{
    public class Item : IItem
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; }
        public string Description { get; }
        public virtual double Weight { get; }
        public int Value { get; }
        public virtual OpenCombatEngine.Core.Enums.ItemRarity Rarity { get; } = OpenCombatEngine.Core.Enums.ItemRarity.Common;
        public virtual OpenCombatEngine.Core.Enums.ItemType Type { get; } = OpenCombatEngine.Core.Enums.ItemType.Other;

        public Item(string name, string description = "", double weight = 0, int value = 0)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty.", nameof(name));
            Name = name;
            Description = description;
            Weight = weight;
            Value = value;
        }
    }
}
