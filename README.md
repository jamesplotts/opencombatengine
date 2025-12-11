# OpenCombatEngine

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4.svg)](https://dotnet.microsoft.com/download)
[![C#](https://img.shields.io/badge/C%23-12.0-239120.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![SRD](https://img.shields.io/badge/SRD-5.1-red.svg)](https://dnd.wizards.com/resources/systems-reference-document)

An open-source, interface-driven combat engine for RPGs compatible with D&D 5e SRD mechanics. Built with extensibility, testability, and AI-assisted development in mind.

## 🎯 Project Goals

- **Interface-First Architecture**: Maximum extensibility through comprehensive interfaces
- **SRD 5.1 Compliant**: Fully compatible with D&D 5e mechanics while respecting IP
- **Test-Driven Development**: Minimum 80% code coverage with TDD methodology
- **AI-Friendly**: Optimized for pair programming with AI assistants

> [!TIP]
> Check out the [Project Roadmap](ROADMAP.md) to see our progress and architectural decisions.

## Features

- **Core D20 Mechanics**:
  - Ability Scores (STR, DEX, CON, INT, WIS, CHA)
  - Modifiers calculated automatically (`(Score - 10) / 2`)
  - Dice Rolling (Standard notation like `1d20+5`, Advantage/Disadvantage)
- **Creature Management**:
  - Composition-based architecture (`ICreature`, `IAbilityScores`, `IHitPoints`)
  - **Serialization**: Memento pattern support for saving/loading creature state (JSON compatible)
- **Action System**:
  - Command-based Actions (`IAction`, `AttackAction`)
  - Combat Stats (AC, Initiative, Speed)
  - Damage and Healing logic
- **Turn Management**:
  - Cyclic Initiative system
  - Tie-breaking using Dexterity score
  - Cyclic Initiative system
  - Tie-breaking using Dexterity score
  - Round tracking
- **Health & Survival**:
  - Death Saving Throws (Success/Failure tracking, Stabilization)
  - Damage Types & Resistances (Resistance, Vulnerability, Immunity logic)
  - Ability Checks & Saving Throws
- **Spellcasting System**:
  - Spell Slots & Preparation (Wizard/Sorcerer style support)
  - Spell Resolution (Attack Rolls, Saving Throws, Damage)
  - Spell Resolution (Attack Rolls, Saving Throws, Damage)
  - Content Import (JSON support for Spells)
  - **Open5e Integration**: Direct API access to SRD Spells and Monsters
- **Magic Items**:
  - Attunement System (Max 3 items)
  - Passive Bonuses (Features/Conditions applied automatically)
  - Item Types (Weapons, Armor, Rings, Wondrous Items)
- **Extensible Design**:
  - Interface-driven architecture
  - Dependency Injection friendly
  - Result pattern for robust error handling without exceptions
- ✅ **Reproducible Testing**: Seed-based dice rolling for deterministic tests

### Planned Features
> [!NOTE]
> See [PLANNED.md](PLANNED.md) for our future feature roadmap, including Content Systems and Advanced Magic.

## 🚀 Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or higher
- Any IDE that supports C# (Visual Studio, VS Code, Rider, etc.)

### Installation

```bash
# Clone the repository
git clone https://github.com/yourusername/OpenCombatEngine.git
cd OpenCombatEngine

# Build the solution
dotnet build

# Run tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Quick Example

```csharp
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Implementation.Dice;

// Create a dice roller
IDiceRoller roller = new StandardDiceRoller();

// Roll some dice
var result = roller.Roll("3d6+2");
if (result.IsSuccess)
{
    Console.WriteLine($"Rolled: {result.Value.Total}");
    Console.WriteLine($"Individual rolls: [{string.Join(", ", result.Value.IndividualRolls)}]");
}

// Roll with advantage
var advantage = roller.RollWithAdvantage("1d20+5");
Console.WriteLine($"Advantage result: {advantage.Value}");
```

### 🎮 Runs the Demo

We include a CLI demo project that showcases the Event System and Reaction System in action.

```bash
dotnet run --project src/OpenCombatEngine.Demo/OpenCombatEngine.Demo.csproj
```

## 📁 Project Structure

```
OpenCombatEngine/
├── src/
│   ├── OpenCombatEngine.Core/          # Interfaces and contracts only
│   ├── OpenCombatEngine.Implementation/ # Concrete implementations
│   └── OpenCombatEngine.Content/        # Content import system
├── tests/
│   └── OpenCombatEngine.Core.Tests/    # Comprehensive unit tests
├── docs/
│   ├── architecture/                   # Architecture decisions
│   └── implementation/                 # Implementation details
├── examples/
│   └── DiceRollerDemo.cs              # Usage examples
└── .ai/
    └── project-context.md             # AI assistant context
```

## 🏗️ Architecture

### Core Principles

1. **Interface-First Design**: Every feature starts as an interface
2. **Separation of Concerns**: Core interfaces, implementations, and content are separate
3. **Immutable Data**: DTOs and value objects are immutable
4. **No Exceptions in Public APIs**: Result<T> pattern for error handling
5. **Comprehensive Documentation**: XML docs on every public member

### Key Patterns

- **Result<T> Pattern**: Safe error handling without exceptions
- **Enum Sentinels**: Every enum has Unspecified=0 and LastValue for validation
- **Test-Driven Development**: Tests written before implementation
- **Builder Pattern**: For complex object construction in tests

## 📋 Coding Standards

This project follows **strict** coding standards. See [.ai/project-context.md](.ai/project-context.md) for complete details.

### Highlights:
- **Mandatory** XML documentation on ALL public members
- Specific enum patterns with validation sentinels
- Underscore prefix for private fields
- Result<T> pattern for all public APIs
- Minimum 80% test coverage

## 🤝 Contributing

We welcome contributions! Please ensure:

1. All code follows the standards in [.ai/project-context.md](.ai/project-context.md)
2. Tests are written BEFORE implementation (TDD)
3. All tests pass
4. Code coverage remains ≥80%
5. No compiler warnings

### Getting Started with Contributing

1. Fork the repository
2. Create a feature branch (`feature/add-amazing-feature`)
3. Write tests for your feature
4. Implement the feature
5. Ensure all tests pass
6. Submit a Pull Request

### AI-Assisted Development

This project is optimized for AI pair programming. If using an AI assistant:

**→ See [AI Development Guide](docs/contributing/AI-DEVELOPMENT.md) to get started**

We support Antigravity, Cursor, Claude, GPT-4, and GitHub Copilot with comprehensive context files.

## ⚖️ Legal Compliance

- **Code License**: MIT License
- **Game Mechanics**: OGL 1.0a compliant
- **Content**: SRD 5.1 only - no proprietary D&D content
- **Trademarks**: No use of Wizards of the Coast trademarks

### Important Notes:
- This project uses game mechanics from the SRD 5.1
- No proprietary monster names (Beholder, Mind Flayer, etc.)
- No copyrighted spell descriptions (mechanics only)
- No setting-specific content (Forgotten Realms, etc.)

For important legal disclaimers, see [LEGAL.md](LEGAL.md).

## 📚 Documentation

- [API Documentation](docs/api/) - Coming soon
- [Architecture Decisions](docs/architecture/)
- [Implementation Details](docs/implementation/)
- [Content Import Formats](docs/content-formats/) - Coming soon
- [AI Prompt History](docs/ai-prompt-history/) - Complete development conversations with AI assistants
  - Demonstrates AI-assisted development methodology
  - Shows design decision evolution
  - Useful for contributors using AI tools

## 🗺️ Roadmap

See [ROADMAP.md](ROADMAP.md) for the detailed project roadmap and task history.

## 🔧 Development

### Build Commands

```bash
# Build
dotnet build
dotnet build --configuration Release

# Test
dotnet test
dotnet test --collect:"XPlat Code Coverage"
dotnet test --filter "FullyQualifiedName~DiceRollerTests"

# Package
dotnet pack --configuration Release
```
## Code of Conduct

This project adheres to a [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## 💬 Community

- [Issues](https://github.com/yourusername/OpenCombatEngine/issues) - Bug reports and feature requests
- [Discussions](https://github.com/yourusername/OpenCombatEngine/discussions) - General discussions
- [Wiki](https://github.com/yourusername/OpenCombatEngine/wiki) - Community documentation

## 👥 Contributors

- James Duane Plotts - Project Lead & Architecture

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

Game mechanics are used under the Open Gaming License v1.0a - see [docs/legal/OGL.txt](docs/legal/OGL.txt) for details.

## 🙏 Acknowledgments

- Wizards of the Coast for the SRD 5.1
- The open-source rpg gaming community
- Contributors to 5e.tools and Open5e for format inspiration
- The .NET and C# communities

---

**Built with ❤️ for the rpg gaming community**
