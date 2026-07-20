using System.Linq;
using Lumen.UI.Navigation;
using Lumen.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lumen.WinUI;

/// <summary>
/// Hosts the shared <see cref="ShellViewModel"/> in a WinUI NavigationView. Navigation entries and
/// their page view-models are the same instances the Avalonia head uses. Per-page WinUI views are
/// the next porting step; the content area currently shows the active page's title/description.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly ShellViewModel _shell;

    public MainWindow(ShellViewModel shell)
    {
        _shell = shell;
        InitializeComponent();
        Title = "Lumen Launcher";

        foreach (var entry in _shell.Entries)
        {
            Nav.MenuItems.Add(new NavigationViewItem { Content = entry.Title, Tag = entry });
        }

        Nav.SelectedItem = Nav.MenuItems.FirstOrDefault();
        ShowPage(_shell.SelectedEntry);
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem { Tag: NavigationEntry entry })
        {
            _shell.SelectedEntry = entry;
            ShowPage(entry);
        }
    }

    private void ShowPage(NavigationEntry? entry)
    {
        PageTitle.Text = entry?.Page.Title ?? "Lumen";
        PageDescription.Text = entry?.Page.Description ?? string.Empty;
    }
}
