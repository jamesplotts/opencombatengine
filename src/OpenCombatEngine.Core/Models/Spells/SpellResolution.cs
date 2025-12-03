using System.Collections.Generic;
using OpenCombatEngine.Core.Models.Combat;

namespace OpenCombatEngine.Core.Models.Spells
{
    public class SpellResolution
    {
        public bool WasCast { get; }
        public string Message { get; }
        public AttackResult? AttackResult { get; }
        // public SaveResult? SaveResult { get; } // We don't have SaveResult yet, maybe just a bool or a struct?
        // Let's define a simple SaveResult for now or use a property.
        public bool? SaveSucceeded { get; }
        public int DamageDealt { get; }

        public SpellResolution(bool wasCast, string message, AttackResult? attackResult = null, bool? saveSucceeded = null, int damageDealt = 0)
        {
            WasCast = wasCast;
            Message = message;
            AttackResult = attackResult;
            SaveSucceeded = saveSucceeded;
            DamageDealt = damageDealt;
        }
    }
}
