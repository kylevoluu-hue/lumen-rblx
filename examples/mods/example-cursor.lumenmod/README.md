# Example Cursor

A minimal, safe example of the `.lumenmod` package format (see
[docs/MOD_PACKAGE_FORMAT.md](../../../docs/MOD_PACKAGE_FORMAT.md)).

- `manifest.json` — declarative metadata only (no executable content).
- `Cursors/pointer.svg` — a safe vector asset.

To distribute this as a package, zip the contents of this folder (with `manifest.json` at the
root) and give it a `.lumenmod` extension. Lumen extracts it with zip-slip/decompression-bomb
guards, validates the manifest, safety-scans every file, and verifies any declared hashes before
trusting it.

This package contains no executables, scripts, network code, or hidden files, so it passes the
safety scanner. A passing scan reduces risk but is not a guarantee of absolute safety.
