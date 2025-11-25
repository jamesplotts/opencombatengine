using System.Collections.Generic;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Core.Interfaces.Items
{
    public interface IContainer : IItem
    {
        IEnumerable<IItem> Contents { get; }
        double WeightCapacity { get; }
        double WeightMultiplier { get; } // 1.0 for normal, 0.0 for fixed weight (like Bag of Holding)
        
        Result<bool> AddItem(IItem item);
        Result<bool> RemoveItem(IItem item);
    }
}
