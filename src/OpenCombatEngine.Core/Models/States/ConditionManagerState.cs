using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OpenCombatEngine.Core.Models.States
{
    /// <summary>
    /// Serializable state for condition manager.
    /// </summary>
    /// <param name="Conditions">List of active conditions.</param>
    public record ConditionManagerState(Collection<ConditionState> Conditions);
}
