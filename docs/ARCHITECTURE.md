# Lumen — Architecture & Phase 1 Proposal

> **Status:** Phase 1 (design). This document is the authoritative architecture for Lumen. It
> reconciles the detailed launcher specification with the code already implemented in this
> repository, states the engineering decisions honestly, and defines the roadmap. No Phase 2+
> code is written until this is reviewed.

> **Lumen is an independent, community project. It is not affiliated with, endorsed by, or
> sponsored by Roblox Corporation.** "Roblox" is a trademark of Roblox Corporation.

---

## 0. Context: this is not greenfield

A working Lumen already exists in this repository (branch
`claude/lumen-roblox-launcher-3ynxqy`): a modular **.NET 8 / Avalonia** solution with installation
detection, deep-link launching, an allowlisted FastFlag manager, a cosmetic mod pipeline
(scan → validate → hash-verify → apply/restore), profiles, account labels + browser sign-in,
public Roblox social data, a security core, and **102 passing unit tests** with a clean release
build. This Phase 1 document therefore describes the **target architecture** and maps every
requested service onto **what already exists vs. what is new**, so we evolve the codebase rather
than restart it.

## Decisions (confirmed)

1. **UI framework — support BOTH, on one shared core (confirmed: "use both if you can").** The UI
   is now split so any front-end can drive it:
   - `Lumen.Presentation` — **framework-agnostic** view-models + navigation + `IThemeApplier`
     abstraction (references only `Lumen.Core` + the MVVM toolkit; no Avalonia/WinUI).
   - `Lumen.Composition` — **UI-agnostic** service registration (`AddLumenServices`). Both heads
     build an identical service graph and add only their own `IThemeApplier`.
   - `Lumen.App` (**Avalonia head**) — the verified, cross-platform-buildable, shipping app.
   - `Lumen.WinUI` (**WinUI 3 head**) — reuses the same services and view-models; only views +
     theme applier are WinUI-specific. **Windows-only: it cannot compile in this Linux
     environment, so it is excluded from `Lumen.sln` and is an unverified skeleton to build/iterate
     on Windows** (`dotnet build src/Lumen.WinUI`). See `src/Lumen.WinUI/README.md`.

   Result: the Avalonia head remains the fully-tested, runnable app today; the WinUI head is a real
   second front-end sharing 100% of the services and view-models, finished on Windows.

2. **Naming — keep `Lumen.*` (confirmed).** Assembly/namespace names stay `Lumen.*`; the product
   display name is **"Lumen Launcher"**. The mapping to the spec's 8-project layout is in §3.

3. **Full Roblox client *installation* (download-from-scratch) vs. *detect-and-launch* baseline.**
   See §7 and §17. This is the single biggest new capability and the most brittle. It is proposed
   behind `IRobloxUpdateService`, clearly labeled "requires verification."

---

## Table of contents

