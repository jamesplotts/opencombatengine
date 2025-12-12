using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Items;

namespace OpenCombatEngine.Implementation.Items
{
    public class StandardItem : IItem
    {
        public Guid Id { get; }
        public string Name { get; }
        public string Description { get; }
        public double Weight { get; }
        public int Value { get; }
        public ItemRarity Rarity { get; }
        public ItemType Type { get; }

        public StandardItem(
            Guid id,
            string name,
            string description,
            double weight,
            int value,
            ItemRarity rarity = ItemRarity.Common,
            ItemType type = ItemType.Other)
        {
            Id = id != Guid.Empty ? id : Guid.NewGuid();
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            Weight = weight;
            Value = value;
            Rarity = rarity;
            Type = type;
        }
    }
}
