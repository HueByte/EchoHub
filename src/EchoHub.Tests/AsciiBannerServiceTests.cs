using EchoHub.Core.Services;
using Xunit;

namespace EchoHub.Tests;

public class AsciiBannerServiceTests
{
    [Fact]
    public void Render_SimpleText_ProducesFiveRows()
    {
        var banner = AsciiBannerService.Render("hi");

        Assert.NotNull(banner);
        Assert.Equal(5, banner!.Split('\n').Length);
        Assert.Contains("█", banner);
    }

    [Fact]
    public void Render_IsCaseInsensitive()
    {
        Assert.Equal(AsciiBannerService.Render("abc"), AsciiBannerService.Render("ABC"));
    }

    [Fact]
    public void Render_Empty_ReturnsNull()
    {
        Assert.Null(AsciiBannerService.Render(""));
        Assert.Null(AsciiBannerService.Render("   "));
    }

    [Fact]
    public void Render_OnlyUnsupportedChars_ReturnsNull()
    {
        Assert.Null(AsciiBannerService.Render("🦆🦆🦆"));
    }

    [Fact]
    public void Render_UnsupportedCharsSkipped_SupportedRemain()
    {
        var mixed = AsciiBannerService.Render("a🦆b");
        var plain = AsciiBannerService.Render("ab");

        Assert.Equal(plain, mixed);
    }

    [Fact]
    public void Render_InputLongerThanCap_IsTruncatedNotRejected()
    {
        var banner = AsciiBannerService.Render(new string('a', AsciiBannerService.MaxInputLength + 30));

        Assert.NotNull(banner);
        // 20 glyphs of 'A' (4 cols) + 19 separators — sane width, not 50 glyphs
        var firstRow = banner!.Split('\n')[0];
        Assert.True(firstRow.Length <= AsciiBannerService.MaxInputLength * 6);
    }

    [Fact]
    public void Render_DigitsAndPunctuation_Supported()
    {
        Assert.NotNull(AsciiBannerService.Render("42!"));
        Assert.NotNull(AsciiBannerService.Render("v0.2"));
    }

    [Fact]
    public void Render_FitsMessageLimits()
    {
        // Worst case must stay under the server's message length cap
        var banner = AsciiBannerService.Render(new string('w', AsciiBannerService.MaxInputLength));

        Assert.NotNull(banner);
        Assert.True(banner!.Length <= EchoHub.Core.Constants.HubConstants.MaxMessageLength);
    }
}
