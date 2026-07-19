# Contributing to Lumen

Thanks for your interest in Lumen! Contributions are welcome. Please read this guide and the
[Code of Conduct](CODE_OF_CONDUCT.md) first.

## Ground rules

Lumen is a **safe, legitimate launcher and cosmetic configuration manager**. Contributions must
never add — and pull requests will be rejected if they add:

- injection, memory access, script execution, or anti-cheat/moderation bypasses;
- ESP, aimbot, hitbox/reach changes, wallhacks, automation, or any competitive-cheat capability;
- cookie/token/password handling, or any credential importing;
- hidden telemetry, undisclosed network requests, or silent persistence.

Security must never be weakened to make a feature "work". If a feature cannot be built safely,
document the limitation and leave it disabled (see
[docs/FEATURE_FEASIBILITY.md](docs/FEATURE_FEASIBILITY.md)).

## Development setup

See [docs/BUILD.md](docs/BUILD.md). In short:

```bash
dotnet restore Lumen.sln
dotnet build Lumen.sln -c Release
dotnet test
```

## Coding standards

- Target **.NET 8**, C# with nullable reference types enabled.
- Use `async`/`await` with `CancellationToken`; keep the UI responsive; dispose resources.
- Prefer interfaces for services; keep security-sensitive logic in `Lumen.Security` and small.
- Return `Result`/`Result<T>` for expected failures; never swallow exceptions silently.
- No hardcoded secrets; never commit API keys, tokens, or `.pfx`/`.snk` files.
- Match the surrounding style; keep comments useful. Follow `.editorconfig`.

## Tests

- Add or update tests for any behaviour change, especially security-sensitive code.
- Tests must pass on a standard user account (no administrator) and run headless.
- Security-relevant PRs should include tests for the failure/rejection paths (traversal, unsafe
  archives, invalid hashes, redaction, URL validation, etc.).

## Pull requests

1. Create a feature branch.
2. Keep changes focused and modular; avoid giant single-file changes.
3. Ensure `dotnet build` and `dotnet test` are green with no new warnings.
4. Describe what changed and why, and note any security or privacy implications.

## Reporting security issues

Please report vulnerabilities privately — see [SECURITY.md](SECURITY.md). Do not open a public
issue for a security problem.
