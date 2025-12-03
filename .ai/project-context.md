# OpenCombatEngine AI Assistant Context

## CRITICAL: Read This Entire Document First
You are assisting with OpenCombatEngine, an open-source combat engine for tabletop RPGs compatible with SRD 5.1 mechanics. This document contains MANDATORY coding standards and architectural patterns that must be followed WITHOUT EXCEPTION.

## Project Overview

### Core Purpose
- **What**: Interface-driven combat engine library for D&D 5e-compatible games
- **Architecture**: Heavy use of interfaces for maximum extensibility
- **Target**: .NET 8.0, C# 12, cross-platform
- **Testing**: Test-Driven Development (TDD) with minimum 80% coverage
- **License**: MIT for code, OGL 1.0a compliance for game mechanics

### Key Design Principles
1. **Interface-First**: Define contracts before implementations
2. **Open-Source Friendly**: Comprehensive documentation for contributors
3. **AI-Assisted Development**: Optimized for pair programming with AI
4. **Legally Compliant**: SRD-only, no proprietary D&D content
5. **Extensible**: Support for homebrew and variant rules

## MANDATORY Coding Conventions

### 1. XML Documentation (ENFORCED - NO EXCEPTIONS)

**EVERY** public, protected, and internal member MUST have XML documentation:

```csharp
/// <summary>
/// Calculates damage after applying all modifiers and resistances
/// </summary>
/// <param name="baseDamage">Initial damage amount before modifications</param>
/// <param name="damageType">Type of damage being dealt</param>
/// <param name="target">Creature receiving the damage</param>
/// <returns>Final damage amount after all calculations</returns>
/// <exception cref="ArgumentNullException">Thrown when target is null</exception>
/// <exception cref="ArgumentOutOfRangeException">Thrown when baseDamage is negative</exception>
/// <remarks>
/// This method accounts for resistance, immunity, and vulnerability.
/// Minimum damage is always 0, even with resistance.
/// </remarks>
public int CalculateDamage(int baseDamage, DamageType damageType, ICreature target)
```

#### Documentation Rules:
- **Summaries**: Start with action verb (Creates, Validates, Calculates, Gets, Sets)
- **Parameters**: Include purpose, valid ranges, special values (null, 0, etc.)
- **Returns**: Specify what value represents and special return conditions
- **Exceptions**: Document ALL exceptions that can be thrown
- **Remarks**: Add for complex logic or important implementation notes
- **Examples**: Include for non-obvious usage patterns

### 2. Enum Pattern (CRITICAL - ALWAYS REQUIRED)

Every enum MUST follow this pattern:

```csharp
/// <summary>
/// Defines creature size categories for game mechanics
/// </summary>
public enum CreatureSize
{
    /// <summary>Unknown or unspecified size</summary>
    Unspecified = 0,  // ALWAYS first, ALWAYS = 0
    
    /// <summary>Tiny creatures (2.5 ft space)</summary>
    Tiny,
    
    /// <summary>Small creatures (5 ft space)</summary>
    Small,
    
    /// <summary>Medium creatures (5 ft space)</summary>
    Medium,
    
    /// <summary>Large creatures (10 ft space)</summary>
    Large,
    
    /// <summary>Huge creatures (15 ft space)</summary>
    Huge,
    
    /// <summary>Gargantuan creatures (20+ ft space)</summary>
    Gargantuan,
    
    /// <summary>Sentinel value for validation</summary>
    LastValue  // ALWAYS last, used for bounds checking
}

// Validation pattern:
public static bool IsValid(CreatureSize size) 
    => size > CreatureSize.Unspecified && size < CreatureSize.LastValue;
```

### 3. Naming Conventions

```csharp
public class ExampleClass
{
    // Private fields: underscore prefix
    private readonly ILogger _logger;
    private int _internalCounter;
    
    // Constants: UPPER_CASE
    private const int MAX_RETRIES = 3;
    
    // Properties: PascalCase
    public string Name { get; set; }
    
    // Methods: PascalCase
    public void PerformAction() { }
    
    // Parameters: camelCase (no prefix)
    public void Method(string parameterName, int itemCount) { }
    
    // Local variables: camelCase, optional 'local' prefix for clarity
    public void Example()
    {
        var localValidator = new Validator();
        var itemCount = 5;
    }
    
    // Interfaces: ALWAYS 'I' prefix
    public interface IExampleInterface { }
}
```

### 4. Error Handling Pattern

