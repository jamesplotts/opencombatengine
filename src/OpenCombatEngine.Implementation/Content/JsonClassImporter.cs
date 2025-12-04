using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OpenCombatEngine.Core.Interfaces.Classes;
using OpenCombatEngine.Core.Interfaces.Content;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Classes;
using OpenCombatEngine.Implementation.Content.Dtos;

namespace OpenCombatEngine.Implementation.Content
{
    public class JsonClassImporter : IContentImporter<IClassDefinition>
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public Result<IEnumerable<IClassDefinition>> Import(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return Result<IEnumerable<IClassDefinition>>.Success(Enumerable.Empty<IClassDefinition>());

            try
            {
                var compendium = JsonSerializer.Deserialize<ClassCompendiumDto>(data, _options);
                if (compendium?.Class == null) return Result<IEnumerable<IClassDefinition>>.Success(Enumerable.Empty<IClassDefinition>());

                var classes = compendium.Class.Select(MapDtoToClass).Where(c => c != null).Cast<IClassDefinition>().ToList();
                return Result<IEnumerable<IClassDefinition>>.Success(classes);
            }
            catch (JsonException ex)
            {
                return Result<IEnumerable<IClassDefinition>>.Failure($"JSON parsing error: {ex.Message}");
            }
        }

        private static IClassDefinition? MapDtoToClass(ClassDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return null;

            int hitDie = dto.Hd?.Faces ?? 8; // Default to d8 if missing

            // TODO: Parse features from ClassTableGroups or other sources
            // For now, we return a class with no features, or maybe just basic ones if we can parse them.
            
            return new ClassDefinition(dto.Name, hitDie);
        }
    }
}
