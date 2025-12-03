namespace OpenCombatEngine.Core.Models.Combat
{
    /// <summary>
    /// Represents the final outcome of an attack resolution (hit/miss, damage dealt).
    /// </summary>
    public class AttackOutcome
    {
        public bool IsHit { get; }
        public int DamageDealt { get; }
        public string Message { get; }

        public AttackOutcome(bool isHit, int damageDealt, string message)
        {
            IsHit = isHit;
            DamageDealt = damageDealt;
            Message = message;
        }
    }
}
