using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Items
{
    public class StandardEquipmentManager : IEquipmentManager
    {
        public IWeapon? MainHand { get; private set; }
        public IWeapon? OffHand { get; private set; }
        public IArmor? Armor { get; private set; }
        public IArmor? Shield { get; private set; }

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
    }
}
