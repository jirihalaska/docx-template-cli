# ADR-001: Clean Architecture Implementation

## Status
Accepted

## Context
The DocxTemplate CLI needs a maintainable, testable architecture that supports:
- Clear separation between business logic and infrastructure
- Independent testing of each layer
- Future extensibility (GUI, web API, etc.)
- Dependency management and inversion of control

The team needs to choose an architectural pattern that will scale with the project's growth and maintain code quality over time.

## Decision
Implement Clean Architecture with three distinct layers:

1. **DocxTemplate.CLI** - Presentation/Interface layer
2. **DocxTemplate.Core** - Business logic layer  
3. **DocxTemplate.Infrastructure** - Data/External services layer

Dependencies flow inward: CLI → Core ← Infrastructure

## Alternatives Considered

### Traditional N-Tier Architecture
- **Pros**: Simple, familiar to most developers
- **Cons**: Tight coupling, difficult to test, database-centric

### Hexagonal Architecture (Ports & Adapters)
- **Pros**: Good separation, testable
- **Cons**: More complex than needed for this project size

### Simple Layered Architecture
- **Pros**: Easier to implement initially
- **Cons**: Tends to become tightly coupled over time

## Consequences

### Positive
- **Testability**: Each layer can be tested independently
- **Maintainability**: Clear responsibilities and boundaries
- **Flexibility**: Easy to swap implementations (e.g., different document processors)
- **Future-proofing**: Can easily add GUI or web API layers
- **Domain-centric**: Business logic is not tied to external concerns

### Negative
- **Initial complexity**: More projects and interfaces than simple approach
- **Learning curve**: Team needs to understand dependency inversion
- **Over-engineering risk**: May be complex for simple operations

### Neutral
- **More files**: Each interface requires implementation
- **Dependency injection**: Need to configure DI container

## Implementation Notes

### Project Dependencies
```
CLI → Core
Infrastructure → Core
CLI → Infrastructure (for DI configuration only)
```

### Key Principles
1. **Dependency Inversion**: Core defines interfaces, Infrastructure implements them
2. **Single Responsibility**: Each service has one clear purpose
3. **Interface Segregation**: Keep interfaces focused and minimal
4. **Testability**: All dependencies injectable for testing

### Service Interface Examples
- `ITemplateDiscoveryService` - Finding template files
- `IPlaceholderScanService` - Scanning for placeholders
- `ITemplateCopyService` - Copying template files
- `IPlaceholderReplaceService` - Replacing placeholders with values

## Related Decisions
- ADR-002: Dependency Injection Container Choice (future)
- ADR-003: Document Processing Library Selection (future)