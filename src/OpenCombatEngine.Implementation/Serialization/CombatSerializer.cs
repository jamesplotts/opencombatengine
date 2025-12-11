using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenCombatEngine.Core.Interfaces.Combat;
using OpenCombatEngine.Core.Models.States;

namespace OpenCombatEngine.Implementation.Serialization
{
    public class CombatSerializer
    {
        private readonly JsonSerializerOptions _options;

        public CombatSerializer()
        {
            _options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() },
                PropertyNameCaseInsensitive = true
            };
        }

        public string Serialize(ICombatManager combatManager)
        {
            ArgumentNullException.ThrowIfNull(combatManager);

            if (combatManager is OpenCombatEngine.Implementation.Combat.StandardCombatManager stdManager)
            {
                var state = stdManager.GetState();
                return JsonSerializer.Serialize(state, _options);
            }
            
            throw new NotSupportedException("Only StandardCombatManager is currently supported for serialization.");
        }

        public void Deserialize(string json, ICombatManager combatManager)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON cannot be empty.", nameof(json));
            ArgumentNullException.ThrowIfNull(combatManager);

            var state = JsonSerializer.Deserialize<CombatState>(json, _options);
            if (state == null) throw new JsonException("Failed to deserialize combat state.");

            if (combatManager is OpenCombatEngine.Implementation.Combat.StandardCombatManager stdManager)
            {
                stdManager.RestoreState(state);
            }
            else
            {
                throw new NotSupportedException("Only StandardCombatManager is currently supported for deserialization.");
            }
        }
    }
}
