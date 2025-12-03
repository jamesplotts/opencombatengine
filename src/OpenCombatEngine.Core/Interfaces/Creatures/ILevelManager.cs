using System.Collections.Generic;

namespace OpenCombatEngine.Core.Interfaces.Creatures
{
    public interface ILevelManager
    {
        int TotalLevel { get; }
        int ExperiencePoints { get; }
        int ProficiencyBonus { get; }
        IReadOnlyDictionary<string, int> Classes { get; }

        void AddExperience(int amount);
        void LevelUp(string className, int hitDieSize, bool takeAverageHp = true);
    }
}