```csharp
/// <summary>
/// Executes an action with comprehensive error handling
/// </summary>
public Result<ActionResult> ExecuteAction(IAction action, ICreature actor, IEnumerable<ICreature> targets)
{
    // Guard clauses first
    if (action == null) 
        return Result<ActionResult>.Failure("Action cannot be null");
    if (actor == null) 
        return Result<ActionResult>.Failure("Actor cannot be null");
    if (targets == null || !targets.Any())
        return Result<ActionResult>.Failure("At least one target is required");
    
    try
    {
        // Validate business rules
        if (!action.CanExecute(actor, targets))
        {
            _logger.LogInformation($"Action {action.Name} cannot be executed by {actor.Name}");
            return Result<ActionResult>.Failure($"Cannot execute {action.Name}");
        }
        
        // Core logic
        var result = PerformActionLogic(action, actor, targets);
        
        _logger.LogDebug($"Action {action.Name} executed successfully");
        return Result<ActionResult>.Success(result);
    }
    catch (GameRuleException ex)
    {
        // Expected game rule violations
        _logger.LogWarning($"Game rule violation: {ex.Message}");
        return Result<ActionResult>.Failure(ex.Message);
    }
    catch (Exception ex)
    {
        // Unexpected errors - log and re-throw
        _logger.LogError(ex, $"Unexpected error executing {action.Name}");
        throw;
    }
}
```

### 5. Test-Driven Development (TDD) Requirements

Write tests BEFORE implementation:

```csharp
public class CreatureTests
{
    /// <summary>
    /// Tests follow pattern: MethodName_Condition_ExpectedResult
    /// </summary>
    [Fact]
    public void TakeDamage_WithNormalDamage_ReducesHitPoints()
    {
        // Arrange - Set up test conditions
        var creature = new TestCreatureBuilder()
            .WithMaxHitPoints(20)
            .WithCurrentHitPoints(15)
            .Build();
        
        // Act - Perform the action
        var damageDealt = creature.TakeDamage(5, DamageType.Slashing);
        
        // Assert - Verify results
        Assert.Equal(5, damageDealt);
        Assert.Equal(10, creature.CurrentHitPoints);
    }
    
    [Theory]
    [InlineData(10, DamageType.Fire, 5)]      // Resistance
    [InlineData(10, DamageType.Poison, 0)]    // Immunity
    [InlineData(10, DamageType.Cold, 20)]     // Vulnerability
    public void TakeDamage_WithDamageModifiers_AppliesCorrectly(
        int baseDamage, DamageType type, int expectedDamage)
    {
        // Parameterized tests for comprehensive coverage
        var creature = new TestCreatureBuilder()
            .WithResistance(DamageType.Fire)
            .WithImmunity(DamageType.Poison)
            .WithVulnerability(DamageType.Cold)
            .WithMaxHitPoints(100)
            .Build();
        
        var actualDamage = creature.TakeDamage(baseDamage, type);
        
        Assert.Equal(expectedDamage, actualDamage);
    }
}
```

### 6. Interface Design Rules

```csharp
// 1. Keep interfaces focused (Interface Segregation Principle)
public interface ICreature : 
    IIdentifiable,
    INameable,
    IHasHitPoints,
    IHasAbilityScores,
    ICanTakeActions
{
    // Core creature properties only
}

// 2. Separate concerns into specific interfaces
public interface IHasHitPoints
{
    int CurrentHitPoints { get; }
    int MaxHitPoints { get; }
    int TemporaryHitPoints { get; set; }
}

// 3. Use generic interfaces for flexibility
public interface IModifiable<T>
{
    void AddModifier(IModifier<T> modifier);
    void RemoveModifier(IModifier<T> modifier);
    T GetModifiedValue();
}

// 4. Prefer composition over inheritance
public interface ISpellcaster : ICreature
{
    ISpellSlots SpellSlots { get; }
    ISpellList KnownSpells { get; }
    ISpellcastingAbility SpellcastingAbility { get; }
}
```

### 7. File Header (MANDATORY FOR ALL SOURCE FILES)

Every `.cs` source file MUST begin with this exact header:

```csharp
// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System;
// other using statements...

namespace OpenCombatEngine.Core;
```

This header is REQUIRED on:
- All interface files
- All implementation files
- All test files
- All example files
- Any C# source file in the project

NO EXCEPTIONS.

## Project Structure

