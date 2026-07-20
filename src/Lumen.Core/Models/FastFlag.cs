using System.Globalization;
using Lumen.Core.Common;

namespace Lumen.Core.Models;

public enum FastFlagValueType
{
    Bool,
    Int,
    String,
}

/// <summary>
/// A single allowlisted FastFlag. Only safe, cosmetic/performance flags are ever defined — nothing
/// that affects anti-cheat, moderation, networking fairness, or player/object visibility.
/// </summary>
public sealed record FastFlagDefinition(
    string Name,
    FastFlagValueType Type,
    string Description,
    string DefaultValue,
    long Min = long.MinValue,
    long Max = long.MaxValue,
    bool IsExperimental = false)
{
    /// <summary>Validates and normalizes a user value to the exact string Roblox expects, or fails.</summary>
    public Result<string> Normalize(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        switch (Type)
        {
            case FastFlagValueType.Bool:
                if (bool.TryParse(trimmed, out var boolean))
                {
                    return Result.Success(boolean ? "True" : "False");
                }

                return Result.Failure<string>($"{Name} expects true or false.");

            case FastFlagValueType.Int:
                if (!long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                {
                    return Result.Failure<string>($"{Name} expects a whole number.");
                }

                if (number < Min || number > Max)
                {
                    return Result.Failure<string>($"{Name} must be between {Min} and {Max}.");
                }

                return Result.Success(number.ToString(CultureInfo.InvariantCulture));

            default:
                if (trimmed.Length > 256 || trimmed.Any(char.IsControl))
                {
                    return Result.Failure<string>($"{Name} value is invalid.");
                }

                return Result.Success(trimmed);
        }
    }
}
