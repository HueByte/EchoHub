using EchoHub.Client.UI.Helpers;
using Xunit;

namespace EchoHub.Tests;

public class DroppedFileParserTests
{
    // ── LooksLikePath ─────────────────────────────────────────────────

    [Theory]
    [InlineData("C:\\Users\\me\\cat.png")]
    [InlineData("D:/photos/pic.jpg")]
    [InlineData("\"C:\\My Files\\a b.png\"")]
    [InlineData("/home/me/song.mp3")]
    [InlineData("\\\\server\\share\\file.txt")]
    public void LooksLikePath_PathLikeInput_ReturnsTrue(string text)
    {
        Assert.True(DroppedFileParser.LooksLikePath(text));
    }

    [Theory]
    [InlineData("hello world")]
    [InlineData("check out my cat")]
    [InlineData("no")]
    [InlineData("")]
    [InlineData("@someone hi")]
    public void LooksLikePath_NormalChat_ReturnsFalse(string text)
    {
        Assert.False(DroppedFileParser.LooksLikePath(text));
    }

    // ── TryGetFiles (injected existence check) ────────────────────────

    [Fact]
    public void TryGetFiles_SingleAbsolutePath_Detected()
    {
        var path = Abs("Users", "me", "cat.png");
        Assert.True(DroppedFileParser.TryGetFiles(path, out var files, Exists(path)));
        Assert.Equal([path], files);
    }

    [Fact]
    public void TryGetFiles_QuotedPathWithSpaces_StripsQuotes()
    {
        var path = Abs("My Files", "a b.png");
        Assert.True(DroppedFileParser.TryGetFiles($"\"{path}\"", out var files, Exists(path)));
        Assert.Equal([path], files);
    }

    [Fact]
    public void TryGetFiles_MultipleQuotedPaths_Detected()
    {
        var a = Abs("a.png");
        var b = Abs("b.mp3");
        Assert.True(DroppedFileParser.TryGetFiles($"\"{a}\" \"{b}\"", out var files, Exists(a, b)));
        Assert.Equal([a, b], files);
    }

    [Fact]
    public void TryGetFiles_PosixAbsolutePath_Detected()
    {
        // Path.IsPathFullyQualified treats "/x" as fully qualified only on non-Windows;
        // this asserts the parser defers that judgment to the platform.
        var isPosix = !OperatingSystem.IsWindows();
        var detected = DroppedFileParser.TryGetFiles("/home/me/song.mp3", out var files, Exists("/home/me/song.mp3"));
        Assert.Equal(isPosix, detected);
        if (isPosix)
            Assert.Equal(["/home/me/song.mp3"], files);
    }

    [Fact]
    public void TryGetFiles_NonExistentPath_ReturnsFalse()
    {
        Assert.False(DroppedFileParser.TryGetFiles(Abs("nope", "missing.png"), out _, _ => false));
    }

    [Fact]
    public void TryGetFiles_PartialPathDuringTyping_ReturnsFalseUntilComplete()
    {
        // Only the fully typed path exists; prefixes do not.
        var full = Abs("Users", "me", "cat.png");
        var partial = Abs("Users", "me", "ca");
        var exists = Exists(full);
        Assert.False(DroppedFileParser.TryGetFiles(partial, out _, exists));
        Assert.True(DroppedFileParser.TryGetFiles(full, out _, exists));
    }

    [Fact]
    public void TryGetFiles_OneMissingAmongMultiple_ReturnsFalse()
    {
        var a = Abs("a.png");
        var gone = Abs("gone.png");
        Assert.False(DroppedFileParser.TryGetFiles($"\"{a}\" \"{gone}\"", out _, Exists(a)));
    }

    [Fact]
    public void TryGetFiles_RealTempFile_DetectedWithDefaultExists()
    {
        var temp = Path.Combine(Path.GetTempPath(), $"echohub_drop_{Guid.NewGuid():N}.txt");
        File.WriteAllText(temp, "x");
        try
        {
            Assert.True(DroppedFileParser.TryGetFiles(temp, out var files));
            Assert.Single(files);
            Assert.Equal(temp, files[0]);
        }
        finally
        {
            File.Delete(temp);
        }
    }

    private static Func<string, bool> Exists(params string[] existing)
    {
        var set = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        return set.Contains;
    }

    /// <summary>
    /// Builds an absolute path that <see cref="Path.IsPathFullyQualified"/> accepts on the current
    /// OS — <c>C:\a\b</c> on Windows, <c>/a/b</c> elsewhere — so these tests run on any platform (CI is Linux).
    /// </summary>
    private static string Abs(params string[] segments) =>
        OperatingSystem.IsWindows()
            ? "C:\\" + string.Join('\\', segments)
            : "/" + string.Join('/', segments);
}
