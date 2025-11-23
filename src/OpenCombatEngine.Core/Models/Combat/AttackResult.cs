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
        public bool HasAdvantage { get; }
        public bool HasDisadvantage { get; }
        private readonly List<DamageRoll> _damage;
        public IReadOnlyList<DamageRoll> Damage => _damage;

        public AttackResult(ICreature source, ICreature target, int attackRoll, bool isCritical, bool hasAdvantage, bool hasDisadvantage, IEnumerable<DamageRoll> damage)
        {
            Source = source;
            Target = target;
            AttackRoll = attackRoll;
            IsCritical = isCritical;
            HasAdvantage = hasAdvantage;
            HasDisadvantage = hasDisadvantage;
            _damage = damage?.ToList() ?? new List<DamageRoll>();
        }

        public void AddDamage(DamageRoll damage)
        {
            _damage.Add(damage);
        }
    }
}
