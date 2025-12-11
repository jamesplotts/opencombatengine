using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Content;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Content.Dtos;
using OpenCombatEngine.Implementation.Conditions;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Items;
using OpenCombatEngine.Implementation.Dice;

namespace OpenCombatEngine.Implementation.Content
{
    public class JsonMonsterImporter : IContentImporter<ICreature>
    {
        public Result<IEnumerable<ICreature>> Import(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return Result<IEnumerable<ICreature>>.Failure("Data cannot be empty.");
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                using var doc = JsonDocument.Parse(data);
                IEnumerable<MonsterDto>? monsterDtos = null;

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    monsterDtos = JsonSerializer.Deserialize<List<MonsterDto>>(data, options);
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("monster", out _))
                    {
                        var compendium = JsonSerializer.Deserialize<MonsterCompendiumDto>(data, options);
                        monsterDtos = compendium?.Monster;
                    }
                    else
                    {
                        var single = JsonSerializer.Deserialize<MonsterDto>(data, options);
                        if (single != null) monsterDtos = new List<MonsterDto> { single };
                    }
                }

                if (monsterDtos == null || !monsterDtos.Any())
                {
                    return Result<IEnumerable<ICreature>>.Success(Enumerable.Empty<ICreature>());
                }

                var creatures = new List<ICreature>();
                foreach (var dto in monsterDtos)
                {
                    if (string.IsNullOrWhiteSpace(dto.Name)) continue;
                    creatures.Add(Mappers.MonsterMapper.Map(dto));
                }

                return Result<IEnumerable<ICreature>>.Success(creatures);
            }
            catch (JsonException ex)
            {
                return Result<IEnumerable<ICreature>>.Failure($"JSON parsing error: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return Result<IEnumerable<ICreature>>.Failure($"Import error: {ex.Message}");
            }
        }
    }
}
