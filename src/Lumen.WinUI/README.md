# Lumen.WinUI — WinUI 3 head (Windows-only)

This is the **WinUI 3** front-end for Lumen. It is a second UI "head" that reuses the exact same
services and view-models as the Avalonia head — only the views and the theme applier are
WinUI-specific.

## ⚠️ Build status — honest note

This project **has not been compiled in this repository's CI environment, which is Linux.**
WinUI 3 / the Windows App SDK can only build on Windows. Because it cannot be verified here, treat
this as a **working skeleton to build and iterate on Windows**, not a finished, verified UI. In
particular, package versions (`Microsoft.WindowsAppSDK`, `Microsoft.Windows.SDK.BuildTools`) may
need bumping to the latest, and the per-page views are still being ported (see below).

It is intentionally **excluded from `Lumen.sln`** so the cross-platform solution keeps building and
testing cleanly. Build this head on Windows via `Lumen.Windows.sln` or:

```powershell
dotnet build src/Lumen.WinUI
```

## What it demonstrates (the point of "both")

- **One service graph, two heads.** `App.xaml.cs` calls the shared
  `Lumen.Composition.LumenServiceRegistration.AddLumenServices()` — the identical registration the
  Avalonia head uses — and only substitutes `WinUIThemeApplier` for the Avalonia one.
- **Shared view-models.** `MainWindow` hosts the shared `ShellViewModel`; the navigation entries and
  their page view-models are the same types in `Lumen.Presentation` that drive Avalonia. No
  view-model is duplicated.

## What is still to do on Windows

- Port each page's view to a WinUI `UserControl` bound to its shared view-model (the Avalonia views
  in `Lumen.UI/Views/Pages` are the reference). The content area currently shows the active page's
  title and description via the shared VM.
- Add a WinUI `ViewLocator` (a `DataTemplateSelector`) mirroring the Avalonia one.
- Add WinUI value converters for `byte[]` → `BitmapImage` (avatars) and `#RRGGBB` → brush, matching
  `Lumen.UI/Converters`.
- Confirm/refresh Windows App SDK package versions and MSIX packaging.

## Architecture

```
Lumen.Core ── Lumen.Presentation (shared VMs) ── Lumen.Composition (shared DI)
                                                     │
                        ┌────────────────────────────┴───────────────────────────┐
                   Lumen.App (Avalonia head, verified)          Lumen.WinUI (this, Windows-only)
```
