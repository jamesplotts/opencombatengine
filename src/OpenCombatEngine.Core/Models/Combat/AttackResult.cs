using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Interfaces.Creatures;

namespace OpenCombatEngine.Core.Models.Combat
{
    /// <summary>
    /// Represents the data of an attack attempt (rolls, source, target) before resolution.
    /// </summary>
    public class AttackResult
    {
        public ICreature Source { get; }
        public ICreature Target { get; }
        public int AttackRoll { get; }
        public bool IsCritical { get; }
        public IReadOnlyList<DamageRoll> Damage { get; }

        public AttackResult(ICreature source, ICreature target, int attackRoll, bool isCritical, IEnumerable<DamageRoll> damage)
        {
            Source = source;
            Target = target;
            AttackRoll = attackRoll;
            IsCritical = isCritical;
            Damage = damage?.ToList().AsReadOnly() ?? new List<DamageRoll>().AsReadOnly();
        }
    }
}
