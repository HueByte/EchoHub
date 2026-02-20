using EchoHub.Server.Setup;
using Xunit;

namespace EchoHub.Tests;

public class DataMigrationServiceTests
{
    // ── AnsiToColorTags ───────────────────────────────────────────────

    [Fact]
    public void AnsiToColorTags_ForegroundEscape_ConvertedToColorTag()
    {
        var ansi = "\x1b[38;2;255;0;0mred text";
        var result = DataMigrationService.AnsiToColorTags(ansi);

        Assert.Equal("{F:FF0000}red text", result);
    }

    [Fact]
    public void AnsiToColorTags_BackgroundEscape_ConvertedToColorTag()
    {
        var ansi = "\x1b[48;2;0;255;0mgreen bg";
        var result = DataMigrationService.AnsiToColorTags(ansi);

        Assert.Equal("{B:00FF00}green bg", result);
    }

    [Fact]
    public void AnsiToColorTags_ResetEscape_ConvertedToResetTag()
    {
        var ansi = "\x1b[0m";
        var result = DataMigrationService.AnsiToColorTags(ansi);

        Assert.Equal("{X}", result);
    }

    [Fact]
    public void AnsiToColorTags_NoEscapes_ReturnsUnchanged()
    {
        var text = "Hello, world!";
        var result = DataMigrationService.AnsiToColorTags(text);

        Assert.Equal("Hello, world!", result);
    }

    [Fact]
    public void AnsiToColorTags_MixedContent_ConvertsEscapesOnly()
    {
        var ansi = "before\x1b[38;2;100;200;50mtextafter";
        var result = DataMigrationService.AnsiToColorTags(ansi);

        Assert.Equal("before{F:64C832}textafter", result);
    }

    [Fact]
    public void AnsiToColorTags_MultipleTags_AllConverted()
    {
        var ansi = "\x1b[38;2;255;0;0mred\x1b[48;2;0;0;255mblue bg\x1b[0mreset";
        var result = DataMigrationService.AnsiToColorTags(ansi);

        Assert.Equal("{F:FF0000}red{B:0000FF}blue bg{X}reset", result);
    }

    [Fact]
    public void AnsiToColorTags_RoundTrip_WithColorTagsToAnsi()
    {
        // AnsiToColorTags and IrcMessageFormatter.ColorTagsToAnsi should be inverses
        var original = "{F:FF0000}red{B:00FF00}green{X}";
        var ansi = EchoHub.Server.Irc.IrcMessageFormatter.ColorTagsToAnsi(original);
        var backToTags = DataMigrationService.AnsiToColorTags(ansi);

        Assert.Equal(original, backToTags);
    }
}
