using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.Combat;

namespace OpenCombatEngine.Core.Interfaces.Features
{
    /// <summary>
    /// Represents a feature that can modify combat behavior (e.g. Sneak Attack, Smite).
    /// </summary>
    public interface IFeature
    {
        /// <summary>
        /// Gets the name of the feature.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Called when the creature is making an attack, before resolution.
        /// Allows the feature to modify the attack (e.g. add damage).
        /// </summary>
        /// <param name="source">The creature making the attack.</param>
        /// <param name="attack">The attack data.</param>
        void OnOutgoingAttack(ICreature source, AttackResult attack);

        /// <summary>
        /// Called when the feature is added to a creature.
        /// </summary>
        /// <param name="creature">The creature the feature is applied to.</param>
        void OnApplied(ICreature creature);

        /// <summary>
        /// Called when the feature is removed from a creature.
        /// </summary>
        /// <param name="creature">The creature the feature is removed from.</param>
        void OnRemoved(ICreature creature);

        /// <summary>
        /// Called at the start of the creature's turn.
        /// </summary>
        /// <param name="creature">The creature starting their turn.</param>
        void OnStartTurn(ICreature creature);
    }
}
