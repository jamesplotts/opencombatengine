using System;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Features;
using OpenCombatEngine.Core.Models.Events;
using OpenCombatEngine.Core.Models.Combat;

namespace OpenCombatEngine.Implementation.Features
{
    public class LifeStealingFeature : IFeature
    {
        public string Name => "Life Stealing";

        public void OnApplied(ICreature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);
            creature.ActionEnded += OnActionEnded;
        }

        public void OnRemoved(ICreature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);
            creature.ActionEnded -= OnActionEnded;
        }

        private void OnActionEnded(object? sender, ActionEventArgs e)
        {
            if (sender is not ICreature creature) return;
            
            // Check if result exists
            if (e.Result == null) return;

            // Check if it was an attack and Critical
            // We rely on string parsing for "Critical Hit!" because ActionResult doesn't expose IsCritical flag directly yet.
            // This is "loose coupling" via message protocol, or we upgrade ActionResult later.
            // Current StandardCreature.ResolveAttack: "Critical Hit! {totalDamage} damage!"
            
            if (e.Result.Message.Contains("Critical Hit", StringComparison.OrdinalIgnoreCase))
            {
                // Heal!
                creature.HitPoints.Heal(10);
                
                // Optional: Log/Notify? "Swallowed life energy!"
            }
        }

        public void OnStartTurn(ICreature creature) { }
        public void OnOutgoingAttack(ICreature source, AttackResult attack) { } // Could also check here but this is PRE-roll usually? No, OnOutgoingAttack modifies the roll. We want POST-roll.
    }
}
