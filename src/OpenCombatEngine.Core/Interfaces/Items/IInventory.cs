using System.Collections.Generic;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Core.Interfaces.Items
{
    public interface IInventory
    {
        IEnumerable<IItem> Items { get; }
        double TotalWeight { get; }
        Result<bool> AddItem(IItem item);
        Result<bool> RemoveItem(IItem item);
        IItem? GetItem(string name);
    }
}
