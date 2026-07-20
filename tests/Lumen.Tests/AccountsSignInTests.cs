using Lumen.Accounts;
using Lumen.Storage;
using Lumen.UI.ViewModels.Pages;
using Xunit;

namespace Lumen.Tests;

public class AccountsSignInTests
{
    [Fact]
    public void SignIn_opens_official_login_page_in_browser()
    {
        using var temp = new TempDirectory();
        var launcher = new FakeProcessLauncher();
        var accounts = new AccountManager(new JsonFileStore(), temp.AsLumenPaths(), new UnavailableCredentialStore());
        var viewModel = new AccountsViewModel(accounts, launcher);

        viewModel.SignInCommand.Execute(null);

        Assert.Equal(AccountsViewModel.RobloxLoginUrl, launcher.LastUrl);
    }
}
