using OpenCombatEngine.Core.Enums;
using System.Collections.Generic;

namespace OpenCombatEngine.Core.Interfaces.Items
{
    public interface IWeapon : IItem
    {
        string DamageDice { get; }
        DamageType DamageType { get; }
        IEnumerable<WeaponProperty> Properties { get; }

        /// <summary>
        /// The weapon's normal attack range in feet — always populated,
        /// never a bare guess. 5 for a standard melee weapon, 10 for one
        /// with <see cref="WeaponProperty.Reach"/> and no parsed ranged
        /// number, or the SRD "normal" range (the first number in an Open5e
        /// property string like "thrown (range 20/60)") for a weapon with
        /// <see cref="WeaponProperty.Thrown"/> or
        /// <see cref="WeaponProperty.Ammunition"/>. The "long" range number
        /// (the second one) is deliberately not modeled — see
        /// Open5eItemMapper's weapon-range parsing for where this is
        /// computed from real Open5e data.
        /// </summary>
        int Range { get; }
    }
}
