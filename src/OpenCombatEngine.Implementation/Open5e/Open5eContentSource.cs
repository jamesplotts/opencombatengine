using System;
using System.Threading.Tasks;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Content.Mappers;

namespace OpenCombatEngine.Implementation.Open5e
{
    public class Open5eContentSource
    {
        private readonly Open5eClient _client;
        private readonly IDiceRoller _diceRoller;

        public Open5eContentSource(Open5eClient client, IDiceRoller diceRoller)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _diceRoller = diceRoller ?? throw new ArgumentNullException(nameof(diceRoller));
        }

        public async Task<Result<ISpell>> GetSpellAsync(string slug)
        {
            var open5eSpell = await _client.GetSpellAsync(slug).ConfigureAwait(false);
            if (open5eSpell == null)
            {
                return Result<ISpell>.Failure($"Spell '{slug}' not found.");
            }

            try
            {
                var dto = Open5eAdapter.ToStandard(open5eSpell);
                var spell = SpellMapper.Map(dto, _diceRoller);
                return Result<ISpell>.Success(spell);
            }
#pragma warning disable CA1031
            catch (Exception ex)
            {
                return Result<ISpell>.Failure($"Error mapping spell '{slug}': {ex.Message}");
            }
#pragma warning restore CA1031
        }

        public async Task<Result<ICreature>> GetMonsterAsync(string slug)
        {
            var open5eMonster = await _client.GetMonsterAsync(slug).ConfigureAwait(false);
            if (open5eMonster == null)
            {
                return Result<ICreature>.Failure($"Monster '{slug}' not found.");
            }

            try
            {
                var dto = Open5eAdapter.ToStandard(open5eMonster);
                var monster = MonsterMapper.Map(dto);
                return Result<ICreature>.Success(monster);
            }
#pragma warning disable CA1031
            catch (Exception ex)
            {
                return Result<ICreature>.Failure($"Error mapping monster '{slug}': {ex.Message}");
            }
#pragma warning restore CA1031
        }
    }
}