1. Architecture proposal
2. Solution folder tree
3. Project responsibilities (and mapping to the spec's 8-project layout)
4. Core service interfaces (existing vs. new)
5. Main data models
6. Configuration schemas
7. Security boundaries
8. Update workflow
9. Launch workflow
10. Modification workflow
11. Backup & rollback workflow
12. Extension permission model
13. Account-authentication limitations
14. Testing strategy
15. Development roadmap
16. Technical risks
17. Features that depend on Roblox behavior (require verification)
18. Features that must NOT be implemented (unsafe / rule-breaking)

---

## 1. Architecture proposal

Lumen is a layered, MVVM, dependency-injected desktop application. Dependencies point **downward
only**; the UI depends only on abstractions in `Lumen.Core`, and the single composition root
(`Lumen.App/Composition.cs`) is the only place concrete implementations are chosen.

```
┌───────────────────────────────────────────────────────────────────────┐
│ Lumen.App         App host + DI composition root + startup             │  ← Presentation host
├───────────────────────────────────────────────────────────────────────┤
│ Lumen.UI          Views (XAML) + view-models (MVVM) + navigation shell │  ← Presentation
│                   depends ONLY on Lumen.Core abstractions              │
├───────────────────────────────────────────────────────────────────────┤
│ Domain services   Roblox · Accounts · Mods · Profiles · Launching ·    │  ← Services (impl)
│                   Diagnostics · Installations · Performance · Updates · │
│                   Backups · Extensions   (each implements Core ifaces)  │
├───────────────────────────────────────────────────────────────────────┤
│ Lumen.Security    Path safety · safe archive extraction · hashing ·    │  ← Infrastructure
│                   URL allowlist · redaction · argument escaping        │     (security-critical)
│ Lumen.Storage     Atomic writes · backups · versioned JSON store       │
│ Lumen.Diagnostics Redacted logging · sanitized diagnostic reports      │
├───────────────────────────────────────────────────────────────────────┤
│ Lumen.Core        Models · Result<T> · configuration · ALL interfaces  │  ← Core (no deps)
└───────────────────────────────────────────────────────────────────────┘
```

**Guiding principles**

- **Business logic never lives in the UI.** View-models orchestrate; services do the work.
- **Everything security-sensitive is small, centralized, and unit-tested** in `Lumen.Security`
  (path validation, extraction, hashing, redaction, URL allowlist, argument escaping).
- **`Result` / `Result<T>`** models expected failures as values, so validation/extraction/parsing
  never rely on exceptions and never silently swallow errors. Exceptions are reserved for
  programmer errors and are typed (§27 of the spec).
- **Async + `CancellationToken`** across all I/O; nullable reference types on; no static global
  state; no blocking network calls; no `Thread.Sleep` for synchronization.
- **Windows-only behavior is isolated behind interfaces** with cross-platform fallbacks that report
  unavailability honestly (so the safety-critical code is verifiable in CI, and the app degrades
  gracefully off-Windows instead of guessing).

## 2. Solution folder tree

```
Lumen.sln
├── Directory.Build.props        # shared: net8.0, nullable, analyzers, version
├── global.json                  # SDK pin
├── nuget.config
├── src/
│   ├── Lumen.App                # Avalonia head: host + composition (adds Avalonia IThemeApplier)
│   ├── Lumen.WinUI              # WinUI 3 head (Windows-only; excluded from Lumen.sln; skeleton)
│   ├── Lumen.UI                 # Avalonia views + ViewLocator + converters + AvaloniaThemeApplier
│   ├── Lumen.Presentation       # SHARED framework-agnostic view-models + navigation + IThemeApplier
│   ├── Lumen.Composition        # SHARED UI-agnostic service registration (AddLumenServices)
│   ├── Lumen.Core               # Models, Result<T>, configuration, ALL service interfaces
│   ├── Lumen.Security           # PathSafety, SafeArchiveExtractor, FileHashing, UrlValidator,
│   │                            #   Redactor, ProcessArguments
│   ├── Lumen.Storage            # AtomicFile, JsonFileStore (versioned + corruption recovery)
│   ├── Lumen.Diagnostics        # FileDiagnosticsLogger, DiagnosticReportBuilder
│   ├── Lumen.Roblox             # Installation locator, deep-link/launch-link, FastFlags,
│   │                            #   web client (public data), recent-experiences store
│   ├── Lumen.Accounts           # AccountManager, DpapiCredentialStore, Unavailable fallback
│   ├── Lumen.Mods               # ModManifestReader, ModSafetyScanner, ModPackageReader,
│   │                            #   ModApplicator
│   ├── Lumen.Profiles           # ProfileService (sanitized import/export)
│   ├── Lumen.Launching          # LaunchService, SystemProcessLauncher
│   └── Lumen.{Installations,Performance,Resolution,Graphics,Overlays,
│              Themes,Extensions,Downloads,Updates}   # domain interfaces + data; impls land per phase
├── tests/
│   └── Lumen.Tests              # xUnit (currently 102 tests)
├── examples/                    # safe theme, cosmetic mod, extension manifest
├── docs/                        # this file + README/SECURITY/PRIVACY/… (see §35 of spec)
└── (Phase 9) installer/         # Lumen.Installer — MSIX/signed installer + Squirrel-style updater
```

## 3. Project responsibilities & mapping to the spec's 8-project layout

The existing structure is **finer-grained** than the spec's 8 projects (which the spec explicitly
values: "Keep business logic out of the UI," "Use interfaces for all major services"). Mapping:

