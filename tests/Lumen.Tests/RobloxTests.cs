using Lumen.Core.Models;
using Lumen.Roblox;
using Xunit;

namespace Lumen.Tests;

public class ExperienceLinkValidatorTests
{
    private readonly ExperienceLinkValidator _validator = new();

    [Fact]
    public void Accepts_bare_place_id()
    {
        var result = _validator.Validate("1818");
        Assert.True(result.IsSuccess);
        Assert.Equal(ExperienceLinkKind.PlaceId, result.Value!.Kind);
        Assert.Equal(1818, result.Value!.PlaceId);
    }

    [Fact]
    public void Accepts_experience_url()
    {
        var result = _validator.Validate("https://www.roblox.com/games/920587237/Adopt-Me");
        Assert.True(result.IsSuccess);
        Assert.Equal(920587237, result.Value!.PlaceId);
    }

    [Fact]
    public void Captures_private_code_but_never_exposes_it_in_text()
    {
        var result = _validator.Validate("https://www.roblox.com/games/123?privateServerLinkCode=abc123XYZ");
        Assert.True(result.IsSuccess);
        Assert.Equal("abc123XYZ", result.Value!.PrivateServerCode);
        Assert.DoesNotContain("abc123XYZ", result.Value!.ToString());
        Assert.DoesNotContain("abc123XYZ", result.Value!.SafeDescription);
    }

    [Theory]
    [InlineData("https://evil.example.com/games/1")]
    [InlineData("http://www.roblox.com/games/1")]
    [InlineData("just some text")]
    [InlineData("")]
    public void Rejects_invalid_input(string input) =>
        Assert.True(_validator.Validate(input).IsFailure);
}

public class RobloxDeepLinkTests
{
    [Fact]
    public void Builds_place_url()
    {
        var target = new ExperienceTarget { Kind = ExperienceLinkKind.PlaceId, PlaceId = 55 };
        var result = RobloxDeepLink.Build(target);
        Assert.True(result.IsSuccess);
        Assert.Equal("https://www.roblox.com/games/55", result.Value);
    }

    [Fact]
    public void Builds_private_server_url_with_code()
    {
        var target = new ExperienceTarget
        {
            Kind = ExperienceLinkKind.PrivateServer,
            PlaceId = 55,
            PrivateServerCode = "code-1",
        };
        var result = RobloxDeepLink.Build(target);
        Assert.True(result.IsSuccess);
        Assert.Contains("privateServerLinkCode=code-1", result.Value!);
    }

    [Fact]
    public void Fails_without_place_id()
    {
        var target = new ExperienceTarget { Kind = ExperienceLinkKind.PlaceId };
        Assert.True(RobloxDeepLink.Build(target).IsFailure);
    }
}

public class RobloxInstallationLocatorTests
{
    [Fact]
    public async Task Reports_platform_unsupported_off_windows()
    {
        if (OperatingSystem.IsWindows())
        {
            return; // This assertion only holds on non-Windows; the CI host here is Linux.
        }

        var locator = new RobloxInstallationLocator();
        var installation = await locator.LocateAsync(RobloxProduct.Player);

        Assert.Equal(RobloxInstallationState.PlatformUnsupported, installation.State);
    }
}
