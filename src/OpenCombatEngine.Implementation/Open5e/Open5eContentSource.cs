using System;
using System.Threading.Tasks;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Content.Dtos;
using OpenCombatEngine.Implementation.Content.Mappers;
using OpenCombatEngine.Core.Interfaces.Items;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// Fetches and maps every spell in the Open5e SRD spell list to
        /// <see cref="SpellDto"/> (not yet the final <c>ISpell</c> —
        /// <see cref="GetAllSpellsAsync"/> does that last step), following
        /// pagination until Open5e reports no further page. A page that
        /// fails to fetch, or a spell that fails to map, is skipped
        /// rather than aborting the whole fetch — callers populating a
        /// long-lived repository from this should prefer a partial
        /// result over none at all. Split out from
        /// <see cref="GetAllSpellsAsync"/> so a caller (e.g.
        /// <c>Open5eSpellCache</c>) can persist the DTOs themselves —
        /// <c>ISpell</c>/<c>Spell</c> isn't a plain serializable data
        /// type, <c>SpellDto</c> already is.
        /// </summary>
        public async Task<List<SpellDto>> GetAllSpellDtosAsync()
        {
            var result = new List<SpellDto>();
            int page = 1;
            while (true)
            {
                var response = await _client.GetSpellsAsync(page).ConfigureAwait(false);
                if (response == null || response.Results.Count == 0) break;

                foreach (var open5eSpell in response.Results)
                {
                    try
                    {
                        result.Add(Open5eAdapter.ToStandard(open5eSpell));
                    }
#pragma warning disable CA1031
                    catch (Exception)
                    {
                        // A single malformed spell entry shouldn't cost the rest of
                        // the SRD list — same "degrade gracefully" reasoning as a
                        // failed page fetch below.
                    }
#pragma warning restore CA1031
                }

                if (string.IsNullOrEmpty(response.Next)) break;
                page++;
            }
            return result;
        }

        /// <summary>
        /// Fetches and maps every spell in the Open5e SRD spell list, all
        /// the way to real <c>ISpell</c> instances — see
        /// <see cref="GetAllSpellDtosAsync"/> for the fetch/pagination
        /// behavior itself, which this simply maps through
        /// <c>SpellMapper</c>.
        /// </summary>
        public async Task<IEnumerable<ISpell>> GetAllSpellsAsync()
        {
            var dtos = await GetAllSpellDtosAsync().ConfigureAwait(false);
            return dtos.Select(dto => SpellMapper.Map(dto, _diceRoller));
        }

        public async Task<IEnumerable<IWeapon>> GetAllWeaponsAsync()
        {
            var result = new List<IWeapon>();
            int page = 1;
            while (true)
            {
                var response = await _client.GetWeaponsAsync(page).ConfigureAwait(false);
                if (response == null || response.Results.Count == 0) break;

                result.AddRange(response.Results.Select(Open5eItemMapper.MapWeapon));
                
                if (string.IsNullOrEmpty(response.Next)) break;
                page++;
            }
            return result;
        }

        public async Task<IEnumerable<IArmor>> GetAllArmorAsync()
        {
            var result = new List<IArmor>();
            int page = 1;
            while (true)
            {
                var response = await _client.GetArmorAsync(page).ConfigureAwait(false);
                if (response == null || response.Results.Count == 0) break;

                result.AddRange(response.Results.Select(Open5eItemMapper.MapArmor));
                
                if (string.IsNullOrEmpty(response.Next)) break;
                page++;
            }
            return result;
        }

        public async Task<IEnumerable<IItem>> GetAllMagicItemsAsync()
        {
            var result = new List<IItem>();
            int page = 1;
            while (true)
            {
                var response = await _client.GetMagicItemsAsync(page).ConfigureAwait(false);
                if (response == null || response.Results.Count == 0) break;

                result.AddRange(response.Results.Select(Open5eItemMapper.MapMagicItem));
                
                if (string.IsNullOrEmpty(response.Next)) break;
                page++;
            }
            return result;
        }
    }
}