```
OpenCombatEngine/
├── .ai/                                    # AI assistant context files
│   ├── project-context.md                 # This file
│   ├── current-tasks.md                   # Active development tasks
│   ├── architecture-decisions.md          # ADRs and design choices
│   └── code-examples.md                   # Canonical patterns
├── src/
│   ├── OpenCombatEngine.Core/             # Interfaces and enums ONLY
│   │   ├── Interfaces/
│   │   │   ├── Creatures/                 # ICreature and related
│   │   │   ├── Actions/                   # IAction and related
│   │   │   ├── Combat/                    # ICombatManager and related
│   │   │   └── Dice/                      # IDiceRoller and related
│   │   ├── Enums/
│   │   └── Results/                       # Result<T> pattern
│   ├── OpenCombatEngine.Implementation/   # Concrete implementations
│   │   ├── Creatures/
│   │   ├── Actions/
│   │   ├── Combat/
│   │   └── Dice/
│   └── OpenCombatEngine.Content/          # Content import system
│       ├── Importers/
│       │   ├── FiveEToolsImporter.cs      # 5e.tools format
│       │   ├── Open5eImporter.cs          # Open5e API format
│       │   ├── FoundryVttImporter.cs      # Foundry VTT format
│       │   ├── FightClub5Importer.cs      # FC5 XML format
│       │   └── NativeFormatImporter.cs    # OpenCombat Format (.ocf)
│       ├── Schemas/
│       │   └── opencombat-v1.schema.json  # Native format JSON schema
│       └── Mappings/                      # Format conversion mappings
├── tests/
│   ├── OpenCombatEngine.Core.Tests/       # Interface contract tests
│   ├── OpenCombatEngine.Implementation.Tests/
│   ├── OpenCombatEngine.Content.Tests/    # Importer tests
│   │   └── TestData/                      # Sample files for each format
│   └── OpenCombatEngine.Integration.Tests/
├── docs/
│   ├── architecture/                      # Architecture Decision Records
│   ├── api/                              # API documentation
│   ├── content-formats/                   # Import format documentation
│   │   ├── 5etools-mapping.md
│   │   ├── open5e-mapping.md
│   │   ├── foundry-mapping.md
│   │   └── native-format.md
│   └── legal/                            # OGL compliance docs
└── examples/
    ├── SimpleCombat/                     # Example usage
    └── ContentImport/                    # Import examples for each format
```

## Legal Compliance (CRITICAL)

### ALLOWED (SRD Content):
- Basic game mechanics (ability scores, saving throws, advantage/disadvantage)
- Generic creature statistics (AC, HP, ability scores)
- Spell mechanics (but NOT descriptions)
- Generic class features as mechanics
- Standard conditions (blinded, charmed, etc.)
- Action economy (action, bonus action, reaction)

### FORBIDDEN (Proprietary Content):
- Terms "Dungeons & Dragons", "D&D", "WotC"
- Specific character names (Drizzt, Elminster, etc.)
- Monster names not in SRD (Beholder, Mind Flayer, etc.)
- Spell descriptions (mechanical effects only)
- Setting-specific content (Forgotten Realms, etc.)
- Artwork or visual assets

### Content Import Strategy (Hybrid Approach):

The engine supports multiple community-standard formats while maintaining a clean internal representation:

```csharp
// Generic importer interface - we provide the pipes, users provide the water
public interface IContentImporter
{
    /// <summary>
    /// Checks if this importer can handle the given format
    /// </summary>
    bool CanImport(string format);
    
    /// <summary>
    /// Imports content from user-provided data stream
    /// </summary>
    /// <remarks>
    /// The engine does not validate legality of imported content.
    /// Users are responsible for compliance with applicable licenses.
    /// </remarks>
    Task<GameContent> ImportAsync(Stream contentStream);
}

// Factory pattern for format detection
public class ContentImporterFactory
{
    public IContentImporter GetImporter(string fileExtension)
    {
        return fileExtension.ToLower() switch
        {
            ".5et" or ".5etools" => new FiveEToolsImporter(),
            ".open5e" or ".json" when IsOpen5eFormat() => new Open5eImporter(), 
            ".fvtt" => new FoundryVttImporter(),
            ".fc5" or ".xml" => new FightClub5Importer(),
            ".ocf" => new NativeFormatImporter(), // Our native format
            _ => throw new NotSupportedException($"Format {fileExtension} not supported")
        };
    }
}
```

#### Supported Import Formats:
1. **5e.tools JSON** (.5et, .5etools) - De facto community standard
2. **Open5e API JSON** (.json with Open5e structure)
3. **Foundry VTT** (.fvtt) - Popular VTT format
4. **FightClub 5e XML** (.fc5, .xml) - Mobile app format
5. **OpenCombat Format** (.ocf) - Our native clean format

