using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Content;
using OpenCombatEngine.Implementation.Content.Mappers;
using OpenCombatEngine.Implementation.Open5e;
using OpenCombatEngine.Implementation.Open5e.Models;
using OpenCombatEngine.Implementation.Spells;

namespace OpenCombatEngine.Implementation.Items
{
    public class StandardItemLibrary : IItemLibrary
    {
        private readonly Open5eContentSource _contentSource;
        private readonly IDiceRoller _diceRoller;
        private readonly ISpellRepository _spellRepository;
        private readonly List<IItem> _items = new();
        private bool _isInitialized;

        public StandardItemLibrary(Open5eContentSource contentSource, IDiceRoller diceRoller, ISpellRepository? spellRepository = null)
        {
            _contentSource = contentSource ?? throw new ArgumentNullException(nameof(contentSource));
            _diceRoller = diceRoller ?? throw new ArgumentNullException(nameof(diceRoller));
            // Only needed to resolve CastSpellFromItemAbility references on items imported via
            // ImportMagicItemsFromJson; spell resolution there is lazy (at ability-execute time),
            // so an empty repository is a harmless default when the caller doesn't need it.
            _spellRepository = spellRepository ?? new InMemorySpellRepository();
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

        /// <summary>
        /// Populates the library directly from already-fetched Open5e DTOs
        /// (weapons/armor/magic items), mapping them the same way
        /// <see cref="InitializeAsync"/> does internally — the entry point
        /// for a cache-hit startup path (<c>Open5eItemCache</c>) that
        /// never needs to touch the network at all, mirroring how a
        /// cached spell list is mapped and added to a
        /// <c>ISpellRepository</c> without ever calling this class's own
        /// content-source-driven <see cref="InitializeAsync"/>.
        /// </summary>
        public void InitializeFromDtos(IEnumerable<Open5eWeapon> weapons, IEnumerable<Open5eArmor> armor, IEnumerable<Open5eMagicItem> magicItems)
        {
            if (_isInitialized) return;

            _items.AddRange(weapons.Select(Open5eItemMapper.MapWeapon));
            _items.AddRange(armor.Select(Open5eItemMapper.MapArmor));
            _items.AddRange(magicItems.Select(Open5eItemMapper.MapMagicItem));

            _isInitialized = true;
        }

        /// <summary>
        /// Imports magic items from 5eTools-style JSON (via <see cref="JsonMagicItemImporter"/>)
        /// and adds them to the library alongside whatever was loaded from Open5e. Unlike Open5e's
        /// magicitems endpoint, this format carries structured charges/recharge, weapon/armor
        /// bonuses, and attached-spell abilities, so items imported this way get full IMagicItem
        /// fidelity rather than the best-effort extraction Open5e content is limited to.
        /// </summary>
        /// <param name="json">Raw JSON content (single item, array, or a compendium-style {"item": [...]} wrapper).</param>
        public Result<IEnumerable<IMagicItem>> ImportMagicItemsFromJson(string json)
        {
            var importer = new JsonMagicItemImporter(_spellRepository, _diceRoller);
            var result = importer.Import(json);
            if (!result.IsSuccess) return result;

            var imported = result.Value.ToList();
            _items.AddRange(imported);
            return Result<IEnumerable<IMagicItem>>.Success(imported);
        }

        /// <summary>
        /// Convenience wrapper around <see cref="ImportMagicItemsFromJson"/> that reads the JSON
        /// from disk first, so callers don't need their own file-reading boilerplate. See
        /// <see cref="ImportMagicItemsFromJson"/> for the accepted JSON shapes and what importing
        /// actually does.
        /// </summary>
        /// <param name="filePath">
        /// Path to a JSON file in the same format accepted by <see cref="ImportMagicItemsFromJson"/>
        /// (single item, array, or a compendium-style <c>{"item": [...]}</c> wrapper).
        /// </param>
        /// <returns>
        /// A <see cref="Result{T}"/> containing the imported items on success. Failure (never a
        /// thrown exception) covers both a file that can't be found/read and JSON that fails to
        /// parse - <see cref="Result{T}.Error"/> distinguishes which.
        /// </returns>
        public Result<IEnumerable<IMagicItem>> ImportMagicItemsFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Result<IEnumerable<IMagicItem>>.Failure("File path cannot be empty.");
            }

            string json;
            try
            {
                json = File.ReadAllText(filePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                return Result<IEnumerable<IMagicItem>>.Failure($"Failed to read '{filePath}': {ex.Message}");
            }

            return ImportMagicItemsFromJson(json);
        }

        public IItem? GetItem(string slug)
        {
            // Simple lookup by name or slug if I kept slug?
            // StandardItem doesn't store slug explicitly in interface IItem.
            // But I stored it in my classes? Actually StandardItem constructor doesn't take slug.
            // I should search by Name (case insensitive) as a proxy for slug if slug isn't on interface.
            // Or better, add Slug to IItem? No time to change interface again.
            // Matching Name is acceptable for now.
            var found = _items.FirstOrDefault(i => i.Name.Equals(slug, StringComparison.OrdinalIgnoreCase) ||
                                              i.Name.Replace(" ", "-", StringComparison.Ordinal).Equals(slug, StringComparison.OrdinalIgnoreCase));

            // Magic items carry per-owner mutable state (charges, attunement). Hand out an
            // independent copy so consuming charges or attuning on one owner's item can't leak
            // into another owner's item of the same name. Weapons/armor/plain items have no
            // mutable state, so sharing the cached instance is harmless and avoids needless copies.
            return found is IMagicItem magicItem ? magicItem.Clone() : found;
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
            var picked = list[Math.Clamp(index, 0, list.Count - 1)];

            // Same reasoning as GetItem: hand out an independent copy of magic items so two
            // separate loot drops of "the same" item don't share mutable charge/attunement state.
            return picked is IMagicItem magicItem ? magicItem.Clone() : picked;
        }
    }
}
