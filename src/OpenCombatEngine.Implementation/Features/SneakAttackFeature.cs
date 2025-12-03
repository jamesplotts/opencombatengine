using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Features;
using OpenCombatEngine.Core.Models.Combat;

namespace OpenCombatEngine.Implementation.Features
{
    public class SneakAttackFeature : IFeature
    {
        public string Name => "Sneak Attack";
        
        private readonly IDiceRoller _diceRoller;
        private readonly int _diceCount; // e.g. 1 for 1d6
        private bool _usedThisTurn;

        // For testing/future grid integration
        public bool IsAllyAdjacent { get; set; }

        public SneakAttackFeature(IDiceRoller diceRoller, int diceCount = 1)
        {
            ArgumentNullException.ThrowIfNull(diceRoller);
            _diceRoller = diceRoller;
            _diceCount = diceCount;
        }

        public void OnStartTurn(ICreature creature)
        {
            _usedThisTurn = false;
        }

        public void OnApplied(ICreature creature)
        {
            // No setup needed
        }

        public void OnRemoved(ICreature creature)
        {
            // No cleanup needed
        }

        public void OnOutgoingAttack(ICreature source, AttackResult attack)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(attack);

            if (_usedThisTurn) return;
            if (attack.HasDisadvantage) return;

            bool canSneakAttack = attack.HasAdvantage || IsAllyAdjacent;
            if (!canSneakAttack) return;

            // Check weapon properties (Finesse or Ranged)
            var weapon = source.Equipment?.MainHand;
            if (weapon == null) return; 
            
            bool isFinesse = weapon.Properties.Contains(WeaponProperty.Finesse);
            bool isRanged = weapon.Properties.Contains(WeaponProperty.Range) || 
                            weapon.Name.Contains("Bow", StringComparison.OrdinalIgnoreCase) || 
                            weapon.Name.Contains("Crossbow", StringComparison.OrdinalIgnoreCase);
            
            if (!isFinesse && !isRanged) return;

            // Roll Sneak Attack Damage
            string diceNotation = $"{_diceCount}d6";
            var roll = _diceRoller.Roll(diceNotation);
            if (roll.IsSuccess)
            {
                int damage = roll.Value.Total;
                // Add to AttackResult.Damage
                attack.AddDamage(new DamageRoll(damage, weapon.DamageType));
                _usedThisTurn = true;
            }
        }
    }
}
