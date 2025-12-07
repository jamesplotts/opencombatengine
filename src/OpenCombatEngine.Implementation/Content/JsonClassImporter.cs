using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OpenCombatEngine.Core.Interfaces.Classes;
using OpenCombatEngine.Core.Interfaces.Content;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Classes;
using OpenCombatEngine.Implementation.Content.Dtos;
using OpenCombatEngine.Implementation.Features;
using OpenCombatEngine.Core.Interfaces.Features;

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

            // TODO: Class features are typically linked via 'classFeature' entries in the JSON which are separate from the class definition itself in 5e.tools.
            // The 'classTableGroups' might contain references.
            // For this cycle, we will initialize with empty features as the parsing logic for class features requires resolving external references or a different JSON structure.
            // We'll add a simple "Class Feature" placeholder if we find 'classFeatures' just to show we touched it.
            
            var featuresByLevel = new Dictionary<int, IEnumerable<IFeature>>();
            var level1Features = new List<IFeature>();

            if (dto.Proficiency != null)
            {
                foreach (var prof in dto.Proficiency)
                {
                    // Create a feature for each proficiency
                    // We treat them as skill proficiencies for now, which covers armor/weapons as string checks too
                    level1Features.Add(new ProficiencyFeature($"Proficiency: {prof}", prof));
                }
            }

            // Parse Class Features
            if (dto.ClassFeatures != null)
            {
                foreach (var featureObj in dto.ClassFeatures)
                {
                    string? featureName = null;
                    string featureDesc = "";

                    if (featureObj is JsonElement element)
                    {
                        if (element.ValueKind == JsonValueKind.String)
                        {
                            featureName = element.GetString();
                        }
                        else if (element.ValueKind == JsonValueKind.Object)
                        {
                            // Try to get "name" or "classFeature"
                            if (element.TryGetProperty("name", out var nameProp))
                            {
                                featureName = nameProp.GetString();
                            }
                            else if (element.TryGetProperty("classFeature", out var cfProp))
                            {
                                // Format: Name|Class|Source|Level
                                var raw = cfProp.GetString();
                                if (!string.IsNullOrEmpty(raw))
                                {
                                    featureName = raw.Split('|')[0];
                                }
                            }
                        }
                    }
                    else if (featureObj is string str)
                    {
                        featureName = str;
                    }

                    if (!string.IsNullOrWhiteSpace(featureName))
                    {
                        var feature = FeatureFactory.CreateFeature(featureName, featureDesc) 
                                      ?? new TextFeature(featureName, featureDesc);
                        level1Features.Add(feature);
                    }
                }
            }

            if (level1Features.Count > 0)
            {
                featuresByLevel[1] = level1Features;
            }

            OpenCombatEngine.Core.Models.Spells.SpellList? spellList = null;
            if (dto.Spells != null && dto.Spells.Count > 0)
            {
                spellList = new OpenCombatEngine.Core.Models.Spells.SpellList(dto.Name, dto.Spells);
            }
            
            return new ClassDefinition(dto.Name, hitDie, featuresByLevel, spellList);
        }
    }
}
