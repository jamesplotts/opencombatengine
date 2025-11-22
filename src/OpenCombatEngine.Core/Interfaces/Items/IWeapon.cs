using OpenCombatEngine.Core.Enums;
using System.Collections.Generic;

namespace OpenCombatEngine.Core.Interfaces.Items
{
    public interface IWeapon : IItem
    {
        string DamageDice { get; }
        DamageType DamageType { get; }
        IEnumerable<string> Properties { get; }
    }
}
