namespace Lumen.Core.Models;

/// <summary>
/// Describes whether interactive Roblox sign-in / account switching is available through an
/// officially supported mechanism, and — when it is not — an honest explanation the UI shows
/// the user instead of a fake login form.
/// </summary>
public sealed record AccountAuthenticationAvailability(bool IsAvailable, string Explanation)
{
    public static AccountAuthenticationAvailability Unavailable(string explanation) =>
        new(false, explanation);

    public static AccountAuthenticationAvailability Available() =>
        new(true, "Sign-in is available through the official Roblox login page in your browser.");
}
