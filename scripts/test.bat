@echo off
echo ========================================
echo Running automated tests...
echo ========================================
echo.

cd /d "%~dp0\..\src"
dotnet test --verbosity normal

if errorlevel 1 (
    echo.
    echo [31mTests failed! Fix failing tests before proceeding.[0m
    exit /b 1
) else (
    echo.
    echo [32mAll tests passed![0m
    exit /b 0
)
