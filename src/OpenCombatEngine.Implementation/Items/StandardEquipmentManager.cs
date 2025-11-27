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
        
        public IItem? Head { get; private set; }
        public IItem? Neck { get; private set; }
        public IItem? Shoulders { get; private set; }
        public IItem? Hands { get; private set; }
        public IItem? Waist { get; private set; }
        public IItem? Feet { get; private set; }
        public IItem? Ring1 { get; private set; }
        public IItem? Ring2 { get; private set; }

        private readonly System.Collections.Generic.List<IMagicItem> _attunedItems = new();
        public System.Collections.Generic.IReadOnlyList<IMagicItem> AttunedItems => _attunedItems;

        private readonly ICreature _owner;

        private readonly System.Collections.Generic.Dictionary<OpenCombatEngine.Core.Enums.EquipmentSlot, IItem> _equippedItems = new();

        public StandardEquipmentManager(ICreature owner)
        {
            _owner = owner ?? throw new System.ArgumentNullException(nameof(owner));
        }

        public Result<bool> EquipMainHand(IItem item) => Equip(item, OpenCombatEngine.Core.Enums.EquipmentSlot.MainHand);
        public Result<bool> EquipOffHand(IItem item) => Equip(item, OpenCombatEngine.Core.Enums.EquipmentSlot.OffHand);
        public Result<bool> EquipArmor(IItem item) => Equip(item, OpenCombatEngine.Core.Enums.EquipmentSlot.Armor);
        public Result<bool> EquipShield(IItem item) => Equip(item, OpenCombatEngine.Core.Enums.EquipmentSlot.OffHand);

        public Result<bool> Equip(IItem item, OpenCombatEngine.Core.Enums.EquipmentSlot slot)
        {
            if (item == null) return Result<bool>.Failure("Item cannot be null.");
            
            Result<bool> result = Result<bool>.Failure("Unknown error");

            switch (slot)
            {
                case OpenCombatEngine.Core.Enums.EquipmentSlot.MainHand:
                    result = EquipMainHandInternal(item);
                    break;
                case OpenCombatEngine.Core.Enums.EquipmentSlot.OffHand:
                    result = EquipOffHandInternal(item);
                    break;
                case OpenCombatEngine.Core.Enums.EquipmentSlot.Armor:
                    result = EquipArmorInternal(item);
                    break;
                case OpenCombatEngine.Core.Enums.EquipmentSlot.Head:
                    Head = item; result = Result<bool>.Success(true); break;
                case OpenCombatEngine.Core.Enums.EquipmentSlot.Neck:
                    Neck = item; result = Result<bool>.Success(true); break;
                case OpenCombatEngine.Core.Enums.EquipmentSlot.Shoulders:
                    Shoulders = item; result = Result<bool>.Success(true); break;
                case OpenCombatEngine.Core.Enums.EquipmentSlot.Hands:
                    Hands = item; result = Result<bool>.Success(true); break;
                case OpenCombatEngine.Core.Enums.EquipmentSlot.Waist:
                    Waist = item; result = Result<bool>.Success(true); break;
                case OpenCombatEngine.Core.Enums.EquipmentSlot.Feet:
                    Feet = item; result = Result<bool>.Success(true); break;
                case OpenCombatEngine.Core.Enums.EquipmentSlot.Ring1:
                    Ring1 = item; result = Result<bool>.Success(true); break;
                case OpenCombatEngine.Core.Enums.EquipmentSlot.Ring2:
                    Ring2 = item; result = Result<bool>.Success(true); break;
                default:
                    return Result<bool>.Failure("Invalid slot.");
            }

            if (result.IsSuccess)
            {
                _equippedItems[slot] = item;
            }
            return result;
        }
        
        private Result<bool> EquipMainHandInternal(IItem item)
        {
            IWeapon? weapon = item as IWeapon;
            if (weapon == null && item is IMagicItem magicItem) weapon = magicItem.WeaponProperties;
            if (weapon == null) return Result<bool>.Failure("Item is not a weapon.");
            MainHand = weapon;
            return Result<bool>.Success(true);
        }

        private Result<bool> EquipOffHandInternal(IItem item)
        {
            // Check for shield first
            IArmor? shield = item as IArmor;
            if (shield == null && item is IMagicItem magicItem) shield = magicItem.ArmorProperties;
            
            if (shield != null && shield.Category == ArmorCategory.Shield)
            {
                Shield = shield;
                OffHand = null; // Shield takes offhand
                return Result<bool>.Success(true);
            }

            // Else weapon
            IWeapon? weapon = item as IWeapon;
            if (weapon == null && item is IMagicItem magicItem2) weapon = magicItem2.WeaponProperties;
            
            if (weapon != null)
            {
                OffHand = weapon;
                Shield = null; // Weapon takes offhand
                return Result<bool>.Success(true);
            }
            
            return Result<bool>.Failure("Item is not a weapon or shield.");
        }

        private Result<bool> EquipArmorInternal(IItem item)
        {
            IArmor? armor = item as IArmor;
            if (armor == null && item is IMagicItem magicItem) armor = magicItem.ArmorProperties;
            if (armor == null) return Result<bool>.Failure("Item is not armor.");
            if (armor.Category == ArmorCategory.Shield) return Result<bool>.Failure("Cannot equip shield as armor.");
            Armor = armor;
            return Result<bool>.Success(true);
        }

        public Result<bool> Unequip(OpenCombatEngine.Core.Enums.EquipmentSlot slot)
        {
            switch (slot)
            {
                case OpenCombatEngine.Core.Enums.EquipmentSlot.MainHand: MainHand = null; break;
                case OpenCombatEngine.Core.Enums.EquipmentSlot.OffHand: OffHand = null; Shield = null; break;
                case OpenCombatEngine.Core.Enums.EquipmentSlot.Armor: Armor = null; break;
                case OpenCombatEngine.Core.Enums.EquipmentSlot.Head: Head = null; break;
                case OpenCombatEngine.Core.Enums.EquipmentSlot.Neck: Neck = null; break;
                case OpenCombatEngine.Core.Enums.EquipmentSlot.Shoulders: Shoulders = null; break;
                case OpenCombatEngine.Core.Enums.EquipmentSlot.Hands: Hands = null; break;
                case OpenCombatEngine.Core.Enums.EquipmentSlot.Waist: Waist = null; break;
                case OpenCombatEngine.Core.Enums.EquipmentSlot.Feet: Feet = null; break;
                case OpenCombatEngine.Core.Enums.EquipmentSlot.Ring1: Ring1 = null; break;
                case OpenCombatEngine.Core.Enums.EquipmentSlot.Ring2: Ring2 = null; break;
            }
            _equippedItems.Remove(slot);
            return Result<bool>.Success(true);
        }

        public void UnequipMainHand() => Unequip(OpenCombatEngine.Core.Enums.EquipmentSlot.MainHand);
        public void UnequipOffHand() => Unequip(OpenCombatEngine.Core.Enums.EquipmentSlot.OffHand);
        public void UnequipArmor() => Unequip(OpenCombatEngine.Core.Enums.EquipmentSlot.Armor);
        public void UnequipShield() => Unequip(OpenCombatEngine.Core.Enums.EquipmentSlot.OffHand);

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

        public System.Collections.Generic.IEnumerable<IItem> GetEquippedItems()
        {
            return _equippedItems.Values;
        }
    }
}
