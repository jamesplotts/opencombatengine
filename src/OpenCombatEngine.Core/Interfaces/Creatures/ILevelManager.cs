using System.Collections.Generic;
using OpenCombatEngine.Core.Interfaces.Classes;

namespace OpenCombatEngine.Core.Interfaces.Creatures
{
    public interface ILevelManager
    {
        int TotalLevel { get; }
        int ExperiencePoints { get; }
        int ProficiencyBonus { get; }
        IReadOnlyDictionary<IClassDefinition, int> Classes { get; }

        void AddExperience(int amount);
        void LevelUp(IClassDefinition classDefinition, bool takeAverageHp = true);
    }
}
