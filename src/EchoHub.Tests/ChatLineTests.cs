using EchoHub.Client.UI.Chat;
using Xunit;

namespace EchoHub.Tests;

/// <summary>
/// Tests for static string-utility methods on ChatLine.
/// Note: Tests that construct ChatLine/ChatListSource or use Terminal.Gui types
/// (Attribute, Color) are excluded because Terminal.Gui's module initializer
/// requires a display driver which is unavailable in CI/test environments.
/// </summary>
public class ChatLineTests
{
    // ── HasColorTags ──────────────────────────────────────────────────

    [Fact]
    public void HasColorTags_ForegroundTag_ReturnsTrue()
    {
        Assert.True(ChatLine.HasColorTags("Hello {F:FF0000}world"));
    }

    [Fact]
    public void HasColorTags_BackgroundTag_ReturnsTrue()
    {
        Assert.True(ChatLine.HasColorTags("Hello {B:00FF00}world"));
    }

    [Fact]
    public void HasColorTags_ResetTag_ReturnsTrue()
    {
        Assert.True(ChatLine.HasColorTags("Hello{X}"));
    }

    [Fact]
    public void HasColorTags_NoTags_ReturnsFalse()
    {
        Assert.False(ChatLine.HasColorTags("Hello world"));
    }

    [Fact]
    public void HasColorTags_PartialTag_ReturnsFalse()
    {
        // {Z:...} is not a valid tag (only F or B)
        Assert.False(ChatLine.HasColorTags("Hello {Z:000000}"));
    }

    [Fact]
    public void HasColorTags_EmptyString_ReturnsFalse()
    {
        Assert.False(ChatLine.HasColorTags(""));
    }

    [Theory]
    [InlineData("{F:AABBCC}text")]
    [InlineData("prefix{B:112233}suffix")]
    [InlineData("a{X}b")]
    [InlineData("{F:000000}{B:FFFFFF}{X}")]
    public void HasColorTags_VariousValidTags_ReturnsTrue(string input)
    {
        Assert.True(ChatLine.HasColorTags(input));
    }

    // ── StripColorTags ────────────────────────────────────────────────

    [Fact]
    public void StripColorTags_RemovesAllTags()
    {
        var result = ChatLine.StripColorTags("{F:FF0000}red{B:00FF00}green{X}");
        Assert.Equal("redgreen", result);
    }

    [Fact]
    public void StripColorTags_NoTags_ReturnsOriginal()
    {
        var result = ChatLine.StripColorTags("plain text");
        Assert.Equal("plain text", result);
    }

    [Fact]
    public void StripColorTags_OnlyTags_ReturnsEmpty()
    {
        var result = ChatLine.StripColorTags("{F:AABBCC}{B:112233}{X}");
        Assert.Equal("", result);
    }

    [Fact]
    public void StripColorTags_MixedContent_KeepsText()
    {
        var result = ChatLine.StripColorTags("before{F:FF0000}middle{X}after");
        Assert.Equal("beforemiddleafter", result);
    }

    [Fact]
    public void StripColorTags_MultipleConsecutiveTags_AllStripped()
    {
        var result = ChatLine.StripColorTags("{F:FF0000}{B:00FF00}{X}{F:0000FF}text{X}");
        Assert.Equal("text", result);
    }

    [Fact]
    public void StripColorTags_PreservesNonTagBraces()
    {
        // {Hello} is not a valid tag and should be preserved
        var result = ChatLine.StripColorTags("{Hello} world");
        Assert.Equal("{Hello} world", result);
    }
}
