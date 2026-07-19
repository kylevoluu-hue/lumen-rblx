using Lumen.Core.Abstractions;
using Lumen.Core.Common;
using Lumen.Core.Configuration;
using Lumen.Core.Models;

namespace Lumen.Accounts;

/// <summary>Serializable container for locally-stored account metadata.</summary>
public sealed class AccountCollection
{
    public int SchemaVersion { get; set; } = 1;

    public List<Account> Accounts { get; set; } = new();
}

/// <summary>
/// Manages local account metadata. It deliberately handles no passwords, cookies, or session
/// tokens — <see cref="AuthenticationAvailability"/> explains why interactive sign-in is not
/// offered. Removing an account also removes any material the OS credential vault holds for it.
/// </summary>
public sealed class AccountManager : IAccountManager, IDisposable
{
    private readonly IVersionedJsonStore _store;
    private readonly ILumenPaths _paths;
    private readonly ISecureCredentialStore _credentials;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private AccountCollection _collection = new();

    public AccountManager(IVersionedJsonStore store, ILumenPaths paths, ISecureCredentialStore credentials)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
    }

    public IReadOnlyList<Account> Accounts => _collection.Accounts;

    public AccountAuthenticationAvailability AuthenticationAvailability { get; } =
        AccountAuthenticationAvailability.Unavailable(
            "Roblox does not currently provide a supported way for an independent launcher to sign in " +
            "or switch accounts without importing the .ROBLOSECURITY cookie — which Lumen will never do. " +
            "You can add local account labels here for organisation, but launching hands off to the " +
            "Roblox client you are already signed in to. If Roblox adds an official method, Lumen will adopt it.");

    private string FilePath => Path.Combine(_paths.Accounts, "accounts.json");

    public async Task<Result> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var result = await _store.LoadAsync(FilePath, static () => new AccountCollection(), cancellationToken)
                .ConfigureAwait(false);
            if (result.IsFailure)
            {
                return Result.Failure(result.Error!);
            }

            _collection = result.Value!;
            return Result.Success();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Result<Account>> AddAsync(string nickname, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            return Result.Failure<Account>("Enter a nickname for the account.");
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var account = new Account
            {
                Id = Guid.NewGuid().ToString("N"),
                Nickname = nickname.Trim(),
            };
            _collection.Accounts.Add(account);

            var saved = await SaveInternalAsync(cancellationToken).ConfigureAwait(false);
            return saved.IsSuccess ? Result.Success(account) : Result.Failure<Account>(saved.Error!);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Result> UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var index = _collection.Accounts.FindIndex(a => a.Id == account.Id);
            if (index < 0)
            {
                return Result.Failure("Account not found.");
            }

            _collection.Accounts[index] = account;
            return await SaveInternalAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Result> RemoveAsync(string accountId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var removed = _collection.Accounts.RemoveAll(a => a.Id == accountId) > 0;
            if (!removed)
            {
                return Result.Failure("Account not found.");
            }

            // Remove any vaulted material tied to this account (best-effort; there may be none).
            await _credentials.RemoveAsync(accountId, cancellationToken).ConfigureAwait(false);

            return await SaveInternalAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Result> RemoveAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            foreach (var account in _collection.Accounts)
            {
                await _credentials.RemoveAsync(account.Id, cancellationToken).ConfigureAwait(false);
            }

            _collection.Accounts.Clear();
            return await SaveInternalAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private Task<Result> SaveInternalAsync(CancellationToken cancellationToken) =>
        _store.SaveAsync(FilePath, _collection, cancellationToken);

    public void Dispose() => _gate.Dispose();
}
