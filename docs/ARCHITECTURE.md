# Architecture

## Overview

Lumen is a modular .NET 8 solution using Avalonia UI with the MVVM pattern and constructor
dependency injection. The design separates the UI, the domain services, and the security-critical
primitives so that the safety logic is small, isolated, and fully unit-tested.

## Layers

```
┌─────────────────────────────────────────────────────────────┐
│ Lumen.App        Avalonia host + composition root (DI wiring) │
├─────────────────────────────────────────────────────────────┤
│ Lumen.UI         Views (XAML) + view-models (MVVM)            │
│                  depends only on Lumen.Core abstractions      │
├─────────────────────────────────────────────────────────────┤
│ Domain services  Roblox · Accounts · Mods · Profiles ·        │
│                  Diagnostics · (phased) Launching, Perf, …     │
│                  implement Lumen.Core interfaces              │
├─────────────────────────────────────────────────────────────┤
│ Lumen.Security   Path safety · archive extraction · hashing · │
│                  URL allowlist · redaction · arg escaping     │
│ Lumen.Storage    Atomic writes · backups · versioned JSON     │
├─────────────────────────────────────────────────────────────┤
│ Lumen.Core       Models · Result<T> · config · interfaces     │
│                  (no dependencies)                            │
└─────────────────────────────────────────────────────────────┘
```

Dependencies point downward only. `Lumen.Core` depends on nothing; the UI depends only on
`Lumen.Core` abstractions and is composed by `Lumen.App`, which is the sole place concrete
implementations are chosen.

## Key patterns

- **MVVM** via `CommunityToolkit.Mvvm` (`ObservableObject`, `[ObservableProperty]`,
  `[RelayCommand]` source generators). A `ViewLocator` maps view-models to views by convention.
- **Dependency injection** via `Microsoft.Extensions.DependencyInjection`. `Composition.Build()`
  is the single composition root.
- **Result types** (`Result` / `Result<T>`) — expected/recoverable failures (validation,
  extraction, parsing) are values, not exceptions, so security paths cannot rely on exception
  control flow or silently swallow errors.
- **Async + cancellation** throughout the service layer.
- **Interfaces in Core** so the UI and tests depend on abstractions, and platform-specific
  implementations can be swapped.

## Platform strategy

All projects target `net8.0` and build/test on any OS, which lets the security-critical code be
verified in a cross-platform pipeline. Windows-only runtime behaviour is isolated behind
interfaces with cross-platform fallbacks that report unavailability honestly rather than guessing:

- `IRobloxInstallationLocator` → `RobloxInstallationLocator` guards on `OperatingSystem.IsWindows()`
  and returns `PlatformUnsupported` elsewhere.
- `ISecureCredentialStore` → `DpapiCredentialStore` (Windows DPAPI) or `UnavailableCredentialStore`
  (explicit failure, never an insecure fallback), selected in the composition root.

Windows-specific features still to be implemented (launch hand-off, process priority, native
overlays, display changes) follow the same pattern: an interface in `Lumen.Core` (or the phased
project), a Windows implementation guarded at runtime, and a clearly-marked fallback.

## Navigation

The `ShellViewModel` owns an ordered list of `NavigationEntry` (id, title, glyph, page
view-model). Selecting an entry swaps the active `PageViewModel`, whose view the `ViewLocator`
resolves. Pages implement `InitializeAsync()` to load their data and are required to handle their
own errors (never throw). The navigation order and page visibility are backed by
`LumenSettings.NavigationOrder`/`HiddenPages` to support reordering and hiding.

## Data storage

All state persists as human-readable, versioned JSON under a single root (`ILumenPaths`). Writes
go through `AtomicFile` (temp-then-move with backup) and `JsonFileStore`, which recovers to
defaults and quarantines a corrupt file rather than crashing or losing data silently.

## Testing

`Lumen.Tests` (xUnit) covers the security primitives, storage, mod pipeline, Roblox validation,
accounts, profiles, and shell navigation. Tests run on Linux/macOS/Windows without a display or
administrator rights.
