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

            // Determine Action
            var decision = await _controller.DetermineAction(creature, context).ConfigureAwait(false);

            if (decision != null)
            {
                // Execute Decision
                // We need to create a NEW context specific to this action execution.
                // The passed 'context' was for "Looking around" (e.g. had Grid, but Target was potentially empty/self).
                // The new context needs the Target from decision.
                
                var executionContext = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                    context.Source,
                    decision.Target,
                    context.Grid
                );

                var result = decision.Action.Execute(executionContext);
                
                // Logging result?
                // result.Value.ToString();
            }

            // End Turn Logic?
            // The runner 'Runs the Turn'. It should probably call NextTurn?
            // OR checks generic EndTurn logic.
            // Usually game loop calls NextTurn after RunTurn finishes.
            // We just perform the ACTION here.
            
            // Also: AI might want to move AND attack?
            // The DetermineAction currently returns ONE decision.
            // Ideally: Loop until DetermineAction returns null or no resources?
            // For Tier 1: One Action per turn is acceptable simplification, 
            // OR we loop:
            
            // int operations = 0;
            // while (operations < 2) 
            // {
            //    decision = ...
            //    if (decision == null) break;
            //    Execute...
            //    operations++;
            // }
            
            // For Zombie: Move OR Attack (if in range).
            // BasicAiController logic was "Attack IF in range, ELSE Move".
            // If it Moves, it ends turn. It doesn't Attack after moving (in current logic).
            // Proper Zombie logic: Move closer. If NOW in range, Attack.
            // This requires a Loop in Runner.
            
            // Let's implement a loop with a safety break.
            int maxActions = 2; // Move + Action
            for (int i = 0; i < maxActions; i++)
            {
                // Refresh Context? Grid state changed? Yes.
                // Context is mostly static references (GridManager), so ok.
                
                var loopDecision = await _controller.DetermineAction(creature, context).ConfigureAwait(false);
                if (loopDecision == null) break;

                // Execute
                var executionContext = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                    context.Source,
                    loopDecision.Target,
                    context.Grid
                );
                
                var result = loopDecision.Action.Execute(executionContext);
                if (!result.IsSuccess) 
                {
                    // If failed (e.g. invalid move), break to prevent infinite loops
                    break;
                }
            }
        }
    }
}
