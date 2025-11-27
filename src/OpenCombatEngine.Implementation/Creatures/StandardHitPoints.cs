using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Models.Events;
using OpenCombatEngine.Core.Models.States;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Creatures
{
    /// <summary>
    /// Standard implementation of hit points management.
    /// </summary>
    public class StandardHitPoints : IHitPoints, IStateful<HitPointsState>
    {
        private readonly ICombatStats? _combatStats;
        private readonly IDiceRoller _diceRoller;

        public int Max { get; private set; }
        public int Current { get; private set; }
        public int Temporary { get; private set; }
        
        public bool IsDead => DeathSaveFailures >= 3; // Simplified death logic
        public bool IsStable { get; private set; }
        
        public int DeathSaveSuccesses { get; private set; }
        public int DeathSaveFailures { get; private set; }

        public string HitDice { get; }
        public int HitDiceTotal { get; private set; }
        public int HitDiceRemaining { get; private set; }

        public event EventHandler<DamageTakenEventArgs>? DamageTaken;
        public event EventHandler<HealedEventArgs>? Healed;
#pragma warning disable CS0067 // The event 'StandardHitPoints.Died' is never used
        public event EventHandler<DeathEventArgs>? Died;
#pragma warning restore CS0067

        public StandardHitPoints(int max, int current, int temporary, ICombatStats? combatStats = null, string hitDice = "1d8", int hitDiceTotal = 1, IDiceRoller? diceRoller = null)
        {
            if (max <= 0) throw new ArgumentOutOfRangeException(nameof(max), "Max HP must be positive.");
            Max = max;
            Current = Math.Clamp(current, 0, Max);
            Temporary = Math.Max(0, temporary);
            _combatStats = combatStats;
            HitDice = hitDice;
            HitDiceTotal = hitDiceTotal > 0 ? hitDiceTotal : 1;
            HitDiceRemaining = HitDiceTotal; // Default to full
            _diceRoller = diceRoller ?? new OpenCombatEngine.Implementation.Dice.StandardDiceRoller();
        }

        public StandardHitPoints(int max, ICombatStats? combatStats = null, string hitDice = "1d8", int hitDiceTotal = 1, IDiceRoller? diceRoller = null) 
            : this(max, max, 0, combatStats, hitDice, hitDiceTotal, diceRoller) { }

        public StandardHitPoints(HitPointsState state, ICombatStats? combatStats = null, IDiceRoller? diceRoller = null)
        {
            ArgumentNullException.ThrowIfNull(state);
            Max = state.Max;
            Current = state.Current;
            Temporary = state.Temporary;
            _combatStats = combatStats;
            // State doesn't have HitDice info yet! We need to update HitPointsState or assume defaults/passed in.
            // For now, defaults. Ideally state has this.
            HitDice = "1d8"; 
            HitDiceTotal = 1;
            HitDiceRemaining = 1;
            _diceRoller = diceRoller ?? new OpenCombatEngine.Implementation.Dice.StandardDiceRoller();
        }

        public void TakeDamage(int amount, DamageType type = DamageType.Bludgeoning)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Damage amount cannot be negative.");
            if (amount == 0) return;

            // Apply Resistances/Vulnerabilities/Immunities if stats available
            if (_combatStats != null)
            {
                if (_combatStats.Immunities.Contains(type))
                {
                    amount = 0;
                }
                else if (_combatStats.Resistances.Contains(type))
                {
                    amount /= 2;
                }
                else if (_combatStats.Vulnerabilities.Contains(type))
                {
                    amount *= 2;
                }
            }

            if (amount == 0) return;

            int damageToTemp = Math.Min(Temporary, amount);
            Temporary -= damageToTemp;
            int remainingDamage = amount - damageToTemp;

            int damageToCurrent = Math.Min(Current, remainingDamage);
            Current -= damageToCurrent;

            DamageTaken?.Invoke(this, new DamageTakenEventArgs(amount, type, Current, Temporary));

            if (Current == 0 && !IsStable)
            {
                // Check for massive damage instant death (optional rule, not implementing yet)
                // Trigger death logic if needed
                // For now, just event
                Died?.Invoke(this, new DeathEventArgs()); 
            }
        }

        public void TakeDamage(int amount)
        {
            TakeDamage(amount, DamageType.Bludgeoning); // Default fallback
        }

        public void Heal(int amount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(amount);
            if (amount == 0) return;
            if (Current <= 0)
            {
                Current = 0;
                IsStable = false; // Healed from 0 means conscious? Or just stable? 5e: Regain consciousness.
                DeathSaveSuccesses = 0;
                DeathSaveFailures = 0;
            }

            int healAmount = Math.Min(Max - Current, amount);
            Current += healAmount;

            Healed?.Invoke(this, new HealedEventArgs(healAmount, Current));
        }

        public void AddTemporaryHitPoints(int amount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(amount);
            if (amount > Temporary)
            {
                Temporary = amount;
            }
        }

        public void RecordDeathSave(bool success, bool critical = false)
        {
            if (success)
            {
                DeathSaveSuccesses += critical ? 2 : 1;
                if (DeathSaveSuccesses >= 3)
                {
                    Stabilize();
                    DeathSaveSuccesses = 0;
                    DeathSaveFailures = 0;
                }
            }
            else
            {
                DeathSaveFailures += critical ? 2 : 1;
                if (DeathSaveFailures >= 3)
                {
                    // Dead
                    Died?.Invoke(this, new DeathEventArgs());
                }
            }
        }

        public void Stabilize()
        {
            IsStable = true;
            DeathSaveSuccesses = 0;
            DeathSaveFailures = 0;
        }

        public Result<int> UseHitDice(int amount)
        {
            if (amount <= 0) return Result<int>.Failure("Amount must be positive.");
            if (amount > HitDiceRemaining) return Result<int>.Failure("Not enough hit dice remaining.");

            int totalHealed = 0;
            for (int i = 0; i < amount; i++)
            {
                var roll = _diceRoller.Roll(HitDice);
                if (!roll.IsSuccess) return Result<int>.Failure($"Failed to roll hit die: {roll.Error}");
                
                totalHealed += roll.Value.Total;
            }

            HitDiceRemaining -= amount;
            return Result<int>.Success(totalHealed);
        }

        public void RecoverHitDice(int amount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(amount);
            HitDiceRemaining = Math.Min(HitDiceTotal, HitDiceRemaining + amount);
        }

        public void IncreaseMax(int amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
            Max += amount;
            Current += amount;
        }

        public void AddHitDie(int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive.");
            HitDiceTotal += count;
            HitDiceRemaining += count;
        }

        public HitPointsState GetState()
        {
            return new HitPointsState(Current, Max, Temporary);
        }
    }
}
