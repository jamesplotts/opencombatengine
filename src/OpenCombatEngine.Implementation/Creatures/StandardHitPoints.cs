using System;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.States;

namespace OpenCombatEngine.Implementation.Creatures
{
    /// <summary>
    /// Standard implementation of hit points management.
    /// </summary>
    public class StandardHitPoints : IHitPoints, IStateful<HitPointsState>
    {
        public int Current { get; private set; }
        public int Max { get; }
        public int Temporary { get; private set; }
        public bool IsDead => Current <= 0;

        /// <summary>
        /// Initializes a new instance of StandardHitPoints.
        /// </summary>
        /// <param name="max">Maximum hit points (must be positive)</param>
        /// <param name="current">Current hit points (defaults to max)</param>
        /// <param name="temporary">Temporary hit points (defaults to 0)</param>
        public StandardHitPoints(int max, int? current = null, int temporary = 0)
        {
            if (max <= 0)
                throw new ArgumentOutOfRangeException(nameof(max), max, "Max hit points must be positive");
            
            if (temporary < 0)
                throw new ArgumentOutOfRangeException(nameof(temporary), temporary, "Temporary hit points cannot be negative");

            Max = max;
            Temporary = temporary;
            
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
        public StandardHitPoints(HitPointsState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            
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
            }
        }

        /// <inheritdoc />
        public void Heal(int amount)
        {
            if (amount <= 0) return;
            if (IsDead) return; // Cannot heal dead creatures normally (needs resurrection magic)

            Current = Math.Min(Max, Current + amount);
        }
    }
}
