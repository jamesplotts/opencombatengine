using System;
using OpenCombatEngine.Core.Enums;

namespace OpenCombatEngine.Core.Interfaces.Items
{
    public interface IItem
    {
        Guid Id { get; }
        string Name { get; }
        string Description { get; }
        double Weight { get; }

        /// <summary>
        /// The item's base market price, in copper pieces (the finest SRD
        /// currency denomination, so every real price is representable as
        /// a lossless integer — e.g. a torch at 1 cp is <c>1</c>, not 0).
        /// </summary>
        int Value { get; }
        ItemRarity Rarity { get; }
        ItemType Type { get; }
    }
}
