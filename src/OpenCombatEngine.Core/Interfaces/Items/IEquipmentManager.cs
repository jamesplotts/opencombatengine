using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Core.Interfaces.Items
{
    public interface IEquipmentManager
    {
        IWeapon? MainHand { get; }
        IWeapon? OffHand { get; }
        IArmor? Armor { get; }
        IArmor? Shield { get; }

        Result<bool> EquipMainHand(IWeapon weapon);
        Result<bool> EquipOffHand(IWeapon weapon);
        Result<bool> EquipArmor(IArmor armor);
        Result<bool> EquipShield(IArmor shield);

        void UnequipMainHand();
        void UnequipOffHand();
        void UnequipArmor();
        void UnequipShield();
    }
}
