using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Models.States;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Items
{
    public class StandardInventory : IInventory, IStateful<InventoryState>
    {
        private readonly List<IItem> _items = new();
        private OpenCombatEngine.Core.Interfaces.Items.IEquipmentManager? _equipmentManager;

        public IEnumerable<IItem> Items => _items.AsReadOnly();

        public double TotalWeight => _items.Sum(i => i.Weight);

        public int Copper { get; private set; }
        public int Silver { get; private set; }
        public int Gold { get; private set; }
        public int Platinum { get; private set; }

        public Result<bool> AddCurrency(int copper, int silver, int gold, int platinum)
        {
            if (copper < 0 || silver < 0 || gold < 0 || platinum < 0)
                return Result<bool>.Failure("Cannot add a negative amount of currency.");

            Copper += copper;
            Silver += silver;
            Gold += gold;
            Platinum += platinum;
            return Result<bool>.Success(true);
        }

        public Result<bool> RemoveCurrency(int copper, int silver, int gold, int platinum)
        {
            if (copper < 0 || silver < 0 || gold < 0 || platinum < 0)
                return Result<bool>.Failure("Cannot remove a negative amount of currency.");
            if (copper > Copper || silver > Silver || gold > Gold || platinum > Platinum)
                return Result<bool>.Failure("Insufficient currency.");

            Copper -= copper;
            Silver -= silver;
            Gold -= gold;
            Platinum -= platinum;
            return Result<bool>.Success(true);
        }

        public void SetEquipmentManager(OpenCombatEngine.Core.Interfaces.Items.IEquipmentManager equipmentManager)
        {
            _equipmentManager = equipmentManager;
        }

        public Result<bool> AddItem(IItem item)
        {
            if (item == null) return Result<bool>.Failure("Item cannot be null.");
            _items.Add(item);
            return Result<bool>.Success(true);
        }

        public Result<bool> RemoveItem(IItem item)
        {
            if (item == null) return Result<bool>.Failure("Item cannot be null.");
            if (_items.Remove(item))
            {
                // An item leaving the inventory (dropped, sold, given away) can no longer be
                // equipped, or it would keep applying its bonuses to a creature that no longer owns it.
                _equipmentManager?.UnequipItem(item);
                return Result<bool>.Success(true);
            }
            return Result<bool>.Failure("Item not found in inventory.");
        }

        public IItem? GetItem(string name)
        {
            return _items.FirstOrDefault(i => i.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
        }

        public InventoryState GetState()
        {
            var itemStates = _items.Select(BuildItemState).ToList();
            return new InventoryState(new Collection<ItemInstanceState>(itemStates), Copper, Silver, Gold, Platinum);
        }

        private static ItemInstanceState BuildItemState(IItem item)
        {
            Collection<ItemInstanceState>? contents = null;
            if (item is IContainer container && container.Contents.Any())
            {
                contents = new Collection<ItemInstanceState>(container.Contents.Select(BuildItemState).ToList());
            }

            return new ItemInstanceState(item.Name, (item as IMagicItem)?.Charges, contents);
        }
    }
}
