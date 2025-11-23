using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Items
{
    public class StandardEquipmentManager : IEquipmentManager
    {
        public IWeapon? MainHand { get; private set; }
        public IWeapon? OffHand { get; private set; }
        public IArmor? Armor { get; private set; }
        public IArmor? Shield { get; private set; }

        private readonly System.Collections.Generic.List<IMagicItem> _attunedItems = new();
        public System.Collections.Generic.IReadOnlyList<IMagicItem> AttunedItems => _attunedItems;

        private readonly ICreature _owner;

        public StandardEquipmentManager(ICreature owner)
        {
            _owner = owner ?? throw new System.ArgumentNullException(nameof(owner));
        }

        public Result<bool> EquipMainHand(IWeapon weapon)
        {
            if (weapon == null) return Result<bool>.Failure("Weapon cannot be null.");
            MainHand = weapon;
            return Result<bool>.Success(true);
        }

        public Result<bool> EquipOffHand(IWeapon weapon)
        {
            if (weapon == null) return Result<bool>.Failure("Weapon cannot be null.");
            OffHand = weapon;
            return Result<bool>.Success(true);
        }

        public Result<bool> EquipArmor(IArmor armor)
        {
            if (armor == null) return Result<bool>.Failure("Armor cannot be null.");
            if (armor.Category == ArmorCategory.Shield) return Result<bool>.Failure("Cannot equip shield as armor.");
            Armor = armor;
            return Result<bool>.Success(true);
        }

        public Result<bool> EquipShield(IArmor shield)
        {
            if (shield == null) return Result<bool>.Failure("Shield cannot be null.");
            if (shield.Category != ArmorCategory.Shield) return Result<bool>.Failure("Item is not a shield.");
            Shield = shield;
            return Result<bool>.Success(true);
        }

        public void UnequipMainHand() => MainHand = null;
        public void UnequipOffHand() => OffHand = null;
        public void UnequipArmor() => Armor = null;
        public void UnequipShield() => Shield = null;

        public Result<bool> AttuneItem(IMagicItem item)
        {
            if (item == null) return Result<bool>.Failure("Item cannot be null.");
            if (!item.RequiresAttunement) return Result<bool>.Failure("Item does not require attunement.");
            if (_attunedItems.Contains(item)) return Result<bool>.Failure("Already attuned to this item.");
            if (_attunedItems.Count >= 3) return Result<bool>.Failure("Attunement slots full (3/3).");

            var result = item.Attune(_owner);
            if (!result.IsSuccess) return result;

            _attunedItems.Add(item);

            // Apply bonuses
            foreach (var feature in item.Features)
            {
                _owner.AddFeature(feature);
            }
            foreach (var condition in item.Conditions)
            {
                _owner.Conditions.AddCondition(condition);
            }

            return Result<bool>.Success(true);
        }

        public Result<bool> UnattuneItem(IMagicItem item)
        {
            if (item == null) return Result<bool>.Failure("Item cannot be null.");
            if (!_attunedItems.Contains(item)) return Result<bool>.Failure("Not attuned to this item.");

            var result = item.Unattune();
            if (!result.IsSuccess) return result;

            _attunedItems.Remove(item);

            // Remove bonuses
            foreach (var feature in item.Features)
            {
                _owner.RemoveFeature(feature);
            }
            foreach (var condition in item.Conditions)
            {
                _owner.Conditions.RemoveCondition(condition.Name);
            }

            return Result<bool>.Success(true);
        }
    }
}
