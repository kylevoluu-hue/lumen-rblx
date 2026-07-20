using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lumen.Core.Abstractions;

namespace Lumen.UI.ViewModels.Pages;

/// <summary>A friend card. Avatar loads asynchronously; the name shows immediately.</summary>
public sealed partial class FriendItemViewModel : ObservableObject
{
    public FriendItemViewModel(string displayName, string handle)
    {
        DisplayName = displayName;
        Handle = handle;
    }

    public string DisplayName { get; }

    public string Handle { get; }

    public string Initial => string.IsNullOrEmpty(DisplayName) ? "?" : DisplayName[..1].ToUpperInvariant();

    [ObservableProperty]
    private Bitmap? _avatar;
}

/// <summary>
/// Shows the linked account's PUBLIC Roblox profile and friends, rendered in Lumen's own theme
/// (not a copy of Roblox's look). Uses only public official APIs — no cookies, no private data.
/// </summary>
public sealed partial class SocialViewModel : PageViewModel
{
    private readonly IAccountManager _accounts;
    private readonly IRobloxWebClient _web;

    [ObservableProperty]
    private bool _hasLinkedAccount;

    [ObservableProperty]
    private string _profileName = "Not linked";

    [ObservableProperty]
    private string _profileHandle = string.Empty;

    [ObservableProperty]
    private string? _profileDescription;

    [ObservableProperty]
    private Bitmap? _profileAvatar;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public SocialViewModel(IAccountManager accounts, IRobloxWebClient web)
    {
        _accounts = accounts;
        _web = web;
    }

    public override string Title => "Social";

    public override string Description => "Your public Roblox profile and friends — shown in Lumen's style.";

    public ObservableCollection<FriendItemViewModel> Friends { get; } = new();

    public override Task InitializeAsync() => RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await _accounts.LoadAsync().ConfigureAwait(true);
        var account = _accounts.Accounts.FirstOrDefault(a => a.UserId is > 0);
        if (account?.UserId is not > 0)
        {
            HasLinkedAccount = false;
            StatusMessage = "Link a Roblox account on the Accounts page to see your profile and friends here.";
            return;
        }

        HasLinkedAccount = true;
        StatusMessage = "Loading…";
        var userId = account.UserId.Value;

        var user = await _web.GetUserAsync(userId).ConfigureAwait(true);
        if (user.IsSuccess)
        {
            ProfileName = user.Value!.DisplayName;
            ProfileHandle = "@" + user.Value!.Username;
            ProfileDescription = user.Value!.Description;
        }

        var avatar = await _web.GetAvatarHeadshotUrlAsync(userId).ConfigureAwait(true);
        if (avatar.IsSuccess)
        {
            ProfileAvatar = await LoadBitmapAsync(avatar.Value!).ConfigureAwait(true);
        }

        Friends.Clear();
        var friends = await _web.GetFriendsAsync(userId).ConfigureAwait(true);
        if (friends.IsFailure)
        {
            StatusMessage = friends.Error!;
            return;
        }

        foreach (var friend in friends.Value!.Take(36))
        {
            var item = new FriendItemViewModel(friend.DisplayName, "@" + friend.Username);
            Friends.Add(item);
            _ = LoadFriendAvatarAsync(item, friend.UserId);
        }

        StatusMessage = Friends.Count == 0 ? "No public friends to show." : $"{Friends.Count} friend(s).";
    }

    private async Task LoadFriendAvatarAsync(FriendItemViewModel item, long userId)
    {
        var url = await _web.GetAvatarHeadshotUrlAsync(userId).ConfigureAwait(true);
        if (url.IsSuccess)
        {
            item.Avatar = await LoadBitmapAsync(url.Value!).ConfigureAwait(true);
        }
    }

    private async Task<Bitmap?> LoadBitmapAsync(string url)
    {
        var bytes = await _web.GetImageAsync(url).ConfigureAwait(true);
        if (bytes.IsFailure)
        {
            return null;
        }

        try
        {
            using var stream = new MemoryStream(bytes.Value!);
            return new Bitmap(stream);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
