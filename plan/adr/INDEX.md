# Architecture Decision Records - Index

## About This Index

This index provides a categorized view of all architectural decisions made for the Weather Wallpaper App. ADRs are listed by category for easy discovery. For more about the ADR system, see [README.md](./README.md).

---

## All ADRs (Chronological)

| # | Title | Status | Date |
|---|-------|--------|------|
| [ADR-001](./ADR-001-use-systemparametersinfo-for-wallpaper.md) | Use SystemParametersInfo for Wallpaper Changes | Accepted | 2026-02-14 |
| [ADR-002](./ADR-002-self-contained-deployment.md) | Self-Contained Deployment Model | Accepted | 2026-02-14 |
| [ADR-003](./ADR-003-no-retry-logic-for-failures.md) | No Retry Logic for Transient Failures | Accepted | 2026-02-14 |
| [ADR-004](./ADR-004-appsettings-json-configuration.md) | Use appsettings.json for Configuration | Accepted | 2026-02-14 |

---

## By Category

### Core Architecture

- **[ADR-001](./ADR-001-use-systemparametersinfo-for-wallpaper.md)** - Use SystemParametersInfo for Wallpaper Changes
- **[ADR-004](./ADR-004-appsettings-json-configuration.md)** - Use appsettings.json for Configuration

### Deployment & Distribution

- **[ADR-002](./ADR-002-self-contained-deployment.md)** - Self-Contained Deployment Model

### Error Handling & Reliability

- **[ADR-003](./ADR-003-no-retry-logic-for-failures.md)** - No Retry Logic for Transient Failures

### Testing Strategy

*(No ADRs yet - testing strategy is covered in STORY_MAP.md)*

### Future Categories

As the project grows, additional categories may include:
- Security
- Performance
- Observability
- Extensibility

---

## Quick Reference: Active Decisions

These ADRs have **Status: Accepted** and should guide current development:

1. **Windows API**: Use `SystemParametersInfo` with P/Invoke ([ADR-001](./ADR-001-use-systemparametersinfo-for-wallpaper.md))
2. **Deployment**: Bundle .NET runtime in executable ([ADR-002](./ADR-002-self-contained-deployment.md))
3. **Error handling**: Log failures, no retries ([ADR-003](./ADR-003-no-retry-logic-for-failures.md))
4. **Configuration**: JSON file over registry/command-line args ([ADR-004](./ADR-004-appsettings-json-configuration.md))

---

## How to Use This Index

- **Starting a new story?** Review the relevant category to understand constraints
- **Proposing a change?** Check if an existing ADR covers it; if so, update or supersede
- **Onboarding?** Read all "Accepted" ADRs to understand the project's foundation

---

**Last Updated**: 2026-02-14
