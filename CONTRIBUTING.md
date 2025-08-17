# Contributing to DocxTemplate CLI

Thank you for your interest in contributing to DocxTemplate CLI! This document provides guidelines and information for contributors.

## Code of Conduct

We are committed to creating a welcoming and inclusive environment. Please be respectful in all interactions.

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Git
- Your favorite code editor (VS Code, Visual Studio, Rider, etc.)

### Setting Up Development Environment

1. **Fork and Clone**
   ```bash
   git clone https://github.com/your-username/docx-template-cli.git
   cd docx-template-cli
   ```

2. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the Project**
   ```bash
   dotnet build
   ```

4. **Run Tests**
   ```bash
   dotnet test
   ```

## Development Workflow

### Branch Naming

Use descriptive branch names following this pattern:
- `feature/add-new-command` - for new features
- `bugfix/fix-placeholder-parsing` - for bug fixes
- `docs/update-contributing-guide` - for documentation updates
- `refactor/improve-service-interfaces` - for refactoring

### Commit Messages

Follow conventional commit format:
```
type(scope): description

[optional body]

[optional footer]
```

Examples:
- `feat(cli): add new discover command`
- `fix(core): resolve placeholder parsing edge case`
- `docs(readme): update installation instructions`
- `test(core): add unit tests for template service`

### Code Style and Quality

#### Automated Enforcement

The project uses several tools to ensure code quality:
- **StyleCop**: Enforces C# coding conventions
- **EditorConfig**: Maintains consistent formatting
- **Analyzers**: Catches potential issues and enforces best practices

#### Manual Guidelines

1. **Naming Conventions**
   - Use PascalCase for classes, methods, properties
   - Use camelCase for local variables and parameters
   - Use meaningful, descriptive names

2. **Code Organization**
   - Follow the three-layer architecture (CLI → Core → Infrastructure)
   - Keep classes focused on single responsibility
   - Use interfaces for all service contracts

3. **Documentation**
   - Add XML documentation for public APIs
   - Include inline comments for complex logic
   - Update README when adding new features

### Testing Guidelines

#### Test Structure

Follow the AAA pattern in tests:

```csharp
[Fact]
public void ScanAsync_WhenValidTemplate_ShouldReturnPlaceholders()
{
    // arrange
    var service = new PlaceholderScanService();
    var templatePath = "path/to/template.docx";
    
    // act
    var result = await service.ScanAsync(templatePath);
    
    // assert
    result.Should().NotBeNull();
    result.Placeholders.Should().HaveCountGreaterThan(0);
}
```

#### Test Coverage

- Write unit tests for all public methods
- Include integration tests for critical workflows
- Test edge cases and error conditions
- Aim for >80% code coverage

#### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/DocxTemplate.Core.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~PlaceholderScanServiceTests.ScanAsync_WhenValidTemplate_ShouldReturnPlaceholders"
```

## Pull Request Process

### Before Submitting

1. **Ensure all tests pass**
   ```bash
   dotnet test
   ```

2. **Build without warnings**
   ```bash
   dotnet build
   ```

3. **Check code style** (warnings will appear during build)

4. **Update documentation** if needed

### PR Guidelines

1. **Descriptive Title**: Use conventional commit format
2. **Clear Description**: Explain what changes were made and why
3. **Link Issues**: Reference any related issues
4. **Small, Focused Changes**: Keep PRs manageable in size
5. **Update Tests**: Include or update tests for your changes

### PR Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update

## Testing
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Manual testing completed

## Checklist
- [ ] My code follows the project's style guidelines
- [ ] I have performed a self-review of my own code
- [ ] I have commented my code, particularly in hard-to-understand areas
- [ ] I have made corresponding changes to the documentation
- [ ] My changes generate no new warnings
- [ ] I have added tests that prove my fix is effective or that my feature works
```

## Architecture Guidelines

### Layer Responsibilities

1. **CLI Layer** (`DocxTemplate.CLI`)
   - Command parsing and validation
   - User interaction and feedback
   - Input/output formatting
   - No business logic

2. **Core Layer** (`DocxTemplate.Core`)
   - Business logic and rules
   - Service interfaces
   - Domain models
   - No infrastructure dependencies

3. **Infrastructure Layer** (`DocxTemplate.Infrastructure`)
   - File system operations
   - Word document processing
   - External service integrations
   - Implementation of core interfaces

### Adding New Features

When adding new functionality:

1. **Define Interface** in Core layer
2. **Implement Service** in Infrastructure layer
3. **Add CLI Command** in CLI layer
4. **Write Tests** for all layers
5. **Update Documentation**

## Performance Considerations

- Use async/await for I/O operations
- Consider memory usage with large documents
- Profile performance-critical paths
- Add benchmarks for new algorithms

## Documentation

### Types of Documentation

1. **Code Documentation**: XML comments for public APIs
2. **User Documentation**: README, CLI help text
3. **Architecture Documentation**: Design decisions, ADRs
4. **Contributor Documentation**: This file

### Architecture Decision Records (ADRs)

For significant architectural decisions, create an ADR in `docs/adr/`:

```markdown
# ADR-001: Use Clean Architecture

## Status
Accepted

## Context
Need to organize code for maintainability and testability.

## Decision
Implement three-layer clean architecture.

## Consequences
- Clear separation of concerns
- Better testability
- More complex project structure
```

## Release Process

1. Update version numbers
2. Update CHANGELOG.md
3. Create release branch
4. Tag release
5. Build and publish packages
6. Update GitHub release

## Getting Help

- **Issues**: Use GitHub issues for bugs and feature requests
- **Discussions**: Use GitHub discussions for questions
- **Documentation**: Check the `docs/` directory

## Recognition

Contributors will be recognized in:
- CONTRIBUTORS.md file
- Release notes
- Annual contributor highlights

Thank you for contributing to DocxTemplate CLI! 🎉