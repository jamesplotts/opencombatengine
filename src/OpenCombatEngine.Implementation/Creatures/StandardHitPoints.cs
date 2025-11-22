using System;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.Events;
using OpenCombatEngine.Core.Models.States;

namespace OpenCombatEngine.Implementation.Creatures
{
    /// <summary>
    /// Standard implementation of hit points management.
    /// </summary>
    public record StandardHitPoints : IHitPoints, IStateful<HitPointsState>
    {
        public event EventHandler<DamageTakenEventArgs>? DamageTaken;
        public event EventHandler<HealedEventArgs>? Healed;
        public event EventHandler<DeathEventArgs>? Died;

        public int Current { get; private set; }
        public int Max { get; }
        public int Temporary { get; private set; }

        
        // Dead if failures >= 3. (Or massive damage, but ignoring for now).
        // Note: Previously IsDead was Current <= 0. Now that's "Unconscious/Down".
        public bool IsDead => DeathSaveFailures >= 3;

        private readonly ICombatStats? _combatStats;

        /// <summary>
        /// Initializes a new instance of StandardHitPoints.
        /// </summary>
        /// <param name="max">Maximum hit points (must be positive)</param>
        /// <param name="current">Current hit points (defaults to max)</param>
        /// <param name="temporary">Temporary hit points (defaults to 0)</param>
        /// <param name="combatStats">Optional combat stats for resistance calculations</param>
        public StandardHitPoints(int max, int? current = null, int temporary = 0, ICombatStats? combatStats = null)
        {
            if (max <= 0)
                throw new ArgumentOutOfRangeException(nameof(max), max, "Max hit points must be positive");
            
            if (temporary < 0)
                throw new ArgumentOutOfRangeException(nameof(temporary), temporary, "Temporary hit points cannot be negative");

            Max = max;
            Temporary = temporary;
            _combatStats = combatStats;
            
            // If current is not specified, default to max. Otherwise clamp between 0 and max.
            // Note: In some systems you can have current > max, but typically not in standard 5e without temp HP.
            // We'll enforce 0 <= Current <= Max for now.
            int initialCurrent = current ?? max;
            if (initialCurrent < 0) initialCurrent = 0;
            if (initialCurrent > max) initialCurrent = max;
            
            Current = initialCurrent;
        }


        /// <summary>
        /// Initializes a new instance of StandardHitPoints from a state object.
        /// </summary>
        /// <param name="state">The state to restore from.</param>
        /// <param name="combatStats">Optional combat stats.</param>
        public StandardHitPoints(HitPointsState state, ICombatStats? combatStats = null)
        {
            ArgumentNullException.ThrowIfNull(state);
            _combatStats = combatStats;
            
            // Re-use logic from main constructor manually or call it if possible.
            // Since we can't validate before calling 'this', we have to duplicate logic or use a static helper.
            // Or just suppress CA1062 if we are sure 'this' handles it? No, 'this' expects values.
            // Actually, we can just call the main constructor, but we need to ensure state is not null first.
            // But we can't do statements before 'this'.
            // So we'll just implement the logic directly here to satisfy the analyzer and be safe.
            
            if (state.Max <= 0)
                throw new ArgumentOutOfRangeException(nameof(state), state.Max, "Max hit points must be positive");
            
            if (state.Temporary < 0)
                throw new ArgumentOutOfRangeException(nameof(state), state.Temporary, "Temporary hit points cannot be negative");

            Max = state.Max;
            Temporary = state.Temporary;
            Current = state.Current; // Trust the state, or re-validate? Let's trust state for now but ensure basic sanity.
            if (Current < 0) Current = 0;
            if (Current > Max) Current = Max;
        }

        /// <inheritdoc />
        public HitPointsState GetState()
        {
            return new HitPointsState(Current, Max, Temporary);
        }

        /// <inheritdoc />
        public void TakeDamage(int amount)
        {
            TakeDamage(amount, OpenCombatEngine.Core.Enums.DamageType.Unspecified);
        }

        /// <inheritdoc />
        public void TakeDamage(int amount, OpenCombatEngine.Core.Enums.DamageType type)
        {
            if (amount <= 0) return;

            // Apply Resistances/Vulnerabilities if stats are available
            if (_combatStats != null && type != OpenCombatEngine.Core.Enums.DamageType.Unspecified)
            {
                if (_combatStats.Immunities.Contains(type))
                {
                    amount = 0;
                }
                else
                {
                    if (_combatStats.Resistances.Contains(type))
                    {
                        amount /= 2;
                    }
                    
                    if (_combatStats.Vulnerabilities.Contains(type))
                    {
                        amount *= 2;
                    }
                }
            }

            if (amount <= 0) return;

            // First reduce temporary HP
            if (Temporary > 0)
            {
                int tempDamage = Math.Min(Temporary, amount);
                Temporary -= tempDamage;
                amount -= tempDamage;
            }

            // Then reduce current HP
            if (amount > 0)
            {
                Current = Math.Max(0, Current - amount);
                DamageTaken?.Invoke(this, new DamageTakenEventArgs(amount, Current));
            }

            if (IsDead)
            {
                Died?.Invoke(this, new DeathEventArgs());
            }
        }

        public int DeathSaveSuccesses { get; private set; }
        public int DeathSaveFailures { get; private set; }
        public bool IsStable { get; private set; }

        /// <inheritdoc />
        public void RecordDeathSave(bool success, bool critical = false)
        {
            if (Current > 0) return; // Only record if at 0 HP
            if (IsStable) return; // Only record if not stable

            if (success)
            {
                DeathSaveSuccesses += 1;
                // Nat 20 (critical success) logic is handled by caller (Heal 1 HP), 
                // but if we just want to record success here:
                // If 3 successes, stabilize.
                if (DeathSaveSuccesses >= 3)
                {
                    Stabilize();
                }
            }
            else
            {
                DeathSaveFailures += critical ? 2 : 1;
                if (DeathSaveFailures >= 3)
                {
                    // Die
                    // We don't set a flag, IsDead checks Failures.
                    // But we should fire Died event if we just crossed the threshold.
                    // Wait, IsDead is a property.
                    // If we just became dead, fire event.
                    // We need to check if we WERE dead before? No, IsDead checks Failures.
                    // So if we are now dead, fire event.
                    if (IsDead)
                    {
                        Died?.Invoke(this, new DeathEventArgs());
                    }
                }
            }
        }

        /// <inheritdoc />
        public void Stabilize()
        {
            IsStable = true;
            DeathSaveSuccesses = 0;
            DeathSaveFailures = 0;
        }

        /// <inheritdoc />
        public void Heal(int amount)
        {
            if (amount <= 0) return;
            if (IsDead) return; // Cannot heal dead creatures normally (needs resurrection magic)

            Current = Math.Min(Max, Current + amount);
            
            // Healing stabilizes and resets death saves
            if (Current > 0)
            {
                IsStable = false; // Not stable, just alive
                DeathSaveSuccesses = 0;
                DeathSaveFailures = 0;
            }

            Healed?.Invoke(this, new HealedEventArgs(amount, Current));
        }
    }
}
