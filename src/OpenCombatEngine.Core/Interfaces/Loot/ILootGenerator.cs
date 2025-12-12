using System.Collections.Generic;
using OpenCombatEngine.Core.Interfaces.Items;

namespace OpenCombatEngine.Core.Interfaces.Loot
{
    public interface ILootGenerator
    {
        LootBundle GenerateLoot(int challengeRating);
    }

    public class LootBundle
    {
        public int Copper { get; set; }
        public int Silver { get; set; }
        public int Gold { get; set; }
        public int Platinum { get; set; }
        public System.Collections.Generic.IList<IItem> Items { get; } = new System.Collections.Generic.List<IItem>();
    }
}