#### Native Format Structure:
```json
{
  "format": "opencombat/v1",
  "version": "1.0.0",
  "source": "user-content",
  "legal": "OGL-1.0a",
  "content": {
    "creatures": [],
    "spells": [],
    "items": [],
    "actions": []
  }
}
```

## Architecture Decisions

### 1. Interface-First Design
Every feature starts as an interface. This enables:
- Multiple implementations (basic vs advanced)
- Easy mocking for tests
- Community extensions
- Clear contracts

### 2. Result<T> Pattern for Error Handling
Public APIs return `Result<T>` instead of throwing exceptions:

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }
    
    public static Result<T> Success(T value) => new(value, null, true);
    public static Result<T> Failure(string error) => new(default, error, false);
}
```

### 3. Immutable DTOs
Data Transfer Objects are immutable:

```csharp
public record DamageRoll(
    int Amount,
    DamageType Type,
    bool IsCritical = false,
    string Source = ""
);
```

### 4. Event-Driven Architecture
Combat events use standard C# events:

```csharp
public class CombatManager : ICombatManager
{
    public event EventHandler<TurnStartedEventArgs> TurnStarted;
    public event EventHandler<DamageDealtEventArgs> DamageDealt;
    public event EventHandler<CombatEndedEventArgs> CombatEnded;
}
```

## Common Implementation Patterns

### Creating a New Feature

1. **Define the Interface** (Core project):
```csharp
/// <summary>
/// Manages concentration for spellcasting
/// </summary>
public interface IConcentrationManager
{
    /// <summary>
    /// Checks if concentration is maintained after taking damage
    /// </summary>
    bool MakeConcentrationSave(int damageTaken);
}
```

2. **Write Tests** (Test project):
```csharp
[Theory]
[InlineData(5, true)]   // Low damage, likely success
[InlineData(25, false)] // High damage, likely failure
public void MakeConcentrationSave_WithVaryingDamage_ReturnsExpectedResult(
    int damage, bool expectedSuccess)
{
    // Test implementation
}
```

3. **Implement** (Implementation project):
```csharp
public class ConcentrationManager : IConcentrationManager
{
    private readonly IDiceRoller _diceRoller;
    
    public bool MakeConcentrationSave(int damageTaken)
    {
        var dc = Math.Max(10, damageTaken / 2);
        var roll = _diceRoller.Roll("1d20");
        return roll >= dc;
    }
}
```

### Adding Content Support

```csharp
// Internal clean content structure (no flavor text)
public record SpellData(
    string Id,
    string Name,
    int Level,
    SpellSchool School,
    CastingTime CastingTime,
    Range Range,
    Components Components,
    Duration Duration,
    // Note: No description field - mechanics only
    List<IEffect> Effects,
    string SourceFormat // Track where it came from
);

// Example: 5e.tools format importer
public class FiveEToolsImporter : IContentImporter
{
    public bool CanImport(string format) => 
        format.EndsWith(".5et") || format.EndsWith(".5etools");
    
    public async Task<GameContent> ImportAsync(Stream contentStream)
    {
        // Parse 5e.tools specific format
        var json = await new StreamReader(contentStream).ReadToEndAsync();
        var data = JsonSerializer.Deserialize<FiveEToolsFormat>(json);
        
        // Map to our clean internal format
        var spells = data.Spells.Select(s => MapToInternalFormat(s));
        
        return new GameContent
        {
            Spells = spells,
            Source = "5e.tools-import",
            LegalNotice = "User-imported content - verify licensing"
        };
    }
    
    private SpellData MapToInternalFormat(FiveEToolsSpell spell)
    {
        return new SpellData(
            Id: GenerateId(spell.Name),
            Name: spell.Name,
            Level: spell.Level,
            School: MapSchool(spell.School),
            CastingTime: MapCastingTime(spell.Time),
            Range: MapRange(spell.Range),
            Components: MapComponents(spell.Components),
            Duration: MapDuration(spell.Duration),
            Effects: ExtractMechanicalEffects(spell.Entries),
            SourceFormat: "5e.tools"
        );
    }
}

// Native format for clean data exchange
public class NativeFormatImporter : IContentImporter
{
    public bool CanImport(string format) => format.EndsWith(".ocf");
    
