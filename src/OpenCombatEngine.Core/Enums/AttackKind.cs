namespace OpenCombatEngine.Core.Enums
{
    /// <summary>
    /// Which SRD weapon-attack rule a weapon's own properties are being
    /// checked against — see <c>WeaponAttackRules</c>
    /// (<c>OpenCombatEngine.Implementation.Actions</c>) for the actual
    /// legality/ability-modifier logic this selects between.
    /// </summary>
    public enum AttackKind
    {
        Melee,
        Ranged,
        /// <summary>
        /// A bonus-action attack with the equipped off-hand weapon (SRD
        /// Two-Weapon Fighting) — resolves against
        /// <see cref="OpenCombatEngine.Core.Interfaces.Items.IEquipmentManager.OffHand"/>,
        /// not <c>MainHand</c>.
        /// </summary>
        Offhand
    }
}
