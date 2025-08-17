# Architecture Decision Records (ADRs)

This directory contains Architecture Decision Records for the DocxTemplate CLI project.

## What are ADRs?

Architecture Decision Records (ADRs) are short text documents that capture important architectural decisions made during the project, along with their context and consequences.

## When to Write an ADR

Create an ADR when you make a significant architectural decision that:
- Affects the overall structure of the system
- Has long-term implications
- Is difficult to reverse
- Involves trade-offs between alternatives
- Impacts multiple components or layers

## ADR Template

Use this template for new ADRs:

```markdown
# ADR-XXX: [Decision Title]

## Status
[Proposed | Accepted | Deprecated | Superseded by ADR-XXX]

## Context
[Describe the problem/situation that requires a decision]

## Decision
[State the decision that was made]

## Alternatives Considered
[List other options that were considered]

## Consequences
### Positive
- [List benefits of this decision]

### Negative
- [List drawbacks or costs of this decision]

### Neutral
- [List neutral consequences or trade-offs]

## Implementation Notes
[Any specific implementation guidance]

## Related Decisions
[Links to related ADRs]
```

## Existing ADRs

| ADR | Title | Status |
|-----|-------|--------|
| [001](./001-clean-architecture.md) | Clean Architecture Implementation | Accepted |

## Naming Convention

- Use format: `XXX-kebab-case-title.md`
- Number sequentially starting from 001
- Use descriptive, concise titles