# Architecture Decision Records (ADRs)

## What is an ADR?

An **Architecture Decision Record** (ADR) captures an important architectural decision made during the project, along with its context and consequences. ADRs help future contributors understand **why** decisions were made, not just **what** was decided.

## When to Create an ADR

Create an ADR when making decisions about:

- Technology choices (frameworks, libraries, deployment models)
- Architectural patterns (service boundaries, data flow, error handling)
- Trade-offs with significant impact (performance vs. simplicity, features vs. maintenance)
- API or integration strategies
- Security or compliance approaches

**Do NOT create ADRs for:**
- Routine coding choices (naming conventions are in STORY_MAP.md)
- Temporary spike decisions (those go in spike-results.md)
- Story-specific implementation details (those go in story plans)

## ADR Format

Each ADR follows this template:

```markdown
# ADR-XXX: [Title]

**Status**: [Proposed | Accepted | Deprecated | Superseded by ADR-YYY]
**Date**: YYYY-MM-DD
**Deciders**: [Who made this decision]

## Context
What is the issue we're facing? What constraints exist?

## Decision
What did we decide to do?

## Consequences
What are the positive and negative outcomes?

### Positive
- Benefit 1
- Benefit 2

### Negative
- Trade-off 1
- Risk 1

## References
- Link to spike results, documentation, or research
```

## ADR Lifecycle

1. **Proposed**: Decision is being considered
2. **Accepted**: Decision is adopted and should be followed
3. **Deprecated**: Decision is outdated but still in use (plan to migrate away)
4. **Superseded**: Decision has been replaced by a newer ADR

## How to Use ADRs

### Creating a New ADR

1. Check `INDEX.md` for the next available ADR number
2. Create `ADR-XXX-short-title.md` in `plan/adr/`
3. Fill out the template (see above)
4. Add an entry to `INDEX.md`
5. Commit with message: `ADR-XXX: [Title]`

### Updating an Existing ADR

- Minor clarifications: Just edit the ADR
- Major changes: Create a new ADR and mark the old one as **Superseded**

### Finding Decisions

- **By topic**: Read `INDEX.md` (decisions are categorized)
- **By date**: ADRs are numbered chronologically
- **By status**: Search for "Status: Accepted" to find active decisions

## Directory Structure

```
plan/adr/
├── README.md          # This file - explains the ADR system
├── INDEX.md           # Categorized list of all ADRs
├── ADR-001-*.md       # First decision
├── ADR-002-*.md       # Second decision
└── ...
```

## Why We Use ADRs

1. **Onboarding**: New contributors can quickly understand the project's architectural foundation
2. **Decision tracking**: Prevents re-litigating settled decisions
3. **Context preservation**: The "why" behind decisions is often lost without documentation
4. **Trade-off visibility**: Makes explicit the pros/cons of each choice
5. **Accountability**: Records who decided what and when

## Additional Resources

- [ADR GitHub Organization](https://adr.github.io/) - ADR best practices and templates
- Michael Nygard's blog post: ["Documenting Architecture Decisions"](http://thinkrelevance.com/blog/2011/11/15/documenting-architecture-decisions)

---

**Remember**: ADRs are lightweight. They should be concise (1-2 pages max) and focus on the decision's essence, not exhaustive detail. Link to spike results or external docs for deep dives.
