using System;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Models.Events;

namespace OpenCombatEngine.Demo
{
    public class CombatLogger
    {
        public CombatLogger(IGridManager grid)
        {
            grid.CreatureMoved += OnCreatureMoved;
        }

        public void RegisterCreature(ICreature creature)
        {
            creature.ActionStarted += OnActionStarted;
            creature.ActionEnded += OnActionEnded;
            // Conditions needs lambda or matching signature, but methods below are private.
            // We use RegisterCreatureWithContext instead for clearer closures.
            // Leaving this simple method restricted to Action/Movement events for now or removing it.
            // Let's rely on RegisterCreatureWithContext.
        }

        private void OnCreatureMoved(object? sender, MovedEventArgs e)
        {
            var creatureName = e.Creature?.Name ?? "Unknown";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"{creatureName} moved from {e.From} to {e.To}.");
            Console.ResetColor();
        }

        private void OnActionStarted(object? sender, ActionEventArgs e)
        {
            var creature = sender as ICreature;
            var creatureName = creature?.Name ?? "Unknown";
            var targetDesc = e.Target is ICreature tc ? tc.Name : e.Target?.ToString();
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{creatureName} started action: {e.ActionName} on {targetDesc}.");
            Console.ResetColor();
        }

        private void OnActionEnded(object? sender, ActionEventArgs e)
        {
            var creature = sender as ICreature;
            var creatureName = creature?.Name ?? "Unknown";
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"{creatureName} finished action: {e.ActionName}.");
            Console.ResetColor();
        }

        private void OnDamageTaken(object? sender, DamageTakenEventArgs e)
        {
            // The sender should be HitPoints. We need to find the creature? 
            // Or we just print the event info.
            // Ideally we'd know WHO took damage.
            // But HitPoints doesn't have a reference to its owner publicly?
            // Actually, in StandardCreature setup: HitPoints.DamageTaken...
            // StandardHitPoints doesn't hold Owner.
            // So we might just print damage amount.
            // However, in our RegisterCreature, we could capture the creature in a closure / wrapper?
            // Instead, let's assume sender is IHitPoints... still no owner.
            // Wait, StandardHitPoints constructor takes 'ICreature owner' in some versions? No, 'owner' is not in constructor shown before.
            // But 'RecordDeathSave' etc.
            // We have a gap here: identifying the source of the event if sender is a component.
            // But for this demo, we can just print "Something took damage" or fix the event args?
            // Cycle 48 defined `DamageTakenEventArgs`.
            // Let's assume we can't identify the creature from sender easily without changing code.
            // WORKAROUND: In RegisterCreature, subscribe with a lambda capturing the creature name.
            
            // (Re-registering with lambda instead of method group below)
        }
        
        // Re-implementing correctly with closures for context
        public void RegisterCreatureWithContext(ICreature creature)
        {
             creature.ActionStarted += (s, e) => LogActionStarted(creature, e);
             creature.ActionEnded += (s, e) => LogActionEnded(creature, e);
             
             // Wrap internal components events
             creature.HitPoints.DamageTaken += (s, e) => LogDamageTaken(creature, e);
             creature.HitPoints.Died += (s, e) => LogDied(creature, e);
             
             creature.Conditions.ConditionAdded += (s, e) => LogConditionAdded(creature, e);
             creature.Conditions.ConditionRemoved += (s, e) => LogConditionRemoved(creature, e);
        }

        private void LogActionStarted(ICreature subject, ActionEventArgs e)
        {
             var targetDesc = e.Target is ICreature tc ? tc.Name : (e.Target?.ToString() ?? "nothing");
             Console.ForegroundColor = ConsoleColor.Yellow;
             Console.WriteLine($"{subject.Name} prepares to use {e.ActionName} on {targetDesc}...");
             Console.ResetColor();
        }

        private void LogActionEnded(ICreature subject, ActionEventArgs e)
        {
             // Console.WriteLine($"{subject.Name} context done.");
        }

        private void LogDamageTaken(ICreature subject, DamageTakenEventArgs e)
        {
             Console.ForegroundColor = ConsoleColor.Red;
             Console.WriteLine($"{subject.Name} took {e.Amount} damage (Remaining: {subject.HitPoints.Current}/{subject.HitPoints.Max}).");
             Console.ResetColor();
        }

        private void LogDied(ICreature subject, EventArgs e)
        {
             Console.ForegroundColor = ConsoleColor.DarkRed;
             Console.WriteLine($"{subject.Name} has DIED!");
             Console.ResetColor();
        }

        private void LogConditionAdded(ICreature subject, ConditionEventArgs e)
        {
             Console.ForegroundColor = ConsoleColor.Magenta;
             Console.WriteLine($"{subject.Name} is now affected by {e.Condition.Name}!");
             Console.ResetColor();
        }

        private void LogConditionRemoved(ICreature subject, ConditionEventArgs e)
        {
             Console.ForegroundColor = ConsoleColor.DarkMagenta;
             Console.WriteLine($"{subject.Name} is no longer affected by {e.Condition.Name}.");
             Console.ResetColor();
        }
    }
}
