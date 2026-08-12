using System;
using System.Threading.Tasks;
using OpenCombatEngine.Core.Interfaces.AI;
using OpenCombatEngine.Core.Interfaces.Combat;
using OpenCombatEngine.Core.Interfaces; // Added for ITurnManager
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.Actions;

namespace OpenCombatEngine.Implementation.AI
{
    public class CombatRunner : ICombatRunner
    {
        private readonly IAiController _controller;

        public CombatRunner(IAiController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        public async Task RunTurn(ITurnManager turnManager, IActionContext context)
        {
            ArgumentNullException.ThrowIfNull(turnManager);
            ArgumentNullException.ThrowIfNull(context);

            var creature = turnManager.CurrentCreature;
            if (creature == null) return;

            // A creature gets at most one Move and one Action/BonusAction worth of decisions per turn.
            // Each decision is re-derived after the previous one executes (e.g. move closer, then attack),
            // and is routed through ICreature.PerformAction so ActionStarted/ActionEnded fire correctly.
            const int maxActions = 2;
            for (int i = 0; i < maxActions; i++)
            {
                var decision = await _controller.DetermineAction(creature, context).ConfigureAwait(false);
                if (decision == null) break;

                var executionContext = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                    context.Source,
                    decision.Target,
                    context.Grid
                );

                var result = creature.PerformAction(decision.Action, executionContext);
                if (!result.IsSuccess)
                {
                    // If failed (e.g. invalid move or no resources left), break to prevent infinite loops
                    break;
                }
            }
        }
    }
}
