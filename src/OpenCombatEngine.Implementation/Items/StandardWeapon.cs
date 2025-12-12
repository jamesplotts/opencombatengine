using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Items;

namespace OpenCombatEngine.Implementation.Items
{
    public class StandardWeapon : StandardItem, IWeapon
    {
        public string DamageDice { get; }
        public DamageType DamageType { get; }
        public IEnumerable<WeaponProperty> Properties { get; }

        public StandardWeapon(
            Guid id,
            string name,
            string description,
            double weight,
            int value,
            ItemRarity rarity,
            string damageDice,
            DamageType damageType,
            IEnumerable<WeaponProperty>? properties = null)
            : base(id, name, description, weight, value, rarity, ItemType.Weapon)
        {
            DamageDice = damageDice ?? string.Empty;
            DamageType = damageType;
            Properties = (properties ?? Enumerable.Empty<WeaponProperty>()).ToList();
        }
    }
}