| Spec project | Realized by |
| --- | --- |
| `LumenLauncher.App` | `Lumen.App` |
| `LumenLauncher.Core` | `Lumen.Core` (models + interfaces + config) |
| `LumenLauncher.Models` | `Lumen.Core/Models` |
| `LumenLauncher.Infrastructure` | `Lumen.Security` + `Lumen.Storage` + `Lumen.Diagnostics` |
| `LumenLauncher.Services` | `Lumen.Roblox/Accounts/Mods/Profiles/Launching/Installations/Performance/Updates/…` |
| `LumenLauncher.Extensions` | `Lumen.Extensions` |
| `LumenLauncher.Tests` | `Lumen.Tests` |
| `LumenLauncher.Installer` | `Lumen.Installer` (Phase 9, new) |

Each project's responsibility is listed in the tree above. `Lumen.Core` has **no dependencies**;
the UI depends only on `Lumen.Core`; services implement `Lumen.Core` interfaces; `Lumen.App`
composes everything.

## 4. Core service interfaces (existing vs. new)

All interfaces live in `Lumen.Core.Abstractions`. Spec's requested services (§3) map as follows —
**✅ implemented**, **◐ partial**, **▷ new (proposed)**:

| Spec interface | Lumen interface(s) | Status |
| --- | --- | --- |
| `IRobloxInstallationService` | `IRobloxInstallationLocator` | ✅ (detect); ◐ integrity/repair new |
| `IRobloxVersionService` | `IRobloxVersionService` | ▷ new (version/channel resolution) |
| `IRobloxLaunchService` | `ILaunchService` + `IProcessLauncher` | ✅ |
| `IRobloxUpdateService` | `IRobloxUpdateService` | ▷ new (download/verify/install client) |
| `IModificationService` | `IModPackageReader` + `IModSafetyScanner` + `IModApplicator` | ✅ |
| `IProfileService` | `IProfileService` | ✅ |
| `IAccountSessionService` | `IAccountManager` | ✅ (labels + browser sign-in) |
| `IConfigurationService` | `ISettingsService` + `IVersionedJsonStore` | ✅ |
| `IBackupService` | `IBackupService` | ◐ (per-file backup exists in mods; formalize) |
| `IRepairService` | `IRepairService` | ▷ new |
| `IExtensionService` | `IExtensionHost` | ◐ (manifest/permission model exists; runtime new) |
| `IPerformanceService` | `IFastFlagManager` (+ `IPerformanceService`) | ◐ (FPS/graphics via FastFlags) |
| `IDiagnosticsService` | `IDiagnosticReportBuilder` + `IDiagnosticsLogger` (+ Pulse) | ◐ (Pulse new) |
| `INotificationService` | `INotificationService` | ▷ new |
| `ISecurityService` | `PathSafety`/`UrlValidator`/`Redactor` (wrap in `ISecurityService`) | ◐ |
| `IFileIntegrityService` | `FileHashing` (wrap in `IFileIntegrityService`) | ◐ |
| `IDeepLinkService` | `IExperienceLinkValidator` + `RobloxDeepLink` | ✅ (formalize as `IDeepLinkService`) |
| `IProcessService` | `IProcessLauncher` (+ process detection) | ◐ (detect/close new) |

New interfaces are defined in Phase 3/5/6/8 as their features land — never as empty stubs claimed
to work.

## 5. Main data models (`Lumen.Core/Models`)

