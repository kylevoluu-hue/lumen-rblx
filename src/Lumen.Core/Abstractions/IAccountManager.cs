using Lumen.Core.Common;
using Lumen.Core.Models;

namespace Lumen.Core.Abstractions;

/// <summary>
/// Manages local account metadata (nicknames, usernames, ids, avatars) and exposes whether
/// interactive sign-in is possible through a supported Roblox mechanism. It never handles
/// passwords, cookies, or session tokens.
/// </summary>
public interface IAccountManager
{
    IReadOnlyList<Account> Accounts { get; }

    /// <summary>Whether official, supported interactive sign-in / switching is available.</summary>
    AccountAuthenticationAvailability AuthenticationAvailability { get; }

    Task<Result> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a local account entry (metadata only).</summary>
    Task<Result<Account>> AddAsync(string nickname, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(Account account, CancellationToken cancellationToken = default);

    /// <summary>Removes an account and all of its local data (metadata + any vaulted material).</summary>
    Task<Result> RemoveAsync(string accountId, CancellationToken cancellationToken = default);

    Task<Result> RemoveAllAsync(CancellationToken cancellationToken = default);
}
