using System.Text;
using Lumen.Security;
using Xunit;

namespace Lumen.Tests;

public class PathSafetyTests
{
    [Theory]
    [InlineData("assets/cursor.png")]
    [InlineData("Textures/ui/button.webp")]
    [InlineData("manifest.json")]
    public void Accepts_safe_relative_paths(string path) =>
        Assert.True(PathSafety.IsSafeRelativePath(path));

    [Theory]
    [InlineData("../evil.txt")]
    [InlineData("a/../../b")]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\Windows\\system32")]
    [InlineData("folder\\..\\..\\x")]
    [InlineData("stream:name")]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_unsafe_relative_paths(string path) =>
        Assert.False(PathSafety.IsSafeRelativePath(path));

    [Fact]
    public void ResolveWithinRoot_keeps_path_inside_root()
    {
        using var temp = new TempDirectory();
        var result = PathSafety.ResolveWithinRoot(temp.Path, "sub/file.png");
        Assert.True(result.IsSuccess);
        Assert.StartsWith(temp.Path, result.Value!);
    }

    [Fact]
    public void ResolveWithinRoot_rejects_escape()
    {
        using var temp = new TempDirectory();
        var result = PathSafety.ResolveWithinRoot(temp.Path, "../escape.png");
        Assert.True(result.IsFailure);
    }
}

public class FileHashingTests
{
    [Fact]
    public void Sha256_matches_known_vector() =>
        Assert.Equal(
            "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
            FileHashing.ComputeSha256(Encoding.ASCII.GetBytes("abc")));

    [Fact]
    public void HashesEqual_is_case_insensitive() =>
        Assert.True(FileHashing.HashesEqual("ABCDEF", "abcdef"));

    [Fact]
    public void HashesEqual_rejects_mismatch_and_length() =>
        Assert.False(FileHashing.HashesEqual("abcd", "abce"));

    [Fact]
    public async Task VerifyFileAsync_detects_match_and_mismatch()
    {
        using var temp = new TempDirectory();
        var file = temp.Sub("data.bin");
        await File.WriteAllTextAsync(file, "abc");

        var good = await FileHashing.VerifyFileAsync(
            file, "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
        Assert.True(good.IsSuccess);

        var bad = await FileHashing.VerifyFileAsync(file, new string('0', 64));
        Assert.True(bad.IsFailure);
    }
}

public class UrlValidatorTests
{
    private readonly UrlValidator _validator = UrlValidator.CreateDefault();

    [Theory]
    [InlineData("https://www.roblox.com/games/1")]
    [InlineData("https://api.github.com/repos/x/y/releases")]
    public void Allows_https_allowlisted_hosts(string url) =>
        Assert.True(_validator.IsAllowed(url));

    [Theory]
    [InlineData("http://www.roblox.com")]          // not https
    [InlineData("https://evil.example.com")]        // not allowlisted
    [InlineData("https://user:pass@roblox.com")]    // embedded credentials
    [InlineData("not a url")]
    [InlineData("ftp://roblox.com")]
    public void Rejects_disallowed_urls(string url) =>
        Assert.False(_validator.IsAllowed(url));
}

public class ProcessArgumentsTests
{
    [Fact]
    public void Simple_argument_is_not_quoted() =>
        Assert.Equal("simple", ProcessArguments.EscapeWindowsArgument("simple"));

    [Fact]
    public void Argument_with_space_is_quoted() =>
        Assert.Equal("\"with space\"", ProcessArguments.EscapeWindowsArgument("with space"));

    [Fact]
    public void Embedded_quote_is_escaped()
    {
        var escaped = ProcessArguments.EscapeWindowsArgument("a\"b");
        Assert.Contains("\\\"", escaped);
    }

    [Theory]
    [InlineData("normal", false)]
    [InlineData("has\nnewline", true)]
    [InlineData("has\0null", true)]
    public void Detects_control_characters(string value, bool expected) =>
        Assert.Equal(expected, ProcessArguments.HasControlCharacters(value));
}

public class RedactorTests
{
    [Fact]
    public void Redacts_roblosecurity_cookie()
    {
        var result = Redactor.Redact(".ROBLOSECURITY=SECRETVALUE123");
        Assert.DoesNotContain("SECRETVALUE123", result);
        Assert.Contains("<redacted>", result);
    }

    [Fact]
    public void Redacts_warning_prefixed_cookie()
    {
        var input = "cookie: _|WARNING:-DO-NOT-SHARE-THIS.|_ABC123DEF456";
        var result = Redactor.Redact(input);
        Assert.DoesNotContain("ABC123DEF456", result);
    }

    [Fact]
    public void Redacts_authorization_header()
    {
        var result = Redactor.Redact("Authorization: Bearer eyJhbGciOiExampleTokenValue");
        Assert.DoesNotContain("ExampleTokenValue", result);
    }

    [Fact]
    public void Redacts_private_server_code()
    {
        var result = Redactor.Redact("https://www.roblox.com/games/123?privateServerLinkCode=SUPERSECRET");
        Assert.DoesNotContain("SUPERSECRET", result);
    }

    [Fact]
    public void Redacts_ipv4_and_windows_user_path()
    {
        Assert.DoesNotContain("192.168.1.50", Redactor.Redact("client 192.168.1.50 connected"));
        Assert.DoesNotContain("Alice", Redactor.Redact(@"C:\Users\Alice\AppData\Lumen"));
    }

    [Fact]
    public void Does_not_redact_time_or_plain_text()
    {
        Assert.Equal("event at 14:00:31", Redactor.Redact("event at 14:00:31"));
        Assert.False(Redactor.ContainsSensitiveData("hello world, this is fine"));
    }
}
