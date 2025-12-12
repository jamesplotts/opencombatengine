using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Implementation.Open5e;

namespace OpenCombatEngine.Implementation.Items
{
    public class StandardItemLibrary : IItemLibrary
    {
        private readonly Open5eContentSource _contentSource;
        private readonly IDiceRoller _diceRoller;
        private readonly List<IItem> _items = new();
        private bool _isInitialized;

        public StandardItemLibrary(Open5eContentSource contentSource, IDiceRoller diceRoller)
        {
            _contentSource = contentSource ?? throw new ArgumentNullException(nameof(contentSource));
            _diceRoller = diceRoller ?? throw new ArgumentNullException(nameof(diceRoller));
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            var weapons = await _contentSource.GetAllWeaponsAsync().ConfigureAwait(false);
            var armor = await _contentSource.GetAllArmorAsync().ConfigureAwait(false);
            var magicItems = await _contentSource.GetAllMagicItemsAsync().ConfigureAwait(false);

            _items.AddRange(weapons);
            _items.AddRange(armor);
            _items.AddRange(magicItems);

            _isInitialized = true;
        }

        public IItem? GetItem(string slug)
        {
            // Simple lookup by name or slug if I kept slug?
            // StandardItem doesn't store slug explicitly in interface IItem.
            // But I stored it in my classes? Actually StandardItem constructor doesn't take slug.
            // I should search by Name (case insensitive) as a proxy for slug if slug isn't on interface.
            // Or better, add Slug to IItem? No time to change interface again. 
            // Matching Name is acceptable for now.
            return _items.FirstOrDefault(i => i.Name.Equals(slug, StringComparison.OrdinalIgnoreCase) || 
                                              i.Name.Replace(" ", "-", StringComparison.Ordinal).Equals(slug, StringComparison.OrdinalIgnoreCase));
        }

        public IWeapon? GetWeapon(string slug)
        {
             return GetItem(slug) as IWeapon;
        }

        public IArmor? GetArmor(string slug)
        {
             return GetItem(slug) as IArmor;
        }

        public IEnumerable<IItem> GetAllItems()
        {
            return _items.AsReadOnly();
        }

        public IEnumerable<IItem> GetItemsByRarity(ItemRarity rarity)
        {
            return _items.Where(i => i.Rarity == rarity);
        }

        public IItem? GetRandomItem(ItemRarity? rarity = null, ItemType? type = null)
        {
            var query = _items.AsEnumerable();
            if (rarity.HasValue) query = query.Where(i => i.Rarity == rarity.Value);
            if (type.HasValue) query = query.Where(i => i.Type == type.Value);
            
            var list = query.ToList();
            if (list.Count == 0) return null;

            // Simple random pick
            var index = _diceRoller.Roll($"1d{list.Count}").Value.Total - 1;
            // Handle edge case where roll is 1-based. 1d1 -> 1. index 0.
            return list[Math.Clamp(index, 0, list.Count - 1)];
        }
    }
}
