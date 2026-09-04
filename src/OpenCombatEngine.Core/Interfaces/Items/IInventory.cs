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

        /// <summary>Copper pieces carried.</summary>
        int Copper { get; }

        /// <summary>Silver pieces carried.</summary>
        int Silver { get; }

        /// <summary>Gold pieces carried.</summary>
        int Gold { get; }

        /// <summary>Platinum pieces carried.</summary>
        int Platinum { get; }

        /// <summary>
        /// Adds currency (a treasure find, a reward) — always succeeds for
        /// non-negative amounts.
        /// </summary>
        Result<bool> AddCurrency(int copper, int silver, int gold, int platinum);

        /// <summary>
        /// Removes currency (spent, or moved to another creature via a
        /// transfer). Fails if any single denomination requested exceeds
        /// what's actually carried in that denomination — this does not
        /// make change across denominations (e.g. breaking a gold piece
        /// into 10 silver to cover a silver shortfall), a documented
        /// simplification.
        /// </summary>
        Result<bool> RemoveCurrency(int copper, int silver, int gold, int platinum);
    }
}
