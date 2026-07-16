using EchoHub.Client.UI.Helpers;
using Xunit;

namespace EchoHub.Tests;

/// <summary>
/// Tests for the deterministic nick→palette-index hash.
/// Note: GetAttribute (Terminal.Gui Attribute) is excluded — Terminal.Gui's module
/// initializer requires a display driver unavailable in CI.
/// </summary>
public class NickColorHelperTests
{
    [Fact]
    public void GetPaletteIndex_SameNick_IsStable()
    {
        var first = NickColorHelper.GetPaletteIndex("alice", 12);
        var second = NickColorHelper.GetPaletteIndex("alice", 12);
        Assert.Equal(first, second);
    }

    [Fact]
    public void GetPaletteIndex_IsCaseInsensitive()
    {
        Assert.Equal(
            NickColorHelper.GetPaletteIndex("Alice", 12),
            NickColorHelper.GetPaletteIndex("aLICE", 12));
    }

    [Theory]
    [InlineData("alice")]
    [InlineData("bob")]
    [InlineData("charlie_long_nickname")]
    [InlineData("")]
    [InlineData("émile")]
    public void GetPaletteIndex_AlwaysWithinRange(string nick)
    {
        var index = NickColorHelper.GetPaletteIndex(nick, 12);
        Assert.InRange(index, 0, 11);
    }

    [Fact]
    public void GetPaletteIndex_DistributesAcrossPalette()
    {
        // Not a strict uniformity test — just that the hash isn't degenerate
        var nicks = new[] { "alice", "bob", "carol", "dave", "erin", "frank", "grace", "heidi" };
        var distinct = nicks.Select(n => NickColorHelper.GetPaletteIndex(n, 12)).Distinct().Count();
        Assert.True(distinct >= 3, $"Expected at least 3 distinct palette slots, got {distinct}");
    }

    [Fact]
    public void GetPaletteIndex_NonPositivePaletteSize_ReturnsZero()
    {
        Assert.Equal(0, NickColorHelper.GetPaletteIndex("alice", 0));
    }
}
