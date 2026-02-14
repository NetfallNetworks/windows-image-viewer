# Weather Wallpaper App

A Windows desktop application that automatically updates your desktop wallpaper with weather forecast images.

## Requirements

- Windows 10 or Windows 11 (x64)
- .NET 8 SDK (for building from source)

## Build

```bash
cd src/WallpaperApp
dotnet build
```

## Test

```bash
cd src
dotnet test
```

## Run

```bash
cd src/WallpaperApp
dotnet run
```

## Publish (Self-Contained)

Produces a standalone executable that runs without .NET installed:

```bash
cd src/WallpaperApp
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

Then run:

```bash
./publish/WallpaperApp.exe
```