Immutable `record`s where suitable; mutable classes for editable config. Existing models:
`RobloxInstallation`, `Account`, `LumenProfile`, `ExperienceTarget`, `ModManifest`, `ModPackage`,
`ModScanReport`, `FastFlagDefinition`, `RobloxUser`, `RobloxFriend`, `RecentExperience`,
`DiagnosticInputs`. New/expanded for this spec:

- **`RobloxInstallation`** (expand): add `InstallationId`, `Channel`, `Architecture`,
  `InstallDateUtc`, `IntegrityState`, `IsActive`. Never execute a binary solely on name — require an
  expected directory and (where possible) a valid Authenticode signature before use.
- **`RobloxVersion`** ▷: `VersionGuid`, `Channel`, `Architecture`, `DetectedUtc`, `IsComplete`.
- **`LaunchProfile`** (expand `LumenProfile`): installation id, account id, performance preset,
  resolution, window mode, enabled mods/extensions, deep-link behavior, optional place id /
  private-server link, **allowlisted** launch arguments only, pre-launch/post-exit actions.
- **`LumenSpace`** ▷ (§17): isolated environment bundling profiles, mods, themes, account labels,
  extension selection, performance settings, backups, overlay layouts, shortcuts — with its own
  config directory.
- **`Modification` / `ModManifest`** (expand): files added/replaced/removed, per-file hashes,
  dependencies, conflicts, required permissions, restart requirement, trust status, signature state.
- **`BackupManifest`** ▷: restore-point id, created time, contents list, original-file hashes.
- **`ExtensionManifest`** (exists): id, version, author, permissions, compatibility range, entry
  point, signature, hashes.
- **`PulseReport`** ▷ (§18): overall status + list of `PulseCheck { id, severity, explanation,
  recommendedAction, autoRepairAvailable }`.

## 6. Configuration schemas

Versioned JSON via `IVersionedJsonStore` (atomic writes, backup-before-migrate, corruption
recovery, unknown-field tolerance). Strongly-typed classes only — no dynamic access for critical
settings. Directory layout under a single root (`%APPDATA%\Lumen` or a portable `LumenData\`):

```
Lumen/
  config/      settings.json (SchemaVersion, Appearance, Privacy, …)
  spaces/      <space-id>/…   (isolated per Space)
  profiles/    profiles.json
  mods/        installed packages + applied.json (+ staging/)
  themes/
  extensions/
  backups/     content/ + restore points
  logs/
  cache/
  diagnostics/
  temp/
```

Each schema carries `SchemaVersion` and an additive migration path. `LumenSettings` already exists
(appearance, privacy defaults all-off, navigation order/hidden pages). Space-scoped config is new
(§17): a Space selects which `config/` subtree is active.

## 7. Security boundaries

**Trust boundary:** every external file, URL, archive, manifest, extension, mod, theme, config
import, and deep link is **untrusted**. All of it passes through `Lumen.Security` before use.
Implemented and unit-tested today:

- **Path traversal / zip-slip** — `PathSafety` (rejects `..`, absolute/rooted, drive/UNC, ADS
  colons, control chars; confines to a root) + `SafeArchiveExtractor` (per-entry/total/ratio/count
  caps enforced *while streaming*; rejects symlinks, encrypted, oversized, decompression bombs;
  `CreateNew` prevents overwrite/duplicate).
- **Hashing / integrity** — `FileHashing` (SHA-256, constant-time compare).
- **URL allowlist** — `UrlValidator` (HTTPS-only, host allowlist, rejects embedded credentials).
- **Redaction** — `Redactor` (cookies, tokens, auth headers, private-server links, IPs, user paths)
  in front of every log line and diagnostic export.
- **Safe process launch** — `ProcessArguments` (Windows arg escaping; control-char rejection);
  processes launched with discrete, validated arguments — never a composed shell string.

**Approved-root model:** writes are confined to the Lumen data root and (for mods) the resolved
Roblox `content/` tree; the applicator resolves every destination through `PathSafety`.
**Least privilege:** runs as `asInvoker` (no admin); elevation only for a single, clearly-named
operation if ever required. **Atomic replacement** everywhere (temp-then-move + backup).
**To add in later phases:** Authenticode signature verification (installs, updates, extensions),
DLL-search-order hardening, and an out-of-process extension sandbox (§12/§21).

## 8. Update workflow

Two distinct updaters:

**(a) Roblox client update** (`IRobloxUpdateService`, ▷ new — see §17 caveat): detect current
version/channel → check the **official** Roblox deployment endpoint for the channel → if newer,
download packages over HTTPS to `temp/` → verify hashes → **back up the working install** →
install atomically into a new version folder → verify → mark active → roll back to the last valid
install on any failure. Never launch while an update is incomplete; never fetch client files from
third-party mirrors. *Exact endpoints/manifest format are undocumented and isolated behind the
interface (see §17).*

**(b) Lumen self-update** (`IUpdateService`, ◐): HTTPS only, **signed** metadata + hash
verification, release notes shown, stable/beta/dev channels, download to `temp/`, apply atomically,
roll back on failure, never run unsigned downloaded executables, never update during sensitive
operations, portable mode opts out. (Existing `IUpdateService` interface + `UrlValidator` +
`FileHashing` are the building blocks.)

## 9. Launch workflow

Implemented today and expanded per profile:

1. Resolve the effective **profile** (explicit, deep-link rule, or default) and its **Space**.
2. **Validate** the target via `IExperienceLinkValidator` (place id / official URL / private-server
   link; rejects malformed input and control chars; private codes are never logged).
3. Verify Roblox is installed and **not mid-update**; detect running processes (`IProcessService`).
4. Apply the profile: performance/FastFlags, resolution, enabled mods (with backups), overlays.
5. Build the official **launch link** (`roblox://experiences/start?placeId=…`, or the web link with
   the private-server code) and hand off via `IProcessLauncher` (shell-execute, single discrete
   target — no shell string, no auth ticket, no injection).
