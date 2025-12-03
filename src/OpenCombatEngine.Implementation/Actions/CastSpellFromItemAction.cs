using System;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Actions
{
    public class CastSpellFromItemAction : CastSpellAction
    {
        private readonly IMagicItem _item;
        private readonly int _chargesCost;

        public override string Name => $"Cast {Spell.Name} from {_item.Name}";
        public override string Description => $"Cast {Spell.Name} using {_chargesCost} charges from {_item.Name}.";

        public CastSpellFromItemAction(
            ISpell spell, 
            IMagicItem item, 
            int chargesCost = 1, 
            int? slotLevel = null, 
            IDiceRoller? diceRoller = null) 
            : base(spell, slotLevel, diceRoller)
        {
            _item = item ?? throw new ArgumentNullException(nameof(item));
            _chargesCost = chargesCost;
        }

        protected override bool CheckPreparation(ICreature source)
        {
            // Items do not require preparation
            return true;
        }

        protected override Result<bool> ConsumeResources(ICreature source)
        {
            // Validate charges
            if (_item.Charges < _chargesCost)
            {
                return Result<bool>.Failure($"Not enough charges in {_item.Name}. Needs {_chargesCost}, has {_item.Charges}.");
            }

            // Consume charges
            var consumeResult = _item.ConsumeCharges(_chargesCost);
            if (!consumeResult.IsSuccess)
            {
                return Result<bool>.Failure(consumeResult.Error);
            }
            
            return Result<bool>.Success(true);
        }
    }
}
