// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

namespace OpenCombatEngine.GrpcSidecar.Mapping;

/// <summary>
/// The JSON Schema (draft 2020-12) describing this engine's character-sheet
/// shape, returned by the System Engine gRPC contract's GetCharacterSchema
/// RPC so a client can render a schema-driven stat/inventory/spells/actions
/// panel without any system hardcoded into the UI (docs/design.md §4).
/// </summary>
/// <remarks>
/// Hand-written, not generated, to match exactly what
/// <see cref="CreatureStateJson"/> actually emits (camelCase property
/// names via its own JsonSerializerOptions). Engine-defined enum fields
/// (e.g. damage types, conditions, equipment slots) are described as
/// <c>"type": "string"</c> without an exhaustive enum member list, since
/// OpenCombatEngine.Core's own enums are the source of truth for those
/// values and duplicating every member here would drift out of sync with
/// them over time.
/// </remarks>
public static class CharacterSchema
{
    /// <summary>
    /// The schema_version this schema, and CreatureStateJson's wire format,
    /// both correspond to.
    /// </summary>
    public const string SchemaVersion = ActorMapping.SchemaVersion;

    /// <summary>
    /// The JSON Schema document, as a JSON string.
    /// </summary>
    public const string Json = """
    {
      "$schema": "https://json-schema.org/draft/2020-12/schema",
      "$id": "https://github.com/jamesplotts/OpenCombatEngine/schemas/creature-state.json",
      "title": "OpenCombatEngine CreatureState",
      "type": "object",
      "required": ["id", "name", "team", "abilityScores", "hitPoints"],
      "properties": {
        "id": { "type": "string", "format": "uuid" },
        "name": { "type": "string" },
        "team": { "type": "string" },
        "challengeRating": {
          "type": ["number", "null"],
          "description": "SRD challenge rating (0, 0.125, 0.25, 0.5, or a whole number up to 30). Set this for a monster/NPC created via create_npc, the same way every other stat is authored — it's required before generate_loot can include this character. Omit/null for a player character, which has no CR in 5e."
        },
        "abilityScores": {
          "type": "object",
          "required": ["strength", "dexterity", "constitution", "intelligence", "wisdom", "charisma"],
          "properties": {
            "strength": { "type": "integer" },
            "dexterity": { "type": "integer" },
            "constitution": { "type": "integer" },
            "intelligence": { "type": "integer" },
            "wisdom": { "type": "integer" },
            "charisma": { "type": "integer" }
          }
        },
        "hitPoints": {
          "type": "object",
          "required": ["current", "max", "temporary"],
          "properties": {
            "current": { "type": "integer" },
            "max": { "type": "integer" },
            "temporary": { "type": "integer" }
          }
        },
        "combatStats": {
          "type": ["object", "null"],
          "properties": {
            "armorClass": { "type": "integer" },
            "initiativeBonus": { "type": "integer" },
            "speed": { "type": "integer" },
            "resistances": { "type": "array", "items": { "type": "string" } },
            "vulnerabilities": { "type": "array", "items": { "type": "string" } },
            "immunities": { "type": "array", "items": { "type": "string" } }
          }
        },
        "conditions": {
          "type": ["object", "null"],
          "properties": {
            "conditions": {
              "type": "array",
              "items": {
                "type": "object",
                "required": ["name", "description", "durationRounds", "type"],
                "properties": {
                  "name": { "type": "string" },
                  "description": { "type": "string" },
                  "durationRounds": { "type": "integer" },
                  "type": { "type": "string" }
                }
              }
            }
          }
        },
        "levelManager": {
          "type": ["object", "null"],
          "properties": {
            "experiencePoints": { "type": "integer" },
            "classes": {
              "type": "array",
              "items": {
                "type": "object",
                "required": ["className", "level", "hitDie"],
                "properties": {
                  "className": { "type": "string" },
                  "level": { "type": "integer" },
                  "hitDie": { "type": "integer" }
                }
              }
            }
          }
        },
        "actionEconomy": {
          "type": ["object", "null"],
          "properties": {
            "hasAction": { "type": "boolean" },
            "hasBonusAction": { "type": "boolean" },
            "hasReaction": { "type": "boolean" }
          }
        },
        "inventory": {
          "type": ["object", "null"],
          "properties": {
            "items": {
              "type": "array",
              "items": { "$ref": "#/$defs/itemInstance" }
            },
            "copper": { "type": "integer" },
            "silver": { "type": "integer" },
            "gold": { "type": "integer" },
            "platinum": { "type": "integer" }
          }
        },
        "equipment": {
          "type": ["object", "null"],
          "properties": {
            "equippedSlots": {
              "type": "array",
              "items": {
                "type": "object",
                "required": ["slot", "itemIndex"],
                "properties": {
                  "slot": { "type": "string" },
                  "itemIndex": { "type": "integer" }
                }
              }
            },
            "attunedItemIndices": { "type": "array", "items": { "type": "integer" } }
          }
        },
        "spellcasting": {
          "type": ["object", "null"],
          "properties": {
            "castingAbility": { "type": "string" },
            "isPreparedCaster": { "type": "boolean" },
            "knownSpellNames": { "type": "array", "items": { "type": "string" } },
            "preparedSpellNames": { "type": "array", "items": { "type": "string" } },
            "slots": {
              "type": "array",
              "items": {
                "type": "object",
                "required": ["level", "max", "current"],
                "properties": {
                  "level": { "type": "integer" },
                  "max": { "type": "integer" },
                  "current": { "type": "integer" }
                }
              }
            },
            "pactSlotsMax": { "type": "integer" },
            "pactSlotsCurrent": { "type": "integer" },
            "pactSlotLevel": { "type": "integer" },
            "concentratingOnSpellName": { "type": ["string", "null"] }
          }
        }
      },
      "$defs": {
        "itemInstance": {
          "type": "object",
          "required": ["name"],
          "properties": {
            "name": { "type": "string" },
            "currentCharges": { "type": ["integer", "null"] },
            "contents": {
              "type": ["array", "null"],
              "items": { "$ref": "#/$defs/itemInstance" }
            }
          }
        }
      }
    }
    """;
}
