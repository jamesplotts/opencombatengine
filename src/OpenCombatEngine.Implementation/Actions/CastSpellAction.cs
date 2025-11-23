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

        public Result<ActionResult> Execute(ICreature source, ICreature target)
        {
            if (source == null) return Result<ActionResult>.Failure("Source cannot be null.");
            
            var spellcasting = source.Spellcasting;
            if (spellcasting == null)
            {
                return Result<ActionResult>.Failure($"{source.Name} cannot cast spells.");
            }

            // Check preparation
            // Note: We check by name for simplicity, or reference if possible.
            // Spells are usually value objects or singletons?
            // Let's check by Name.
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

            // Cast spell
            var castResult = _spell.Cast(source, target);
            if (!castResult.IsSuccess)
            {
                return Result<ActionResult>.Failure($"Failed to cast {_spell.Name}: {castResult.Error}");
            }

            var resolution = castResult.Value;
            return Result<ActionResult>.Success(new ActionResult(true, resolution.Message));
        }
    }
}
