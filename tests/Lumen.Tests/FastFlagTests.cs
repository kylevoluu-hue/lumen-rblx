using Lumen.Roblox;
using Xunit;

namespace Lumen.Tests;

public class FastFlagManagerTests
{
    private static FastFlagManager CreateManager() => new(new RobloxInstallationLocator());

    [Fact]
    public void Builds_preview_for_valid_flags()
    {
        var preview = CreateManager().BuildPreview(new Dictionary<string, string>
        {
            ["DFIntTaskSchedulerTargetFps"] = "120",
        });

        Assert.True(preview.IsSuccess);
        Assert.Contains("DFIntTaskSchedulerTargetFps", preview.Value!);
        Assert.Contains("120", preview.Value!);
    }

    [Fact]
    public void Normalizes_boolean_to_roblox_casing()
    {
        var preview = CreateManager().BuildPreview(new Dictionary<string, string>
        {
            ["FFlagDebugGraphicsPreferD3D11"] = "true",
        });

        Assert.True(preview.IsSuccess);
        Assert.Contains("True", preview.Value!);
    }

    [Fact]
    public void Rejects_unknown_flag() =>
        Assert.True(CreateManager().BuildPreview(new Dictionary<string, string> { ["NotAllowedFlag"] = "1" }).IsFailure);

    [Fact]
    public void Rejects_out_of_range_value() =>
        Assert.True(CreateManager().BuildPreview(new Dictionary<string, string> { ["DFIntTaskSchedulerTargetFps"] = "20000" }).IsFailure);

    [Fact]
    public void Rejects_non_boolean_value() =>
        Assert.True(CreateManager().BuildPreview(new Dictionary<string, string> { ["FFlagDebugGraphicsPreferD3D11"] = "yes" }).IsFailure);
}
