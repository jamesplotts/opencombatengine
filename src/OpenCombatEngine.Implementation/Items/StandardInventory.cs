using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Items
{
    public class StandardInventory : IInventory
    {
        private readonly List<IItem> _items = new();

        public IEnumerable<IItem> Items => _items.AsReadOnly();

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
