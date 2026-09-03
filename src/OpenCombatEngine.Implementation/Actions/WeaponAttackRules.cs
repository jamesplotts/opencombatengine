// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System;
using System.Linq;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Items;

namespace OpenCombatEngine.Implementation.Actions
{
    /// <summary>
    /// Real SRD weapon-attack legality and ability-modifier rules,
    /// shared by every entry point that resolves an equipped-weapon
    /// attack (the gRPC sidecar's <c>Attack</c> handler,
    /// <c>GetAvailableActions</c>' menu, and off-hand/secondary-weapon
    /// attacks) so they can't drift into two different answers for the
    /// same weapon. Originally inline only inside the sidecar's Attack
    /// handler; extracted here once a second caller needed the identical
    /// logic.
    /// </summary>
    public static class WeaponAttackRules
    {
        /// <summary>
        /// Whether weapon can be used for the given attack kind, per its
        /// own SRD Thrown/Ammunition properties — a weapon with
        /// Ammunition and no Thrown (a bow, a crossbow) is ranged-only;
        /// a weapon with neither Thrown nor Ammunition (a longsword) is
        /// melee-only. When false, <paramref name="reason"/> is a
        /// human-readable rejection explaining why.
        /// </summary>
        public static bool IsLegalFor(IWeapon weapon, AttackKind kind, out string? reason)
        {
            ArgumentNullException.ThrowIfNull(weapon);
            bool isThrown = weapon.Properties.Contains(WeaponProperty.Thrown);
            bool isAmmunition = weapon.Properties.Contains(WeaponProperty.Ammunition);

            if (kind == AttackKind.Melee && isAmmunition && !isThrown)
            {
                reason = $"{weapon.Name} cannot be used for a melee attack — it's a ranged-only weapon (Ammunition, no Thrown).";
                return false;
            }
            if (kind == AttackKind.Ranged && !isThrown && !isAmmunition)
            {
                reason = $"{weapon.Name} cannot be used for a ranged attack — it has neither Thrown nor Ammunition.";
                return false;
            }
            reason = null;
            return true;
        }

        /// <summary>
        /// SRD ability-modifier selection for a weapon attack: a true
        /// ranged weapon (Ammunition, not also Thrown — a bow/crossbow)
        /// always uses Dexterity. Every other case (melee, or a thrown
        /// weapon used at range) uses Strength, unless the weapon has
        /// Finesse, in which case the better of Strength/Dexterity
        /// applies — same rule either way it's used, per SRD.
        /// </summary>
        public static int AbilityModifier(ICreature attacker, IWeapon weapon, AttackKind kind)
        {
            ArgumentNullException.ThrowIfNull(attacker);
            ArgumentNullException.ThrowIfNull(weapon);
            int strengthModifier = attacker.AbilityScores.GetModifier(Ability.Strength);
            int dexterityModifier = attacker.AbilityScores.GetModifier(Ability.Dexterity);
            bool isThrown = weapon.Properties.Contains(WeaponProperty.Thrown);
            bool isAmmunition = weapon.Properties.Contains(WeaponProperty.Ammunition);
            bool trueRangedWeapon = kind == AttackKind.Ranged && isAmmunition && !isThrown;
            bool finesse = weapon.Properties.Contains(WeaponProperty.Finesse);
            return trueRangedWeapon
                ? dexterityModifier
                : finesse ? Math.Max(strengthModifier, dexterityModifier) : strengthModifier;
        }

        /// <summary>
        /// Builds the real <see cref="AttackAction"/> for attacker's
        /// weapon and kind — attack bonus (proficiency +
        /// <see cref="AbilityModifier"/>) and damage bonus
        /// (<see cref="AbilityModifier"/>), named deliberately not
        /// "Attack" so <see cref="AttackAction"/>'s own legacy
        /// <c>Equipment.MainHand</c>-override branch never fires (this
        /// method already resolved the correct weapon/kind/bonuses
        /// explicitly, from whichever hand weapon actually came from).
        /// </summary>
        public static AttackAction BuildAttackAction(ICreature attacker, IWeapon weapon, AttackKind kind, IDiceRoller diceRoller, ActionType actionType = ActionType.Action)
        {
            int abilityModifier = AbilityModifier(attacker, weapon, kind);
            int attackBonus = attacker.ProficiencyBonus + abilityModifier;
            string actionName = kind == AttackKind.Melee ? "Melee Attack" : "Ranged Attack";
            return new AttackAction(actionName, $"Attack with {weapon.Name}", attackBonus, weapon.DamageDice, weapon.DamageType, abilityModifier, diceRoller, actionType, weapon.Range);
        }
    }
}
