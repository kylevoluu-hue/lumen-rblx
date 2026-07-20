using Lumen.Core.Abstractions;
using Lumen.Core.Models;
using Lumen.Launching;
using Lumen.Roblox;
using Xunit;

namespace Lumen.Tests;

public class RobloxDeepLinkLaunchTests
{
    [Fact]
    public void Place_id_builds_app_deep_link()
    {
        var target = new ExperienceTarget { Kind = ExperienceLinkKind.PlaceId, PlaceId = 55 };
        var result = RobloxDeepLink.BuildLaunchLink(target);
        Assert.True(result.IsSuccess);
        Assert.Equal("roblox://experiences/start?placeId=55", result.Value);
    }

    [Fact]
    public void Private_server_falls_back_to_web_link_with_code()
    {
        var target = new ExperienceTarget
        {
            Kind = ExperienceLinkKind.PrivateServer,
            PlaceId = 55,
            PrivateServerCode = "code-1",
        };
        var result = RobloxDeepLink.BuildLaunchLink(target);
        Assert.True(result.IsSuccess);
        Assert.StartsWith("https://www.roblox.com/games/55", result.Value!);
        Assert.Contains("privateServerLinkCode=code-1", result.Value!);
    }

    [Fact]
    public void Missing_place_id_fails() =>
        Assert.True(RobloxDeepLink.BuildLaunchLink(new ExperienceTarget { Kind = ExperienceLinkKind.PlaceId }).IsFailure);
}

public class LaunchServiceTests
{
    [Fact]
    public async Task Launches_valid_target_via_launcher()
    {
        var launcher = new FakeProcessLauncher();
        var service = new LaunchService(launcher);
        var target = new ExperienceTarget { Kind = ExperienceLinkKind.PlaceId, PlaceId = 1818 };

        var result = await service.LaunchAsync(target);

        Assert.True(result.IsSuccess);
        Assert.Equal("roblox://experiences/start?placeId=1818", launcher.LastUrl);
    }

    [Fact]
    public async Task Reports_launch_phases()
    {
        var progress = new SyncProgress<LaunchPhase>();
        var service = new LaunchService(new FakeProcessLauncher());
        var target = new ExperienceTarget { Kind = ExperienceLinkKind.PlaceId, PlaceId = 1 };

        await service.LaunchAsync(target, progress);

        Assert.Contains(LaunchPhase.LaunchingRoblox, progress.Items);
        Assert.Contains(LaunchPhase.Complete, progress.Items);
    }

    [Fact]
    public async Task Surfaces_launcher_failure()
    {
        var launcher = new FakeProcessLauncher { Succeed = false };
        var service = new LaunchService(launcher);
        var target = new ExperienceTarget { Kind = ExperienceLinkKind.PlaceId, PlaceId = 1 };

        var result = await service.LaunchAsync(target);

        Assert.True(result.IsFailure);
    }
}
