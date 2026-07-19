# Threat Model

This document identifies what Lumen protects, the boundaries where untrusted data enters, the
threats considered, and the mitigations in place or planned.

## Assets to protect

1. **The user's device** — from malicious mods/extensions/packages that could execute code or
   escape their intended folder.
2. **The user's Roblox account** — Lumen never handles credentials, so the strongest protection is
   to never collect, store, log, or transmit passwords, cookies, or tokens.
3. **The user's privacy** — local data (settings, account labels, logs) must not leak via logs,
   diagnostics, exports, or undisclosed network requests.
4. **Integrity of Lumen itself** — updates and its own files must not be tampered with.

## Trust boundaries (where untrusted input enters)

| Boundary | Untrusted input | Primary risks |
| --- | --- | --- |
| Mod/theme/extension packages | ZIP archives, manifests, assets | Zip-slip, decompression bombs, executables, path traversal, malicious hashes |
| Experience links | Place ids, URLs, private-server links | Malformed input, argument injection, leaking private codes |
| Configuration/profile files | JSON on disk | Corruption, injection of unexpected values |
| Network | Update metadata, avatar images, downloads | MITM, undisclosed destinations, tampered payloads |
| Logs/diagnostics | Any string that reaches them | Secret leakage (cookies, tokens, IPs, paths) |

## Threats and mitigations (STRIDE-oriented)

**Tampering / elevation via packages**
- Zip-slip → `PathSafety` + `SafeArchiveExtractor` confine every entry to the destination.
- Decompression bombs → per-entry, total-size, ratio, and count caps enforced while streaming.
- Executable/script content → `ModSafetyScanner` blocks `.exe/.dll/.bat/.ps1/.reg/…`, double and
  hidden extensions, symlinks, and traversal.
- Tampered files → SHA-256 hashes in the manifest are verified before a package is trusted.
- *(Planned)* digital-signature verification and extension sandboxing/permission enforcement.

**Information disclosure**
- Secrets in logs/diagnostics → `Redactor` strips the `.ROBLOSECURITY` cookie,
  authorization/bearer/CSRF tokens, private-server codes, IPs, and user paths; diagnostics exclude
  device identifiers and are previewed, never auto-uploaded.
- Private-server codes → carried in a non-serialized field, excluded from `ToString`, logs, and
  the deep link's log-safe description.
- Credentials at rest → only permitted material is stored, via DPAPI; never in plain-text JSON;
  never exported in profiles (account linkage is stripped on export).

**Spoofing / command injection**
- Malicious URLs → `UrlValidator` enforces HTTPS + host allowlist and rejects embedded creds.
- Argument injection → processes are launched with discrete, validated, escaped arguments
  (`ProcessArguments`); no shell command is ever built from user input.

**Denial of service / corruption**
- Corrupt config → quarantined and recovered to defaults (`JsonFileStore`); atomic writes prevent
  half-written files.

**Repudiation / silent behaviour**
- Least privilege (`asInvoker`); no silent services, drivers, tasks, or startup entries.
- *(Planned)* Network Activity Viewer surfaces every request's destination and purpose.

## Explicitly out of scope (by design)

Lumen does **not** attempt any capability that would require reading Roblox memory, injecting into
Roblox, importing account cookies/tokens, or bypassing anti-cheat/moderation. These are not
"unimplemented" — they are permanently excluded. Features that would need them are left disabled
with an explanation (see [FEATURE_FEASIBILITY.md](FEATURE_FEASIBILITY.md)).

## Residual risk

A passing safety scan reduces risk but does not guarantee absolute safety; users should install
only mods/extensions from sources they trust. Signature verification and the extension sandbox
(planned) will further reduce risk for third-party content.
