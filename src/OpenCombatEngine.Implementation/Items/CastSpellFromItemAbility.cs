using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Actions;

namespace OpenCombatEngine.Implementation.Items
{
    public class CastSpellFromItemAbility : IMagicItemAbility
    {
        private readonly ISpellRepository _spellRepository;
        private readonly string _spellName;
        private readonly IDiceRoller _diceRoller;

        public string Name => $"Cast {_spellName}";
        public string Description => $"Casts {_spellName} using charges.";
        public int Cost { get; }
        public ActionType ActionType => ActionType.Action; // Default to Action, could be refined if we knew spell casting time

        public CastSpellFromItemAbility(
            ISpellRepository spellRepository,
            string spellName,
            int cost,
            IDiceRoller diceRoller)
        {
            _spellRepository = spellRepository ?? throw new ArgumentNullException(nameof(spellRepository));
            _spellName = spellName ?? throw new ArgumentNullException(nameof(spellName));
            Cost = cost;
            _diceRoller = diceRoller ?? throw new ArgumentNullException(nameof(diceRoller));
        }

        public Result<bool> Execute(ICreature user, IActionContext context)
        {
            if (user == null) return Result<bool>.Failure("User cannot be null.");

            // 1. Resolve Spell
            var spellResult = _spellRepository.GetSpell(_spellName);
            if (!spellResult.IsSuccess)
            {
                return Result<bool>.Failure($"Spell '{_spellName}' not found in repository.");
            }
            var spell = spellResult.Value;

            // 2. Create Cast Action
            // We don't use slots for item casting, so slotLevel might be irrelevant or base level.
            // CastSpellAction usually consumes slots. We might need a specialized action or configure it to not consume slots.
            // However, CastSpellAction checks preparation and consumes slots.
            // We need a version that DOES NOT consume slots or check preparation.
            
            // For now, let's assume we use a subclass or modified CastSpellAction.
            // Actually, looking at CastSpellAction, it has virtual methods CheckPreparation and ConsumeResources.
            // We can create an anonymous subclass or a specific CastSpellFromItemAction.
            // OR, we can just implement the logic here directly using spell.Cast().
            
            // But we want to reuse the targeting and effect application logic in CastSpellAction.Execute.
            // Let's subclass CastSpellAction locally or create a CastSpellFromItemAction.
            
            var action = new CastSpellFromItemAction(spell, _diceRoller);
            var result = action.Execute(context);

            return result.IsSuccess ? Result<bool>.Success(true) : Result<bool>.Failure(result.Error);
        }

        private sealed class CastSpellFromItemAction : CastSpellAction
        {
            public CastSpellFromItemAction(ISpell spell, IDiceRoller diceRoller) 
                : base(spell, spell.Level, diceRoller)
            {
            }

            protected override bool CheckPreparation(ICreature source)
            {
                // Items don't require preparation
                return true;
            }

            protected override Result<bool> ConsumeResources(ICreature source)
            {
                // Items consume charges (handled by UseMagicItemAction), not spell slots.
                // So this action consumes nothing extra.
                return Result<bool>.Success(true);
            }
        }
    }
}
