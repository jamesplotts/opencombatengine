# Contributing to OpenCombatEngine

First off, thank you for considering contributing to OpenCombatEngine! It's people like you that make this project possible.

## 🤖 AI-Assisted Development

This project is optimized for AI pair programming! If you're using Claude, GPT-4, Cursor, Antigravity, or any other AI assistant:

**→ See [AI Development Guide](docs/contributing/AI-DEVELOPMENT.md) for comprehensive instructions**

## Code of Conduct

This project and everyone participating in it is governed by our Code of Conduct. By participating, you are expected to uphold this code.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check existing issues as you might find out that you don't need to create one. When you are creating a bug report, please include as many details as possible:

* **Use a clear and descriptive title**
* **Describe the exact steps to reproduce the problem**
* **Provide specific examples to demonstrate the steps**
* **Include test cases that demonstrate the bug**
* **Explain which behavior you expected to see instead and why**

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, please include:

* **Use a clear and descriptive title**
* **Provide a step-by-step description of the suggested enhancement**
* **Provide specific examples to demonstrate the steps**
* **Describe the current behavior and explain expected behavior**
* **Include mockup interfaces if applicable**

### Your First Code Contribution

Unsure where to begin? You can start by looking through `beginner` and `help-wanted` issues:

* **Beginner issues** - issues which should only require a few lines of code
* **Help wanted issues** - issues which should be a bit more involved

## Development Process

### Prerequisites

* .NET 8.0 SDK or higher
* A C# IDE (Visual Studio, VS Code, Rider, etc.)
* Git

### Setting Up Your Development Environment

1. Fork the repository
2. Clone your fork:
   ```bash
   git clone https://github.com/yourusername/OpenCombatEngine.git
   cd OpenCombatEngine
   ```
3. Add the upstream remote:
   ```bash
   git remote add upstream https://github.com/originalowner/OpenCombatEngine.git
   ```
4. Build the project:
   ```bash
   dotnet build
   ```
5. Run tests:
   ```bash
   dotnet test
   ```

### Development Workflow

1. **Create a feature branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Write tests FIRST (TDD)**:
   - Create tests in the appropriate test project
   - Follow naming convention: `MethodName_Condition_ExpectedResult`
   - Include edge cases and error conditions

3. **Implement your feature**:
   - Follow ALL conventions in `.ai/project-context.md`
   - Ensure XML documentation on ALL public members
   - Use the Result<T> pattern for public APIs

4. **Run tests and check coverage**:
   ```bash
   dotnet test --collect:"XPlat Code Coverage"
   ```
   - Minimum 80% coverage required

5. **Commit your changes**:
   ```bash
   git commit -m "feat(scope): description of change"
   ```

6. **Push to your fork**:
   ```bash
   git push origin feature/your-feature-name
   ```

7. **Create a Pull Request**

## Coding Standards (CRITICAL)

### MANDATORY Requirements

All code MUST follow these standards WITHOUT EXCEPTION:

#### 1. XML Documentation
```csharp
/// <summary>
/// EVERY public member must have XML documentation
/// </summary>
/// <param name="parameter">Describe each parameter</param>
/// <returns>Describe the return value</returns>
public Result<int> ExampleMethod(string parameter)
```

#### 2. Enum Pattern
```csharp
public enum ExampleEnum
{
    Unspecified = 0,  // ALWAYS first
    Value1,
    Value2,
    LastValue         // ALWAYS last
}
```

#### 3. Naming Conventions
- Private fields: `_camelCase` with underscore
- Properties: `PascalCase`
- Parameters: `camelCase` (no prefix)
- Interfaces: `I` prefix ALWAYS

#### 4. Project Structure
- **Core**: Interfaces and enums ONLY
- **Implementation**: Concrete implementations
- **Tests**: Comprehensive test coverage

For complete standards, see [`.ai/project-context.md`](.ai/project-context.md)

## Pull Request Process

### Before Submitting

- [ ] All tests pass
- [ ] Code coverage ≥ 80%
- [ ] XML documentation complete
- [ ] No compiler warnings
- [ ] Follows ALL coding conventions
- [ ] Commit messages follow convention

### PR Requirements

1. **Title**: Use conventional commit format
   - `feat:` New feature
   - `fix:` Bug fix
   - `docs:` Documentation only
   - `test:` Adding tests
   - `refactor:` Code change that neither fixes a bug nor adds a feature

2. **Description**: Include:
   - What changes were made
   - Why the changes were necessary
   - Any breaking changes
   - Related issue numbers

3. **Tests**: Every PR must include tests

4. **Documentation**: Update relevant documentation

### Review Process

1. Automated checks must pass
2. At least one maintainer review required
3. All review comments must be addressed
4. Squash merge into main branch

## Legal Considerations

### Content Guidelines

- **ONLY use SRD 5.1 content** - no proprietary D&D terms
- **No copyrighted descriptions** - mechanics only
- **Generic naming** - avoid trademarked names
- **User content** - we provide import tools, users provide content

### License

By contributing, you agree that your contributions will be licensed under:
- **MIT License** for code
- **OGL 1.0a** for game mechanics

## Testing Guidelines

### Test Structure
```csharp
public class ExampleTests
{
    [Fact]
    public void Method_HappyPath_ReturnsExpected()
    {
        // Arrange
        var sut = CreateSystemUnderTest();
        
        // Act
        var result = sut.Method();
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData(input1, expected1)]
    [InlineData(input2, expected2)]
    public void Method_VariousInputs_ReturnsCorrectResults(int input, int expected)
    {
        // Parameterized tests for comprehensive coverage
    }
}
```

### Required Test Coverage
- Happy path scenarios
- Edge cases
- Error conditions
- Boundary testing
- Integration tests where appropriate

## Community

### Getting Help

- **Documentation**: Check `/docs` folder
- **Issues**: Search existing issues first
- **Discussions**: Use GitHub Discussions for questions
- **AI Context**: Read `.ai/project-context.md` for patterns

### Communication Channels

- **GitHub Issues**: Bug reports and feature requests
- **GitHub Discussions**: General questions and discussions
- **Pull Requests**: Code contributions

## Recognition

Contributors will be recognized in:
- The project README
- Release notes
- Contributors page

## Questions?

If you have questions not covered here:
1. Check the [AI Development Guide](docs/contributing/AI-DEVELOPMENT.md)
2. Read [`.ai/project-context.md`](.ai/project-context.md)
3. Open a GitHub Discussion

Thank you for contributing to OpenCombatEngine! 🎲