6. Record the launch locally (recent experiences, last duration, status); run post-exit actions;
   restore any temporary changes.

**Safe Launch** (§19): same pipeline with all mods/extensions/overlays disabled and default
performance, plus a **binary-search troubleshooting** mode that disables groups of mods across
user-initiated launches to isolate a conflict. Never automates gameplay.

## 10. Modification workflow

Implemented today (`Lumen.Mods`), manifest-based and reversible:

1. Open `.lumenmod` (a ZIP) → `SafeArchiveExtractor` into disposable staging (zip-slip/bomb guarded).
2. Parse + **validate** `manifest.json` (`ModManifestReader`).
3. **Safety scan** (`ModSafetyScanner`): block executables/scripts/registry files/symlinks/double
   extensions/traversal; flag embedded URLs and unknown types.
4. **Verify declared hashes** (`FileHashing`).
5. **Conflict detection** against already-applied files/ids; show the exact files that will change;
   require explicit approval for untrusted mods.
6. **Apply atomically** (`ModApplicator`): copy the package's `Content/` subtree into Roblox's
   `content/` folder, backing up each replaced original once (`applied.json` records the set).
7. **Restore** reverts every original (or removes Lumen-added files) and clears the record.

If any step fails, staging is discarded and nothing is left partially applied.

## 11. Backup & rollback workflow

`IBackupService` (◐ → formalized): named restore points capturing launcher settings, profiles,
Spaces, mod manifests, modified Roblox files (where appropriate), original-file hashes, themes,
overlay layouts, and extension config. Automatic backups **before updates and before applying
mods**; retention limit + size estimate; restore preview + selective restore; full factory reset;
export/import to ZIP (through `SafeArchiveExtractor`, so import is traversal-safe). Rollback is the
default failure path for updates and mod application (atomic temp-then-move + restore-from-backup).

## 12. Extension permission model

