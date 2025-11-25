using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Actions
{
    public class CastSpellAction : IAction
    {
        public string Name => "Cast Spell";
        public string Description => "Cast a spell from your known spells.";
        public ActionType Type => ActionType.Action; // Simplified for now, should depend on spell.CastingTime

        private readonly ISpell _spell;
        private readonly int _slotLevel;

        public CastSpellAction(ISpell spell, int? slotLevel = null)
        {
            _spell = spell ?? throw new ArgumentNullException(nameof(spell));
            _slotLevel = slotLevel ?? spell.Level;
        }

        public Result<ActionResult> Execute(IActionContext context)
        {
            if (context == null) return Result<ActionResult>.Failure("Context cannot be null.");
            var source = context.Source;
            
            // For now, assume single target spell.
            // If spell supports position, we need to handle that.
            // But ISpell.Cast currently takes ICreature target.
            // So we must enforce CreatureTarget for now.
            
            // Determine Target
            ICreature? creatureTarget = null;
            OpenCombatEngine.Core.Models.Spatial.Position? positionTarget = null;

            if (context.Target is OpenCombatEngine.Core.Models.Actions.CreatureTarget ct)
            {
                creatureTarget = ct.Creature;
                if (context.Grid != null)
                {
                    positionTarget = context.Grid.GetPosition(creatureTarget);
                }
            }
            else if (context.Target is OpenCombatEngine.Core.Models.Actions.PositionTarget pt)
            {
                positionTarget = pt.Position;
            }

            // Validation based on Spell Type (AOE vs Single Target)
            if (_spell.AreaOfEffect != null)
            {
                // AOE Spell
                if (positionTarget == null && creatureTarget == null)
                {
                     // Some AOE spells originate from caster (Self)
                     // If range is "Self", origin is caster position.
                     if (_spell.Range.Equals("Self", StringComparison.OrdinalIgnoreCase))
                     {
                         if (context.Grid != null)
                         {
                             positionTarget = context.Grid.GetPosition(source);
                         }
                     }
                     else
                     {
                         return Result<ActionResult>.Failure("AOE spell requires a target position or creature.");
                     }
                }
            }
            else
            {
                // Single Target Spell
                if (creatureTarget == null)
                {
                    return Result<ActionResult>.Failure("Target must be a creature for this spell.");
                }
            }

            var spellcasting = source.Spellcasting;
            if (spellcasting == null)
            {
                return Result<ActionResult>.Failure($"{source.Name} cannot cast spells.");
            }

            // Check preparation
            bool isPrepared = false;
            foreach (var prepared in spellcasting.PreparedSpells)
            {
                if (prepared.Name == _spell.Name)
                {
                    isPrepared = true;
                    break;
                }
            }

            if (!isPrepared)
            {
                return Result<ActionResult>.Failure($"Spell {_spell.Name} is not prepared.");
            }

            // Check slots
            if (!spellcasting.HasSlot(_slotLevel))
            {
                return Result<ActionResult>.Failure($"No spell slots available for level {_slotLevel}.");
            }

            // Consume slot
            var consumeResult = spellcasting.ConsumeSlot(_slotLevel);
            if (!consumeResult.IsSuccess)
            {
                return Result<ActionResult>.Failure(consumeResult.Error);
            }

            // Set Concentration
            if (_spell.RequiresConcentration)
            {
                spellcasting.SetConcentration(_spell);
            }

            // Execute Spell
            if (_spell.AreaOfEffect != null && context.Grid != null && positionTarget != null)
            {
                // AOE Execution
                var origin = positionTarget.Value;
                // If range is Self, origin is source. If range is distance, origin is target point.
                // For now, assume origin is the target point.
                
                var targets = context.Grid.GetCreaturesInShape(origin, _spell.AreaOfEffect);
                var messages = new System.Collections.Generic.List<string>();
                int hitCount = 0;

                foreach (var t in targets)
                {
                    // Skip caster if AOE excludes them? (Usually AOE includes everyone in area)
                    // But if it's Self (Cone), caster is origin but not target?
                    // Cone: Origin is caster, direction is target.
                    // Sphere: Origin is center.
                    // Let's assume Sphere for now (Fireball).
                    
                    var castResult = _spell.Cast(source, t);
                    if (castResult.IsSuccess)
                    {
                        messages.Add($"{t.Name}: {castResult.Value.Message}");
                        hitCount++;
                    }
                }
                
                return Result<ActionResult>.Success(new ActionResult(true, $"Cast {_spell.Name} (AOE). Hits: {hitCount}. Details: {string.Join("; ", messages)}"));
            }
            else
            {
                // Single Target Execution
                
                // LOS Check (only for single target or center of AOE if we checked it earlier, but let's keep it simple)
                if (context.Grid != null && creatureTarget != null)
                {
                    var sourcePos = context.Grid.GetPosition(source);
                    var targetPos = context.Grid.GetPosition(creatureTarget);

                    if (sourcePos != null && targetPos != null)
                    {
                        if (!context.Grid.HasLineOfSight(sourcePos.Value, targetPos.Value))
                        {
                            return Result<ActionResult>.Failure("No line of sight to target.");
                        }
                    }
                }

                var castResult = _spell.Cast(source, creatureTarget);
                if (!castResult.IsSuccess)
                {
                    return Result<ActionResult>.Failure($"Failed to cast {_spell.Name}: {castResult.Error}");
                }

                var resolution = castResult.Value;
                return Result<ActionResult>.Success(new ActionResult(true, resolution.Message));
            }
        }
    }
}
