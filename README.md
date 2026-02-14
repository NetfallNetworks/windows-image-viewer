# Weather Wallpaper App

A Windows desktop application that automatically updates your desktop wallpaper with weather forecast images from weather.zamflam.com every 15 minutes.

**Project Status**: 🚧 Under Active Development

---

## Quick Links

- **[Story Map](STORY_MAP.md)** - Complete project plan with all stories and acceptance criteria
- **[Architecture Decision Records (ADRs)](plan/adr/)** - Why we made key technical decisions
  - [ADR Index](plan/adr/INDEX.md) - Browse all decisions by category
  - [ADR System Guide](plan/adr/README.md) - How to create and use ADRs
- **[Story Implementation Plans](plan/)** - Step-by-step guides for implementing each story
  - [Story 0: Wallpaper API Spike](plan/story-0-implementation-plan.md)
  - [Story 1: Foundation](plan/story-1-implementation-plan.md)

---

## Project Philosophy

**Ship fast, zero maintenance, incremental progress.**

Every story delivers a working application with passing tests. No gold-plating, no premature optimization.

---

## For Contributors

### New to This Project?

Start here, in order:

1. **Read [STORY_MAP.md](STORY_MAP.md)** - Understand the project vision, story sequence, and definition of done
2. **Review [ADR Index](plan/adr/INDEX.md)** - Understand the foundational architectural decisions (5-10 min read)
3. **Pick a story** - Start with the next incomplete story in the sequence
4. **Read the implementation plan** - Each story has a detailed plan in `plan/story-N-implementation-plan.md`
5. **Implement following Uncle Bob principles** - See STORY_MAP.md for clean code checklist

### Directory Structure

```
windows-image-viewer/
├── README.md              # This file - project overview
├── STORY_MAP.md           # Complete project roadmap with all stories
├── plan/                  # Planning documents and ADRs
│   ├── adr/               # Architecture Decision Records
│   │   ├── README.md      # ADR system guide
│   │   ├── INDEX.md       # All ADRs by category
│   │   └── ADR-*.md       # Individual decisions
│   ├── spike-results.md   # Story 0 spike findings
│   ├── story-0-implementation-plan.md
│   └── story-1-implementation-plan.md
├── spike/                 # Story 0 throwaway validation code
│   └── WallpaperSpike/
├── src/                   # Application source code (created in Story 1)
│   ├── WallpaperApp/      # Main application
│   └── WallpaperApp.Tests/  # Unit and integration tests
└── .gitignore
```

### Key Architectural Decisions

Before writing code, understand these foundational choices:

| Decision | Rationale | ADR |
|----------|-----------|-----|
| Use SystemParametersInfo API | Well-documented, stable, no dependencies | [ADR-001](plan/adr/ADR-001-use-systemparametersinfo-for-wallpaper.md) |
| Self-contained deployment | No .NET installation required for users | [ADR-002](plan/adr/ADR-002-self-contained-deployment.md) |
| No retry logic | Simpler code, timer retries naturally | [ADR-003](plan/adr/ADR-003-no-retry-logic-for-failures.md) |
| appsettings.json config | Standard .NET pattern, testable, human-readable | [ADR-004](plan/adr/ADR-004-appsettings-json-configuration.md) |

See [ADR Index](plan/adr/INDEX.md) for all decisions.

---

## Build Instructions

*(To be completed in Story 1)*

---

## Current Status

### Completed Stories

- ✅ **Story 0**: Tech Spike - Wallpaper API validated
  - Validated P/Invoke approach with `SystemParametersInfo`
  - Documented findings in [spike-results.md](plan/spike-results.md)
  - Decision: Proceed with wallpaper approach (not widget)

### In Progress

- 🚧 **Story 1**: Foundation - Console App + First Test
  - See [story-1-implementation-plan.md](plan/story-1-implementation-plan.md) for implementation guide

### Next Up

- **Story 2**: Configuration - Read URL from appsettings.json
- **Story 3**: Wallpaper Service - Set Static Image as Wallpaper
- **Story 4**: HTTP Client - Fetch Image from URL
- **Story 5**: Integration - Fetch and Set Wallpaper

See [STORY_MAP.md](STORY_MAP.md) for complete story sequence.

---

## Contributing

### Workflow

1. Pick next incomplete story from [STORY_MAP.md](STORY_MAP.md)
2. Read the story's implementation plan in `plan/`
3. Read relevant ADRs to understand architectural context
4. Implement following TDD (tests first)
5. Check off all items in story's "Definition of Done"
6. Commit with format: `"Story N: Brief description"`
7. Move to next story

### Making Architectural Decisions

If you need to make a significant architectural decision:

1. Check `plan/adr/INDEX.md` - does an ADR already cover this?
2. If no existing ADR, create a new one (see `plan/adr/README.md` for template)
3. Discuss in PR or with team before marking "Accepted"
4. Update INDEX.md with the new ADR

### Testing Standards

- **All code must have tests** (target: >80% coverage)
- Write tests first (TDD)
- Tests must be fast, isolated, repeatable
- Manual test checklists for visual validation (wallpaper changes)

---

## License

*(To be determined)*