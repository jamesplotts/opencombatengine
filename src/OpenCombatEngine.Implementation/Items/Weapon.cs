using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Items;

namespace OpenCombatEngine.Implementation.Items
{
    public class Weapon : Item, IWeapon
    {
        public string DamageDice { get; }
        public DamageType DamageType { get; }
        public IEnumerable<WeaponProperty> Properties { get; }

        public Weapon(string name, string damageDice, DamageType damageType, IEnumerable<WeaponProperty>? properties = null, string description = "", double weight = 0, int value = 0)
            : base(name, description, weight, value)
        {
            if (string.IsNullOrWhiteSpace(damageDice)) throw new System.ArgumentException("Damage dice cannot be empty.", nameof(damageDice));
            DamageDice = damageDice;
            DamageType = damageType;
            Properties = (properties ?? Enumerable.Empty<WeaponProperty>()).ToList();
        }
    }
}
