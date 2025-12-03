using System;
using System.Linq;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Items
{
    public class MagicItemRecharger
    {
        private readonly IDiceRoller _diceRoller;

        public MagicItemRecharger(IDiceRoller diceRoller)
        {
            _diceRoller = diceRoller ?? throw new ArgumentNullException(nameof(diceRoller));
        }

        public Result<int> RechargeItems(ICreature creature, RechargeFrequency frequency)
        {
            if (creature == null) return Result<int>.Failure("Creature cannot be null.");

            int rechargedCount = 0;
            var items = creature.Inventory.Items.OfType<IMagicItem>().Where(i => i.RechargeFrequency == frequency);

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.RechargeFormula)) continue;

                var rollResult = _diceRoller.Roll(item.RechargeFormula);
                if (rollResult.IsSuccess)
                {
                    item.Recharge(rollResult.Value.Total);
                    rechargedCount++;
                }
            }

            return Result<int>.Success(rechargedCount);
        }
    }
}
