# Build & Run

## Prerequisites

- **.NET 8 SDK** (LTS). Verify with `dotnet --version` (expects `8.0.x`).
- No administrator rights are required to build, test, or run.

Installing the SDK:

- **Windows/macOS:** download from <https://dotnet.microsoft.com/download/dotnet/8.0>.
- **Debian/Ubuntu:** `sudo apt-get install -y dotnet-sdk-8.0`.
- **Any OS (script):** <https://dotnet.microsoft.com/download/dotnet/scripts>.

The repository pins the SDK band in `global.json` (`rollForward: latestFeature`), targets
`net8.0`, and restores packages from nuget.org (`nuget.config`).

## Common commands

```bash
# Restore dependencies
dotnet restore Lumen.sln

# Build everything (Release)
dotnet build Lumen.sln -c Release

# Run the unit-test suite
dotnet test

# Launch the app (full functionality on Windows)
dotnet run --project src/Lumen.App
```

## Platform notes

- **Building and testing** work on Windows, Linux, and macOS — the whole solution and its tests
  are cross-platform, which is how CI verifies the safety-critical code.
- **Running the desktop app** is supported on Windows for full functionality. Avalonia can render
  on Linux/macOS too, but Windows-only features (Roblox installation detection and launching,
  DPAPI credential storage) are guarded at runtime and report themselves unavailable on other
  platforms rather than guessing.
- **Headless environments:** the GUI needs a display, but the entire test suite runs headless and
  without administrator rights.

## Project layout

- `src/Lumen.App` — Avalonia entry point + DI composition root (`AssemblyName: Lumen`).
- `src/Lumen.UI` — views and view-models.
- `src/Lumen.Core` — models, `Result<T>`, configuration, service interfaces.
- `src/Lumen.Security`, `src/Lumen.Storage` — security primitives and persistence.
- `src/Lumen.{Roblox,Accounts,Mods,Profiles,Diagnostics}` — implemented domain services.
- `src/Lumen.{Launching,Installations,Performance,Resolution,Graphics,Overlays,Themes,Extensions,Downloads,Updates}`
  — interfaces + data for phased features.
- `tests/Lumen.Tests` — xUnit tests.

## Troubleshooting

- **`dotnet` not found:** ensure the SDK is installed and on `PATH`.
- **Restore fails behind a proxy:** ensure HTTPS access to `api.nuget.org`.
- **Build warnings as errors:** the build keeps `TreatWarningsAsErrors=false` so a clean first
  build is easy; the code is nonetheless warning-free.
