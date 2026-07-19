using Lumen.Core.Common;
using Lumen.UI.Navigation;
using Lumen.UI.ViewModels;
using Lumen.UI.ViewModels.Pages;
using Xunit;

namespace Lumen.Tests;

public class ShellViewModelTests
{
    private static ShellViewModel CreateShell()
    {
        var entries = new List<NavigationEntry>
        {
            new("home", "Home", "H", new PlaceholderPageViewModel("Home", "desc", "note")),
            new("about", "About", "A", new PlaceholderPageViewModel("About", "desc", "note")),
        };
        return new ShellViewModel(entries);
    }

    [Fact]
    public void Selects_first_page_on_construction()
    {
        var shell = CreateShell();
        Assert.NotNull(shell.CurrentPage);
        Assert.Equal("Home", shell.CurrentPage!.Title);
    }

    [Fact]
    public void Navigation_changes_current_page()
    {
        var shell = CreateShell();
        shell.SelectedEntry = shell.Entries[1];
        Assert.Equal("About", shell.CurrentPage!.Title);
    }

    [Fact]
    public void Shell_exposes_mandatory_disclaimer() =>
        Assert.Equal(LumenInfo.Disclaimer, CreateShell().Disclaimer);
}
