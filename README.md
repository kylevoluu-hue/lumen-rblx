# Lumen

**A secure, high-performance, customizable Roblox launcher and cosmetic configuration manager for Windows.**

> ⚠️ **Lumen is an independent, community-created launcher and is not affiliated with,
> endorsed by, sponsored by, or officially connected to Roblox Corporation.**
> "Roblox" is a trademark of Roblox Corporation. Lumen is unofficial.

Lumen focuses on fast startup, deep and safe customization, privacy, transparency, and
accessibility. It is a **launcher and configuration manager** — deliberately **not** a cheat
client, exploit loader, script executor, injector, or account-token importer. Any feature that
cannot be built safely and through supported mechanisms is left disabled with an honest
explanation rather than implemented insecurely.

---

## What Lumen will never do

Lumen contains no malware, spyware, adware, crypto mining, keylogging, hidden telemetry, or
device fingerprinting. It never:

- injects into Roblox, hooks its renderer, or reads/edits Roblox memory;
- executes scripts inside Roblox or bypasses anti-cheat / moderation;
- imports the `.ROBLOSECURITY` cookie, session tokens, or passwords;
- asks you for your Roblox password, or to paste cookies/tokens;
- asks you to disable antivirus/Defender, add exclusions, or install drivers;
- makes undisclosed network requests or runs hidden background processes.

See [SECURITY.md](SECURITY.md), [PRIVACY.md](PRIVACY.md), and
[docs/THREAT_MODEL.md](docs/THREAT_MODEL.md) for the full guarantees.

## Technology

- **.NET 8 (LTS)** and **C#**, with nullable reference types, async/await, cancellation tokens.
- **[Avalonia UI](https://avaloniaui.net/)** for a fast, modern, cross-platform-buildable,
  native Windows desktop app with MVVM (CommunityToolkit.Mvvm) and dependency injection.
- Cross-platform, dependency-light security core so the safety-critical code is unit-tested on
  every platform, while Windows-only runtime features are cleanly abstracted.

> **Why Avalonia?** It builds and unit-tests on Linux/macOS/Windows (so this repository's CI can
> verify the whole app), while still shipping a native Windows release. WinUI 3 / WPF are
> Windows-only and could not be built or verified in a cross-platform pipeline.

## Status — this is Milestone 1 (Foundation + Security Core)

This is an honest, in-progress foundation. The following is **implemented and unit-tested**:

| Area | Status |
| --- | --- |
| Modular solution, DI, navigation shell (17 pages) | ✅ Implemented |
| Privacy-safe logging + sensitive-data redaction | ✅ Implemented & tested |
| Versioned config with atomic writes, backups, corruption recovery | ✅ Implemented & tested |
| Mod safety scanner (blocks executables/scripts/traversal/double-extensions) | ✅ Implemented & tested |
| Safe archive extraction (zip-slip + decompression-bomb guards) | ✅ Implemented & tested |
| `.lumenmod` package reader (extract → validate → scan → hash-verify) | ✅ Implemented & tested |
| SHA-256 hashing/verification, URL allowlist, safe process arguments | ✅ Implemented & tested |
| Experience-link validation (place id / URL / private server, code-safe) | ✅ Implemented & tested |
| Roblox installation detection (read-only, Windows) | ✅ Implemented |
| Account manager (local labels; secure DPAPI store; **sign-in marked unavailable**) | ✅ Implemented & tested |
| Profiles with sanitized import/export | ✅ Implemented & tested |
| Home dashboard bound to real data | ✅ Implemented |

**Deferred to later phases** (interfaces defined, honestly marked in-app): Windows launch
hand-off & installation repair; performance/FPS/resolution/graphics/FastFlag application;
overlays (FPS/ping/keystrokes/CPS/crosshair) rendering; texture/sound/font/skybox/mod-pack
managers; the extension sandbox; the secure updater; the first-launch wizard UI; and
installer/portable packaging. See [docs/FEATURE_FEASIBILITY.md](docs/FEATURE_FEASIBILITY.md) and
the roadmap notes shown on each in-app page.

## Project structure

```
Lumen.sln
├── src/
│   ├── Lumen.App            # Avalonia entry point + DI composition root
│   ├── Lumen.UI             # Views, view-models, navigation shell (MVVM)
│   ├── Lumen.Core           # Models, Result types, config, service interfaces
│   ├── Lumen.Security       # Path safety, archive extraction, hashing, URL/redaction, arg-escaping
│   ├── Lumen.Storage        # Atomic writes, backups, versioned JSON store
│   ├── Lumen.Diagnostics    # Redacted local logging, sanitized diagnostic reports
│   ├── Lumen.Roblox         # Install detection, experience-link validation, deep links
│   ├── Lumen.Accounts       # Account metadata, DPAPI credential store, auth availability
│   ├── Lumen.Mods           # Manifest reader, safety scanner, package reader
│   ├── Lumen.Profiles       # Profiles with sanitized import/export
│   └── Lumen.{Launching,Installations,Performance,Resolution,Graphics,
│              Overlays,Themes,Extensions,Downloads,Updates}   # Interfaces + data (phased)
├── tests/Lumen.Tests        # xUnit unit tests (83 and counting)
├── examples/                # Safe theme, cosmetic mod, and extension manifest
└── docs/                    # Architecture, threat model, feasibility, build, formats
```

## Build & run

Requires the **.NET 8 SDK**.

```bash
dotnet restore Lumen.sln
dotnet build Lumen.sln -c Release
dotnet test                       # runs the unit-test suite
dotnet run --project src/Lumen.App    # launches the app (Windows for full functionality)
```

See [docs/BUILD.md](docs/BUILD.md) for details and platform notes.

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
- [Threat model](docs/THREAT_MODEL.md)
- [Feature feasibility](docs/FEATURE_FEASIBILITY.md)
- [Authentication feasibility](docs/AUTHENTICATION.md)
- [Security policy](SECURITY.md) · [Privacy](PRIVACY.md)
- [Mod package format](docs/MOD_PACKAGE_FORMAT.md)
- [Contributing](CONTRIBUTING.md) · [Code of Conduct](CODE_OF_CONDUCT.md)

## License

To be finalized before first release (an OSI-approved permissive license such as MIT is
intended). Lumen bundles no Roblox-owned binaries and downloads no Roblox executables.
