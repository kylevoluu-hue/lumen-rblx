# Privacy

Lumen is built to be private and transparent. Everything Lumen stores lives on your device, and
nothing is uploaded automatically — ever.

## What Lumen stores (locally only)

All data lives under a single root so you can inspect, export, or delete it in one place:

- **Windows:** `%APPDATA%\Lumen`
- **Portable mode:** a `LumenData` folder next to the executable

| Data | Location | Contains secrets? |
| --- | --- | --- |
| Settings | `config/settings.json` | No |
| Account labels | `accounts/accounts.json` | No — nicknames and public ids only |
| Profiles | `profiles/profiles.json` | No |
| Logs | `logs/` | No — redacted, opt-in |
| Installed mods | `mods/` | No |
| Backups | `backups/` | No |

## What Lumen never does

- **Never** sells or shares your data.
- **Never** uploads launch history, keystroke data, overlay data, or diagnostics automatically.
- **Never** collects passwords, cookies, session tokens, payment data, browser history, or
  personal files.
- **Never** fingerprints your device or enables analytics/telemetry by default (there is none to
  enable in this milestone).

## Opt-in by default-off

Per the first-launch requirements, every potentially sensitive capability is **off** until you
explicitly enable it: optional logging, diagnostic exports, startup launch, Discord Rich
Presence, update checks, community mods, and third-party extensions.

## Logging

Logging is local, minimal, opt-in, and **redacted**. Every log line passes through
`Lumen.Security.Redactor`, which removes the `.ROBLOSECURITY` cookie, authorization/bearer/CSRF
tokens, private-server codes, IP addresses, and OS user paths. Logs never contain passwords,
cookies, tokens, chat messages, captured keystrokes, browser history, or device identifiers.

## Diagnostics

Diagnostic reports are always **previewed** before you do anything with them and are **never
uploaded automatically**. They deliberately exclude machine name, user name, and other device
identifiers, and are redacted as a final safety net. IP addresses are excluded unless you
explicitly opt to include them in a specific report.

## Your controls

The in-app **Privacy** page shows exactly what is stored and where, and lets you open the data
folder. Additional controls (clear cache, delete logs, remove saved accounts, export
non-sensitive settings, delete all Lumen data) are being surfaced as their phases land; the
underlying data model already keeps everything in one deletable location.

To remove everything now, delete the Lumen data folder listed above. See the forthcoming
`docs/DATA_REMOVAL.md` and `docs/UNINSTALL.md` for the full guides.
