using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Lumen.Core.Common;
using Lumen.UI.Navigation;

namespace Lumen.UI.ViewModels;

/// <summary>
/// The application shell: owns the navigation entries and the currently displayed page. Navigation
/// layout can be a sidebar (default) or top bar. Entries are supplied by composition (the host),
/// keeping the UI project free of any concrete service dependencies.
/// </summary>
public sealed partial class ShellViewModel : ViewModelBase
{
    [ObservableProperty]
    private NavigationEntry? _selectedEntry;

    [ObservableProperty]
    private PageViewModel? _currentPage;

    [ObservableProperty]
    private bool _isSidebar = true;

    public ShellViewModel(IEnumerable<NavigationEntry> entries, INavigationService navigation)
    {
        Entries = new ObservableCollection<NavigationEntry>(entries);
        SelectedEntry = Entries.FirstOrDefault();

        navigation.NavigationRequested += OnNavigationRequested;
    }

    private void OnNavigationRequested(string pageId)
    {
        var entry = Entries.FirstOrDefault(e => e.Id == pageId);
        if (entry is not null)
        {
            SelectedEntry = entry;
        }
    }

    public ObservableCollection<NavigationEntry> Entries { get; }

    public string Title => $"{LumenInfo.Name}";

    public string VersionLabel => $"v{LumenInfo.Version}";

    /// <summary>Mandatory independence disclaimer, always visible in the shell.</summary>
    public string Disclaimer => LumenInfo.Disclaimer;

    partial void OnSelectedEntryChanged(NavigationEntry? value)
    {
        CurrentPage = value?.Page;
        if (value?.Page is { } page)
        {
            // Fire-and-forget: pages handle their own errors and never throw from InitializeAsync.
            _ = page.InitializeAsync();
        }
    }
}
