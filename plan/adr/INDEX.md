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
| [ADR-005](./ADR-005-pivot-service-to-tray-app.md) | Pivot from Windows Service to System Tray Application | Accepted | 2026-02-15 |
| [ADR-006](./ADR-006-default-fitmode-fit.md) | Default FitMode Should Be "Fit" Not "Fill" | Accepted | 2026-02-16 |
| [ADR-007](./ADR-007-widget-board-with-sparse-package.md) | Widget Board Integration via Sparse MSIX Identity Package | Accepted | 2026-02-22 |

---

## By Category

### Core Architecture

- **[ADR-001](./ADR-001-use-systemparametersinfo-for-wallpaper.md)** - Use SystemParametersInfo for Wallpaper Changes
- **[ADR-004](./ADR-004-appsettings-json-configuration.md)** - Use appsettings.json for Configuration
- **[ADR-005](./ADR-005-pivot-service-to-tray-app.md)** - Pivot from Windows Service to System Tray Application
- **[ADR-006](./ADR-006-default-fitmode-fit.md)** - Default FitMode Should Be "Fit" Not "Fill"

### Widget & Rendering

- **[ADR-007](./ADR-007-widget-board-with-sparse-package.md)** - Widget Board Integration via Sparse MSIX Identity Package

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
5. **Application architecture**: System Tray App over Windows Service ([ADR-005](./ADR-005-pivot-service-to-tray-app.md))
6. **Default FitMode**: Use Fit (shows full image) over Fill (crops) ([ADR-006](./ADR-006-default-fitmode-fit.md))
7. **Widget Board packaging**: Sparse MSIX identity package alongside existing MSI ([ADR-007](./ADR-007-widget-board-with-sparse-package.md))

---

## How to Use This Index

- **Starting a new story?** Review the relevant category to understand constraints
- **Proposing a change?** Check if an existing ADR covers it; if so, update or supersede
- **Onboarding?** Read all "Accepted" ADRs to understand the project's foundation

---

**Last Updated**: 2026-02-22