Extensions are **separate** from cosmetic mods and run under least privilege. `ExtensionManifest`
(exists) declares id/version/author/compat/entry-point/signature/hashes and an explicit
**permission set** (e.g. `ReadLumenSettings`, `ModifyLumenSettings`, `ReadRobloxInstallationInfo`,
`ModifyRobloxFiles`, `AccessNetwork(approvedDomains)`, `StartApprovedProcess`, `DisplayNotification`,
`AddUiPage`). Rules: no unrestricted file-system/process access by default; permissions shown on a
review screen **before** install; enable/disable, remove, per-extension logs; **crash isolation**
and **timeouts**; a **Safe Mode** disables all third-party extensions. The runtime should be an
**out-of-process host** (or equivalent isolation boundary) — no API capable of stealthy credential
collection. (Manifest/permission model exists; sandboxed host is Phase 8, ▷.)

## 13. Account-authentication limitations (honest)

Roblox provides **no supported public API** for a third-party launcher to log in or switch accounts
without importing the `.ROBLOSECURITY` session cookie — which Lumen will never do. Therefore
(implemented today):

- **Account labels only** — nicknames, public user id, display name, avatar URL (fetched from
  public APIs). No passwords, cookies, or tokens are ever handled, stored, logged, or exported.
- **Sign-in = official browser flow** — a button opens Roblox's official login page in the default
  browser; launches then use the client session the user is already signed into.
- Any permitted local secret (there are none today) would use **Windows DPAPI / Credential
  Manager**, never plaintext JSON. `DpapiCredentialStore` exists with an explicit *unavailable*
  fallback off-Windows (no insecure fallback).

If Roblox ever ships an official OAuth-style flow, it slots in behind `IAccountManager` /
`ISecureCredentialStore` without touching the rest of the app.

## 14. Testing strategy

`Lumen.Tests` (xUnit), **102 tests today**, cross-platform, no admin, no real Roblox install.
Mock the network (`HttpMessageHandler`), file system (temp dirs), and process (`FakeProcessLauncher`).
Covered now: path traversal, zip-slip, decompression bombs, hashing, URL validation, redaction,
config load/corruption/migration, atomic writes, mod manifest/scanner/reader/applicator,
experience-link + deep-link validation, profile import/export sanitization, account
add/remove/persistence, FastFlag validation/preview, recent-store, web-client allowlist, and the
**ViewLocator reflection guard** (every page resolves to a real view). New per phase: version
parsing, update **signature** validation, backup restore, Space isolation, extension permission
checks, command-line parsing, deep-link rejection cases, repair. Integration tests that touch a
real install are clearly marked optional (§34).

## 15. Development roadmap

Maps the spec's phases (§38) onto the existing code. **✓ done this session**, others proposed.

- **Phase 1 — Architecture (this document).**
- **Phase 2 — Shell/DI/config/logging/theme.** ✓ (Avalonia shell, DI, `SettingsService`, redacted
  logging, accent theming, animated navigation).
- **Phase 3 — Installation/version/process detection + launching.** ◐ (detection + deep-link launch
  ✓; version service, process detect/close, `IProcessService` ▷).
- **Phase 4 — Profiles & Spaces, import/export, per-profile launch, shortcuts.** ◐ (profiles ✓;
  Spaces, desktop shortcuts, jump-list ▷).
- **Phase 5 — Mods, manifests, conflicts, backup/rollback, security tests.** ◐ (pipeline + apply +
  restore + security tests ✓; conflict UI, `IBackupService` restore points ▷).
- **Phase 6 — Updates & repair, Lumen Pulse, Safe Launch, diagnostic exports.** ◐ (sanitized
  diagnostics ✓; Pulse, Safe Launch, repair levels, client updater ▷).
- **Phase 7 — Overlays, themes, cursor/crosshair, visual mods.** ◐ (visual filters/mods pipeline ✓;
  overlays = separate transparent process, cursor/crosshair editors ▷).
