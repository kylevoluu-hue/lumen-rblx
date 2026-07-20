using Lumen.Core.Common;
using Lumen.Core.Models;

namespace Lumen.Core.Abstractions;

/// <summary>
/// Applies a validated cosmetic mod package into the Roblox <c>content</c> folder (mirroring the
/// package's <c>Content/</c> subtree), backing up any original files it replaces so they can be
/// restored. It only ever copies files that passed the safety scan and stay within the content
/// folder; it never touches protected Roblox binaries or writes outside the content tree.
/// </summary>
public interface IModApplicator
{
    /// <summary>Applies the package. Returns the number of files written.</summary>
    Task<Result<int>> ApplyAsync(ModPackage package, CancellationToken cancellationToken = default);

    /// <summary>Restores every original file Lumen previously replaced, and clears the applied set.</summary>
    Task<Result> RestoreAsync(CancellationToken cancellationToken = default);
}
