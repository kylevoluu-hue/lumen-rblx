using System.Net.Http;
using Lumen.Accounts;
using Lumen.Roblox;
using Lumen.Security;
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
        var web = new RobloxWebClient(new HttpClient(), UrlValidator.CreateDefault());
        var viewModel = new AccountsViewModel(accounts, launcher, web);

        viewModel.SignInCommand.Execute(null);

        Assert.Equal(AccountsViewModel.RobloxLoginUrl, launcher.LastUrl);
    }
}
