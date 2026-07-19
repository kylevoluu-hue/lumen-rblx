# Feature Feasibility

An honest assessment of the requested features: what is safely possible, what is deferred to a
later phase, and what is intentionally excluded because it cannot be done safely or through
supported mechanisms. Lumen never reduces security to make a feature appear complete.

## Legend

- ✅ **Implemented** in this milestone (and unit-tested where logic exists)
- 🧩 **Safe & planned** — feasible through supported means; scheduled for a later phase
- ⚠️ **Conditional** — possible only via a safe, supported measurement/mechanism, otherwise
  disabled with an explanation (never faked)
- ⛔ **Excluded** — cannot be done safely/supported; will not be implemented

## Core, security, storage

| Feature | Status |
| --- | --- |
| Modular architecture, DI, MVVM shell | ✅ |
| Versioned config, atomic writes, backups, corruption recovery | ✅ |
| Redacted local logging, sanitized diagnostics | ✅ |
| Path-safety, safe archive extraction, hashing, URL allowlist, arg escaping | ✅ |
| Secure updater (signed metadata, hash + signature verification, rollback) | 🧩 |
| Network Activity Viewer | 🧩 |
| Extension sandbox + permission enforcement + Safe Mode | 🧩 |

## Roblox integration

| Feature | Status | Notes |
| --- | --- | --- |
| Detect installation / version (read-only) | ✅ | Windows; reports "unsupported" elsewhere |
| Validate experience links (place id / URL / private server) | ✅ | Private codes never logged |
| Launch hand-off to the official client | 🧩 | Opens the official web link; no auth tickets, no injection |
| Repair/restore launcher-managed cosmetic files | 🧩 | Only Lumen-managed files; originals backed up |
| Modify/replace protected Roblox binaries | ⛔ | Never |

## Accounts & authentication

| Feature | Status | Notes |
| --- | --- | --- |
| Local account labels/metadata | ✅ | Nicknames, public ids/avatars only |
| Secure storage of permitted material (DPAPI) | ✅ | No insecure fallback |
| Interactive sign-in / account switching | ⛔→🧩 | **Unavailable today** — no supported Roblox method exists that does not require the forbidden `.ROBLOSECURITY` cookie. Built as UI + interfaces, marked unavailable. Will adopt an official method if Roblox provides one. See [AUTHENTICATION.md](AUTHENTICATION.md). |
| Cookie/token/password import | ⛔ | Never |

## Performance, display, graphics

| Feature | Status | Notes |
| --- | --- | --- |
| Honest performance presets/options (Lumen's own footprint) | 🧩 | No placebo RAM cleaners, ping boosters, registry edits, or security toggles |
| FPS-limit / frame-pacing via supported configuration | 🧩 | Legitimate config only; never memory patching |
| Resolution presets + validation | ✅ (data) / 🧩 (apply) | Presets/validation implemented; monitor placement is Windows-phase |
| Graphics presets with honest control-scope labels | 🧩 | Clearly distinguishes Lumen-controlled vs Roblox-exposed vs advisory |
| Safe FastFlag manager (allowlist, preview, backup, rollback) | 🧩 | No flags that bypass anti-cheat/moderation or reveal hidden players/objects |

## Overlays & visuals

| Feature | Status | Notes |
| --- | --- | --- |
| FPS / ping / frame-time / system-usage overlays | ⚠️ | Only via safe, non-injection measurement; otherwise disabled with an explanation. Never reads Roblox memory. |
| Keystrokes / CPS / mouse / crosshair / cursor overlays | 🧩 | Display-only; keystrokes limited to selected gameplay keys, in-memory only, never logged or sent. No auto-clickers or input automation. |
| Visual filters / FullBright **accessibility** preset | 🧩 | Safe OS/driver/Lumen-managed filters only. Never disables gameplay fog/darkness, reveals hidden players/objects, or hooks the renderer. |
| ESP / X-ray / aimbot / hitbox / reach / wallhack | ⛔ | Never |

## Mods & extensions

| Feature | Status | Notes |
| --- | --- | --- |
| `.lumenmod` reader, safety scanner, hash verify, manifest validation | ✅ | |
| Cursor/crosshair/texture/sound/font/skybox/UI managers, mod packs, load order | 🧩 | Cosmetic, validated, backed-up, reversible |
| Extension API/manifest/permission model | ✅ (model) / 🧩 (runtime) | Sandbox + enforcement are Phase 8 |
| Any mod requiring injection, memory access, cookies, or hidden executables | ⛔ | Blocked by the scanner |

The status shown in-app on each page mirrors this document.
