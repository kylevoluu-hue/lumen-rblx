# Authentication Feasibility

## Summary

**Secure, interactive Roblox sign-in and account switching cannot be implemented today through
an officially supported mechanism**, so Lumen builds the full account UI and service interfaces
but marks interactive sign-in **Unavailable** and explains why. Lumen will never use the insecure
workarounds that other tools rely on.

## Why account switching is hard (and why Lumen won't fake it)

Roblox does not provide a public, sanctioned authentication API (such as OAuth for arbitrary
third-party launchers) that lets an independent application log a user in or switch between
accounts. In practice, launchers that offer "account switching" do it by importing and replaying
the **`.ROBLOSECURITY`** cookie — the session secret for an account.

Lumen will not do this. Importing, storing, or replaying that cookie:

- requires handling a full account-session secret, which is exactly the kind of credential theft
  Lumen exists to avoid;
- is easy to abuse for account theft and violates the spirit (and often the terms) of the
  platform;
- puts users one leak away from losing their account.

Therefore Lumen treats the following as permanently prohibited: importing `.ROBLOSECURITY`,
pasting cookies or session tokens, exporting browser cookies, recreating the Roblox password
form, or asking for a Roblox password.

## What Lumen does instead

- **Local account labels.** You can add nicknames and (public) metadata to organize accounts.
  This stores no secrets.
- **Launch hand-off.** Lumen opens the official Roblox web link for an experience and hands off
  to the Roblox client you are **already signed in to** in your browser/app. Lumen never
  constructs authenticated launch tickets and never touches your session.
- **Secure storage abstraction, ready for an official method.** `ISecureCredentialStore` is
  implemented with Windows DPAPI (current-user scope) for any material a *supported* flow would be
  permitted to persist, with an explicit "unavailable" implementation elsewhere and **no insecure
  fallback**. If Roblox introduces an official, safe authentication method (e.g. a supported OAuth
  flow opened in the user's default browser), Lumen can adopt it behind these existing interfaces
  without changing the rest of the app.

## Design consequence

The `IAccountManager.AuthenticationAvailability` property returns an "unavailable" result with a
plain-language explanation, which the Accounts page shows the user instead of a login form. This
is the honest, secure default the specification requires: implement the interface and service
abstraction, and clearly mark the functionality unavailable — never fake working account
switching by importing cookies.