    public async Task<GameContent> ImportAsync(Stream contentStream)
    {
        // Our format is already clean - just deserialize
        var json = await new StreamReader(contentStream).ReadToEndAsync();
        return JsonSerializer.Deserialize<GameContent>(json);
    }
}
```

## Git Workflow

### Branch Naming
- `feature/add-{feature-name}` - New features
- `fix/repair-{issue}` - Bug fixes
- `docs/update-{section}` - Documentation
- `test/add-{test-area}` - Test additions

### Commit Messages
```
type(scope): description

- feat(combat): add initiative tracking
- fix(dice): correct advantage roll logic
- docs(api): update ICreature documentation
- test(creature): add damage resistance tests
```

### Pull Request Requirements
- [ ] All tests pass
- [ ] Coverage ≥ 80%
- [ ] XML documentation complete
- [ ] No compiler warnings
- [ ] Follows all conventions in this document

## Build and Test Commands

```bash
# Build
dotnet build                          # Build solution
dotnet build --configuration Release  # Release build

# Test
dotnet test                           # Run all tests
dotnet test --collect:"XPlat Code Coverage"  # With coverage
dotnet test --filter "FullyQualifiedName~CreatureTests"  # Specific tests

# Package
dotnet pack --configuration Release   # Create NuGet package

# Run examples
dotnet run --project examples/SimpleCombat
```

## When Providing Code

### ALWAYS:
1. Include the copyright header at the top of EVERY source file
2. Include XML documentation on EVERY member (no exceptions)
3. Use the Unspecified/LastValue enum pattern
4. Write comprehensive tests FIRST (TDD)
5. Use Result<T> for public APIs (no exceptions thrown)
6. Follow exact naming conventions (underscore for private fields)
7. Validate all inputs with guard clauses
8. Log appropriate information
9. Keep interfaces in Core project only

### NEVER:
1. Skip XML documentation (even for obvious members)
2. Use proprietary D&D terms
3. Include descriptive/flavor text for game content
4. Put implementation in Core project
5. Throw exceptions from public methods (use Result<T>)
6. Use 'var' for anything except obvious types
7. Create enums without Unspecified/LastValue

## Current Development Status

### Completed
- [x] Project structure
- [x] AI context documentation
- [x] Repository setup

### In Progress
- [ ] Core interfaces definition
- [ ] IDiceRoller interface
- [ ] ICreature interface
- [ ] IAction interface

### Next Up
- [ ] Basic combat manager
- [ ] Initiative system
- [ ] Advantage/disadvantage
- [ ] Basic conditions
- [ ] Content import system (5e.tools, Open5e, Foundry formats)

## Common Tasks for AI Assistants

### Task: "Implement the IDiceRoller interface"
1. Check existing interface in `Core/Interfaces/Dice/IDiceRoller.cs`
2. Review tests in `Tests/Dice/DiceRollerTests.cs`
3. Create implementation in `Implementation/Dice/StandardDiceRoller.cs`
4. Ensure all tests pass
5. Add integration tests if needed

### Task: "Add a new creature type"
1. Define interface in `Core/Interfaces/Creatures/INewType.cs`
2. Add to creature type enum with Unspecified/LastValue pattern
3. Write comprehensive tests
4. Implement in `Implementation/Creatures/NewType.cs`
5. Update content templates

### Task: "Fix a bug in [component]"
1. Write a failing test that reproduces the bug
2. Fix the implementation
3. Ensure all tests pass
4. Update documentation if behavior changed

### Task: "Add support for a new import format"
1. Create importer in `Content/Importers/NewFormatImporter.cs`
2. Implement `IContentImporter` interface
3. Add format detection to `ContentImporterFactory`
4. Create mapping from external format to internal `SpellData`, `CreatureData`, etc.
5. Write tests with sample data files
6. Document the mapping in `docs/content-formats/new-format-mapping.md`

### Task: "Import content from [5e.tools/Open5e/etc]"
1. Check if importer exists for that format
2. Use `ContentImporterFactory.GetImporter()`
3. Strip any descriptive/flavor text during import
4. Map to internal mechanical representation only
5. Mark source format for tracking

## Questions or Clarifications?

Before implementing:
1. Check this document for patterns
2. Review existing code for examples
3. Look at tests for expected behavior
4. Verify legal compliance for game content

Remember: When in doubt, favor explicitness and comprehensive documentation over brevity.

## Final Notes

This project prioritizes:
1. **Correctness** over performance
2. **Clarity** over cleverness  
3. **Extensibility** over simplicity
4. **Documentation** over assumptions

Every line of code should be written as if the person maintaining it is a violent psychopath who knows where you live. Document accordingly.
