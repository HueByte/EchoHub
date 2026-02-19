using EchoHub.Core.Constants;
using Xunit;

namespace EchoHub.Tests;

public class ValidationConstantsTests
{
    [Theory]
    [InlineData("alice", true)]
    [InlineData("Bob_123", true)]
    [InlineData("user-name", true)]
    [InlineData("abc", true)]
    [InlineData("ab", false)]
    [InlineData("", false)]
    [InlineData("has space", false)]
    [InlineData("has@symbol", false)]
    public void UsernameRegex_ValidatesCorrectly(string input, bool expected)
    {
        var result = ValidationConstants.UsernameRegex().IsMatch(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("general", true)]
    [InlineData("my-channel_01", true)]
    [InlineData("ab", true)]
    [InlineData("a", false)]
    [InlineData("has space", false)]
    public void ChannelNameRegex_ValidatesCorrectly(string input, bool expected)
    {
        var result = ValidationConstants.ChannelNameRegex().IsMatch(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("#FF0000", true)]
    [InlineData("#aabbcc", true)]
    [InlineData("FF0000", false)]
    [InlineData("#FFF", false)]
    [InlineData("#GGGGGG", false)]
    public void HexColorRegex_ValidatesCorrectly(string input, bool expected)
    {
        var result = ValidationConstants.HexColorRegex().IsMatch(input);
        Assert.Equal(expected, result);
    }
}
