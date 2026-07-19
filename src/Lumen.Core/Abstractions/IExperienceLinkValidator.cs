using Lumen.Core.Common;
using Lumen.Core.Models;

namespace Lumen.Core.Abstractions;

/// <summary>
/// Validates and parses user-supplied experience references (a numeric place id, an official
/// Roblox experience URL, or a supported private-server link) into a safe
/// <see cref="ExperienceTarget"/>. Rejects malformed input and anything that could be used to
/// inject shell arguments. Private-server codes are captured but never logged.
/// </summary>
public interface IExperienceLinkValidator
{
    Result<ExperienceTarget> Validate(string input);
}
