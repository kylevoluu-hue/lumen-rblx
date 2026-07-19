# Security Policy

Lumen treats security and privacy as the top priority, ahead of features. This document
summarizes Lumen's guarantees, its security engineering, and how to report a vulnerability.

## Non-negotiable guarantees

Lumen **never**:

- contains malware, spyware, adware, crypto mining, keylogging, hidden telemetry/analytics, or
  device fingerprinting;
- injects into Roblox, hooks DirectX/Vulkan/OpenGL or the Roblox renderer, reads or edits Roblox
  memory, or executes scripts inside Roblox;
- bypasses anti-cheat, moderation, or safety interfaces, or provides ESP / aimbot / hitbox /
  reach / X-ray / no-clip / flight / automation of any kind;
- imports the `.ROBLOSECURITY` cookie, session tokens, or passwords; scans browser cookies or
  history; or recreates the Roblox password form;
- asks you to disable antivirus/Defender, add antivirus exclusions, install unknown drivers, or
  run suspicious scripts;
- makes undisclosed network requests, runs hidden background processes, or silently persists.

Prohibited mods and extensions (ESP, aimbot, injectors, script executors, memory editors, cookie
importers, auto-clickers, etc.) are rejected. Any package requiring Roblox process access,
browser cookies, authentication tokens, injection, or a hidden executable is **blocked**.

## Security engineering

Implemented and unit-tested in this milestone:

- **Path-traversal prevention** (`Lumen.Security.PathSafety`) — rejects `..`, absolute/rooted
  paths, drive/UNC prefixes, ADS colons, and control characters; confines resolved paths to a
  root.
- **Safe archive extraction** (`SafeArchiveExtractor`) — zip-slip prevention plus
  decompression-bomb guards (per-entry, total-size, ratio, and entry-count caps enforced while
  streaming), duplicate/overwrite protection, and rejection of encrypted/corrupt archives.
- **File-hash verification** (`FileHashing`) — SHA-256 with constant-time comparison.
- **URL allowlisting** (`UrlValidator`) — HTTPS-only, host allowlist, rejects embedded
  credentials and malformed URLs.
- **Sensitive-data redaction** (`Redactor`) — strips the `.ROBLOSECURITY` cookie,
  authorization/bearer/CSRF tokens, private-server and other sensitive query codes, IP addresses,
  and OS user paths from all logs and diagnostics.
- **Safe process launching** (`ProcessArguments`) — correct Windows argument escaping and
  control-character rejection; Lumen launches executables with discrete validated arguments and
  never builds a shell command from user input.
- **Mod safety scanning** (`ModSafetyScanner`) — blocks executables, scripts, registry files,
  symlinks, double/hidden extensions, and path traversal; flags embedded URLs and unknown types.
- **Secure credential storage** (`DpapiCredentialStore`) — Windows DPAPI (current-user scope);
  an explicit "unavailable" store elsewhere with **no insecure fallback**. Secrets are never
  written to plain-text JSON.
- **Atomic writes + backups + rollback** (`AtomicFile`, `JsonFileStore`) — temp-then-move writes,
  automatic backups, and corruption quarantine/recovery.

Planned building blocks for later phases: digital-signature verification for updates and mods,
extension permission enforcement and sandboxing, and the Network Activity Viewer.

## Least privilege

Lumen runs as a standard user (`asInvoker` in the app manifest) and never requests silent
elevation. It never silently installs services, drivers, scheduled tasks, startup entries,
firewall rules, or antivirus exclusions.

## Reporting a vulnerability

Please report suspected vulnerabilities privately via the repository's **Security Advisories**
("Report a vulnerability") rather than a public issue. Include:

- a description and impact assessment,
- clear reproduction steps,
- affected version/commit, and
- any suggested remediation.

We aim to acknowledge reports promptly, keep you updated, credit reporters who wish to be
credited, and coordinate disclosure once a fix is available. Please do not exploit a finding
beyond what is necessary to demonstrate it, and do not access or modify others' data.

A passing safety scan reduces risk but is **not** a guarantee of absolute safety; always review
mods and extensions from sources you trust.
