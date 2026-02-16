# Development Guidelines for Claude

## Critical: Build & Test Before Pushing

**ALWAYS** run the following before any git push:

```powershell
# On Windows
.\scripts\build.bat
.\scripts\test.bat

# On Linux/Mac (if dotnet is available)
dotnet build -c Release
dotnet test
```

## Testing Requirements

- All tests must pass in Release configuration
- No xUnit analyzer warnings (treat warnings as errors)
- Verify thread safety tests complete without deadlocks
- Check for common issues:
  - xUnit1031: Use async/await instead of blocking task operations
  - Proper disposal of resources in tests
  - No hardcoded paths that break on different platforms

## Git Workflow

1. Develop on designated feature branch (e.g., `claude/wallpaper-sync-phase-1-9oLWN`)
2. Run build & test scripts
3. Commit with clear messages
4. Push only after all tests pass

## Code Quality

- Follow existing code style and patterns
- No blocking operations in async contexts
- Use proper async/await patterns
- Ensure thread safety for concurrent operations
- Add tests for new functionality
