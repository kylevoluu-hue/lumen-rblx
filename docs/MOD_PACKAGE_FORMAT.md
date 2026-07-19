# Mod Package Format (`.lumenmod`)

A Lumen mod is a **cosmetic, data-only** package. It contains no executable code and cannot run
scripts, make network requests, or touch account/Roblox data. A `.lumenmod` file is a ZIP archive
with this layout:

```
ExampleMod.lumenmod
├── manifest.json        (required)
├── preview.png          (optional)
├── README.md            (optional)
├── Assets/
├── Textures/
├── Cursors/
├── Crosshairs/
├── Sounds/
├── Fonts/
├── Skyboxes/
└── OverlayLayouts/
```

## Manifest

```json
{
  "formatVersion": 1,
  "id": "author.mod-name",
  "name": "Example Mod",
  "author": "Author Name",
  "version": "1.0.0",
  "description": "A safe cosmetic mod for Lumen.",
  "category": "Cursor",
  "minimumLumenVersion": "1.0.0",
  "supportedRobloxVersions": ["current"],
  "files": [],
  "conflicts": [],
  "dependencies": [],
  "hashes": { "Cursors/pointer.png": "<sha256-hex>" }
}
```

- `formatVersion` must be `1`.
- `id` matches `^[A-Za-z0-9][A-Za-z0-9._-]{1,99}$` (e.g. `author.mod-name`).
- `version` is semantic (`MAJOR.MINOR.PATCH`, optional pre-release suffix).
- `category` is one of the `ModCategory` values (Cursor, Crosshair, Texture, Sound, Font, Skybox,
  Ui, VisualFilter, Accessibility, Overlay, Icon, LoadingScreen, Performance, Display, Theme,
  Profile).
- `hashes` maps relative file paths to their expected SHA-256; each is verified on open.
- The manifest is **declarative only** — it must not contain executable commands.

## How Lumen opens a package (`IModPackageReader`)

1. **Extract** into a disposable staging directory with zip-slip and decompression-bomb guards.
2. **Parse & validate** `manifest.json` (format version, id, name, author, version).
3. **Safety-scan** the extracted files (`ModSafetyScanner`).
4. **Verify hashes** declared in the manifest.
5. If any step fails, the staging directory is discarded and the package is rejected — never left
   partially trusted.

## Safety rules (rejected automatically)

The scanner **blocks** packages containing:

- executables/scripts/installers/shortcuts/registry files:
  `.exe .dll .bat .cmd .com .scr .pif .msi .msp .cpl .msc .ps1 .psm1 .vbs .js .jse .wsf .hta .lnk .reg .sys .jar .cab .sh .py …`;
- **double / hidden extensions** (e.g. `cursor.exe.png`);
- **path traversal** (`..`), absolute/rooted paths, drive/UNC prefixes, or ADS colons;
- **symbolic links**;
- (also enforced at extraction) encrypted archives, decompression bombs, and oversized archives.

Unknown file types are surfaced as **warnings**; embedded URLs are flagged for review. A passing
scan reduces risk but is **not** a guarantee of absolute safety — install mods only from sources
you trust.

## Verification labels

Mods are labelled **Official Lumen**, **Source Reviewed**, **Signature Verified**,
**Community Submitted**, or **Unverified**. Popularity is deliberately not a verification level,
and Lumen never installs mods automatically.

See `examples/mods/example-cursor.lumenmod/` for a conforming example.
