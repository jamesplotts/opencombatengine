using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Items
{
    public class StandardInventory : IInventory
    {
        private readonly List<IItem> _items = new();
        private OpenCombatEngine.Core.Interfaces.Items.IEquipmentManager? _equipmentManager;

        public IEnumerable<IItem> Items => _items.AsReadOnly();

        public double TotalWeight => _items.Sum(i => i.Weight);

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
    }
}
