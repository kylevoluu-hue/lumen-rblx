using System.Text.RegularExpressions;

namespace Lumen.Security;

/// <summary>
/// Removes sensitive data from arbitrary text before it is written to a log or included in a
/// diagnostic report. Redacts OS user paths, the Roblox <c>.ROBLOSECURITY</c> cookie,
/// authorization/bearer tokens, CSRF tokens, private-server and other sensitive query codes,
/// and IP addresses. This is the single choke point every log line and diagnostic string passes
/// through so secrets can never leak to disk.
/// </summary>
public static partial class Redactor
{
    /// <summary>
    /// Returns <paramref name="input"/> with sensitive data replaced by placeholders. IP-address
    /// redaction can be disabled for a diagnostic report where the user has explicitly opted to
    /// include network information.
    /// </summary>
    public static string Redact(string? input, bool redactIpAddresses = true)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input ?? string.Empty;
        }

        var text = input;
        text = WindowsUserPathRegex().Replace(text, "${1}<user>");
        text = UnixUserPathRegex().Replace(text, "${1}<user>");
        text = WarningCookieRegex().Replace(text, "<redacted-cookie>");
        text = RobloSecurityRegex().Replace(text, "${1}<redacted>");
        text = AuthorizationRegex().Replace(text, "${1}<redacted>");
        text = BearerTokenRegex().Replace(text, "Bearer <redacted>");
        text = CsrfTokenRegex().Replace(text, "${1}<redacted>");
        text = SensitiveQueryRegex().Replace(text, "${1}<redacted>");

        if (redactIpAddresses)
        {
            text = IPv4Regex().Replace(text, "<redacted-ip>");
            text = IPv6Regex().Replace(text, "<redacted-ip>");
        }

        return text;
    }

    /// <summary>True if redaction would change the text (i.e. it contains something sensitive).</summary>
    public static bool ContainsSensitiveData(string? input) =>
        !string.Equals(Redact(input), input ?? string.Empty, StringComparison.Ordinal);

    [GeneratedRegex(@"([A-Za-z]:\\Users\\)([^\\/\r\n""';]+)", RegexOptions.IgnoreCase)]
    private static partial Regex WindowsUserPathRegex();

    [GeneratedRegex(@"(/home/|/Users/)([^/\r\n""';]+)")]
    private static partial Regex UnixUserPathRegex();

    // The ".ROBLOSECURITY" value is prefixed with a "_|WARNING:...|_" banner; match the whole token.
    [GeneratedRegex(@"_\|WARNING:[^|]*\|_[^\s""';,<]+")]
    private static partial Regex WarningCookieRegex();

    [GeneratedRegex(@"(\.?ROBLOSECURITY\s*[=:]\s*)[^\s;""'<]+", RegexOptions.IgnoreCase)]
    private static partial Regex RobloSecurityRegex();

    [GeneratedRegex(@"(authorization\s*[:=]\s*)(?:bearer\s+)?[^\s""'<]+", RegexOptions.IgnoreCase)]
    private static partial Regex AuthorizationRegex();

    [GeneratedRegex(@"\bbearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.IgnoreCase)]
    private static partial Regex BearerTokenRegex();

    [GeneratedRegex(@"(x-csrf-token\s*[:=]\s*)[^\s""'<]+", RegexOptions.IgnoreCase)]
    private static partial Regex CsrfTokenRegex();

    [GeneratedRegex(
        @"([?&](?:privateServerLinkCode|linkCode|accessCode|code|token|accessToken|authToken|password|passwd|pwd)=)[^&\s""'<]+",
        RegexOptions.IgnoreCase)]
    private static partial Regex SensitiveQueryRegex();

    [GeneratedRegex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b")]
    private static partial Regex IPv4Regex();

    // Requires at least one hex letter so decimal log timestamps (e.g. 14:00:31) are not matched.
    [GeneratedRegex(@"\b(?=[0-9A-Fa-f:]*[A-Fa-f])(?:[0-9A-Fa-f]{1,4}:){2,7}[0-9A-Fa-f]{1,4}\b")]
    private static partial Regex IPv6Regex();
}
