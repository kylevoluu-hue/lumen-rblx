# Examples

Reference examples that conform to Lumen's safe, non-executable formats.

## `themes/aurora.lumentheme`

A non-executable theme document (see `Lumen.Themes.ThemeManifest`). Themes only describe
cosmetics (colours, fonts, spacing, radii, backgrounds). They cannot run code, make network
requests, read files outside their folder, or touch account/Roblox data.

## `mods/example-cursor.lumenmod/`

A conforming cosmetic mod package (see [../docs/MOD_PACKAGE_FORMAT.md](../docs/MOD_PACKAGE_FORMAT.md)).
Contains a manifest and a safe vector asset — no executables, scripts, or hidden files. Zip its
contents (manifest at the root) and rename to `.lumenmod` to distribute.

## `extensions/hello-widget/manifest.json`

An example **launcher extension** manifest (see `Lumen.Extensions.ExtensionManifest`). Extensions
are strictly separate from cosmetic mods, declare least-privilege permissions that are shown to
the user before installation, and — once the extension host lands (Phase 8) — run sandboxed with
crash isolation and a Safe Mode that disables all third-party extensions. Extensions may never
inject into Roblox, access cookies/tokens, install drivers, or run arbitrary commands.

> These examples are intentionally minimal and are safe to inspect. A passing safety scan reduces
> risk but is not a guarantee of absolute safety — install content only from sources you trust.
