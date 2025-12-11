using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Content;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Content.Dtos;
using OpenCombatEngine.Implementation.Spells;

namespace OpenCombatEngine.Implementation.Content
{
    public class JsonSpellImporter : IContentImporter<ISpell>
    {
        private readonly OpenCombatEngine.Core.Interfaces.Dice.IDiceRoller _diceRoller;

        public JsonSpellImporter(OpenCombatEngine.Core.Interfaces.Dice.IDiceRoller diceRoller)
        {
            _diceRoller = diceRoller ?? throw new ArgumentNullException(nameof(diceRoller));
        }

        public Result<IEnumerable<ISpell>> Import(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return Result<IEnumerable<ISpell>>.Failure("Data cannot be empty.");
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
                using var doc = JsonDocument.Parse(data);
                IEnumerable<SpellDto>? spellDtos = null;

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    spellDtos = JsonSerializer.Deserialize<List<SpellDto>>(data, options);
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("spell", out _))
                    {
                        var compendium = JsonSerializer.Deserialize<CompendiumDto>(data, options);
                        spellDtos = compendium?.Spell;
                    }
                    else
                    {
                        var single = JsonSerializer.Deserialize<SpellDto>(data, options);
                        if (single != null) spellDtos = new List<SpellDto> { single };
                    }
                }

                if (spellDtos == null || !spellDtos.Any())
                {
                    return Result<IEnumerable<ISpell>>.Success(Enumerable.Empty<ISpell>());
                }

                var spells = new List<ISpell>();
                foreach (var dto in spellDtos)
                {
                    if (string.IsNullOrWhiteSpace(dto.Name)) continue;

                    var spell = Mappers.SpellMapper.Map(dto, _diceRoller);
                    spells.Add(spell);
                }

                return Result<IEnumerable<ISpell>>.Success(spells);
            }
            catch (JsonException ex)
            {
                return Result<IEnumerable<ISpell>>.Failure($"JSON parsing error: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return Result<IEnumerable<ISpell>>.Failure($"Import error: {ex.Message}");
            }
        }
    }
}
