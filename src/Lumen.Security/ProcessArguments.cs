using System.Text;

namespace Lumen.Security;

/// <summary>
/// Helpers for launching external processes safely. The preferred path is
/// <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList"/>, which escapes each argument
/// for us. These helpers exist for the cases where a single command line must be produced, and
/// to reject values that cannot be represented safely on a command line.
///
/// Lumen never builds a shell command from user input; it launches executables directly with
/// discrete, validated arguments, so classic shell-injection is not possible.
/// </summary>
public static class ProcessArguments
{
    /// <summary>
    /// Characters that must never appear in a launch argument (they cannot be safely escaped on
    /// a Windows command line and never occur in a legitimate path or Roblox launch URI).
    /// </summary>
    public static bool HasControlCharacters(string value) =>
        value.Any(static c => c is '\0' or '\r' or '\n');

    /// <summary>
    /// Escapes a single argument per the Windows command-line parsing rules (the algorithm the
    /// CRT and <c>CommandLineToArgvW</c> use), so the receiving process sees exactly one token.
    /// </summary>
    public static string EscapeWindowsArgument(string argument)
    {
        ArgumentNullException.ThrowIfNull(argument);

        // No quoting needed when the argument is non-empty and free of whitespace and quotes.
        if (argument.Length > 0 && argument.IndexOfAny([' ', '\t', '\n', '\v', '"']) < 0)
        {
            return argument;
        }

        var builder = new StringBuilder();
        builder.Append('"');

        for (var i = 0; ; i++)
        {
            var backslashes = 0;
            while (i < argument.Length && argument[i] == '\\')
            {
                i++;
                backslashes++;
            }

            if (i == argument.Length)
            {
                // Escape all trailing backslashes so they do not escape the closing quote.
                builder.Append('\\', backslashes * 2);
                break;
            }

            if (argument[i] == '"')
            {
                // Escape the backslashes and the embedded quote.
                builder.Append('\\', (backslashes * 2) + 1);
                builder.Append('"');
            }
            else
            {
                builder.Append('\\', backslashes);
                builder.Append(argument[i]);
            }
        }

        builder.Append('"');
        return builder.ToString();
    }

    /// <summary>Joins arguments into a single safely-escaped command-line string.</summary>
    public static string JoinArguments(IEnumerable<string> arguments) =>
        string.Join(' ', arguments.Select(EscapeWindowsArgument));
}
