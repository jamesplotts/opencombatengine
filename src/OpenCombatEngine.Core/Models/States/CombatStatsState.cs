using OpenCombatEngine.Core.Enums;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OpenCombatEngine.Core.Models.States
{
    /// <param name="ArmorClass">Armor Class.</param>
    /// <param name="InitiativeBonus">Initiative Bonus.</param>
    /// <param name="Speed">Movement Speed.</param>
    /// <param name="Resistances">Damage resistances.</param>
    /// <param name="Vulnerabilities">Damage vulnerabilities.</param>
    /// <param name="Immunities">Damage immunities.</param>
    public record CombatStatsState(
        int ArmorClass, 
        int InitiativeBonus, 
        int Speed,
        Collection<DamageType> Resistances,
        Collection<DamageType> Vulnerabilities,
        Collection<DamageType> Immunities
    );
}
