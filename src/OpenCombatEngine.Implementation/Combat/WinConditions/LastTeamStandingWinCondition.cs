using System;
using System.Linq;
using OpenCombatEngine.Core.Interfaces.Combat;

namespace OpenCombatEngine.Implementation.Combat.WinConditions
{
    public class LastTeamStandingWinCondition : IWinCondition
    {
        public bool Check(ICombatManager combatManager)
        {
            ArgumentNullException.ThrowIfNull(combatManager);

            var activeTeams = combatManager.Participants
                .Where(p => p.HitPoints.Current > 0 && !p.HitPoints.IsDead)
                .Select(p => p.Team)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // End if 0 or 1 team remains.
            return activeTeams.Count <= 1;
        }

        public string GetWinner(ICombatManager combatManager)
        {
            ArgumentNullException.ThrowIfNull(combatManager);

             var activeTeams = combatManager.Participants
                .Where(p => p.HitPoints.Current > 0 && !p.HitPoints.IsDead)
                .Select(p => p.Team)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            
            return activeTeams.FirstOrDefault() ?? "Draw";
        }
    }
}
