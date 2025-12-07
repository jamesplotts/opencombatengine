using System.Collections.ObjectModel;

namespace OpenCombatEngine.Core.Models.States
{
    /// <summary>
    /// Serializable state for the level manager.
    /// </summary>
    /// <param name="ExperiencePoints">Current experience points.</param>
    /// <param name="Classes">List of class levels.</param>
    public record LevelManagerState(int ExperiencePoints, Collection<ClassLevelState> Classes);
}