- **Phase 8 — Extension host, permissions, isolation, sample extension.** ◐ (manifest/permissions
  ✓; sandboxed host + sample ▷).
- **Phase 9 — Installer, docs, accessibility & security review, testing.** ◐ (README/SECURITY/
  PRIVACY/THREAT_MODEL/etc. ✓; MSIX/signed installer, full a11y/security audit ▷).

At each phase end: list files created/modified, build+test instructions, what works, what's
incomplete, and security concerns — then wait for the next instruction.

## 16. Technical risks

- **WinUI 3 vs. Avalonia** (Decision 1): choosing WinUI 3 forfeits cross-platform build/test and
  requires a UI rewrite; choosing Avalonia is a documented deviation from the spec's default.
- **Roblox client install/update brittleness** (§7/§17): endpoints and package format are
  undocumented and change; this is the highest-maintenance feature.
- **Windows-runtime unverifiability in CI**: launching, DPAPI, content-folder writes, native
  overlays, and process control can't be executed on Linux — logic is unit-tested; runtime behavior
  is verified on Windows. This is why those pieces sit behind interfaces with honest fallbacks.
- **FastFlag drift**: Roblox can rename/remove flags; the allowlist must be maintained and unknown
  flags rejected (already enforced).
- **Overlay rendering**: a click-through, multi-monitor transparent overlay process is non-trivial
  and Windows-specific.
- **Extension sandbox**: true isolation (out-of-process + restricted token) is substantial; until
  it exists, third-party extensions stay off by default (Safe Mode).
- **Code signing**: unsigned builds trigger SmartScreen; a real certificate is needed for a smooth
  install and for update signature verification.

## 17. Features that depend on Roblox behavior (require verification)

Isolated behind interfaces and labeled `// requires verification`:

- **Client download/version/channel** (`IRobloxUpdateService`, `IRobloxVersionService`): Roblox
  distributes the client via official deployment CDNs and a client-version endpoint. The exact
  endpoints, channel semantics, and package manifest format are **undocumented and change**. The
  **safe baseline that exists today is detect-and-launch** (use an already-installed official
  client, hand off via the `roblox://` deep link). Full download-and-install is proposed but must be
  validated against current Roblox behavior before it is trusted.
- **`roblox://` deep-link parameters** and private-server link format: used for launch; may change.
- **FastFlags** (`ClientAppSettings.json`): the supported override mechanism; individual flags may
  be renamed/removed by Roblox.
- **Public web APIs** (users/friends/thumbnails/games): public and unauthenticated, but shapes and
  rate limits can change.
- **Cosmetic content-folder mods**: rely on Roblox's `content/` layout, which can shift between
  versions; mods declare supported versions and are re-verified after updates.

## 18. Features that must NOT be implemented (unsafe / rule-breaking)

Permanently excluded — not "unimplemented," **excluded by design**:

- DLL injection, memory reading/editing, process/renderer hooking, script execution/executors,
  anti-cheat or moderation bypass.
- ESP / wallhack / X-ray, aimbot / trigger bot, hitbox or reach changes, no-clip, flight, speed,
  infinite jump, teleport, automation for unfair advantage, packet/economy manipulation.
- `.ROBLOSECURITY` cookie or session-token import, password collection/forms, browser-cookie or
  Discord-token extraction, session upload/sharing.
- Hidden telemetry/analytics, silent background data collection, remote administration, crypto
  mining, device fingerprinting, silent persistence, undisclosed network requests.
- Disabling antivirus/Windows Defender/security features, adding AV exclusions, silent driver or
  service installation, global system-setting changes without explicit consent.
- "FullBright"/lighting sold as a competitive advantage — lighting is offered only as an
  accessibility/visual-preference feature through supported, non-invasive configuration, never to
  reveal hidden gameplay elements.

The mod safety scanner and extension permission model **enforce** these exclusions: any package
requiring injection, memory/process access, cookies/tokens, or a hidden executable is blocked.
