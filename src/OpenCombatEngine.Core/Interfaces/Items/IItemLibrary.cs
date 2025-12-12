using System.Collections.Generic;
using OpenCombatEngine.Core.Enums;

namespace OpenCombatEngine.Core.Interfaces.Items
{
    public interface IItemLibrary
    {
        IItem? GetItem(string slug);
        IWeapon? GetWeapon(string slug);
        IArmor? GetArmor(string slug);
        
        IEnumerable<IItem> GetAllItems();
        IEnumerable<IItem> GetItemsByRarity(ItemRarity rarity);
        
        /// <summary>
        /// Gets a random item matching the criteria.
        /// </summary>
        IItem? GetRandomItem(ItemRarity? rarity = null, ItemType? type = null);
    }
}
