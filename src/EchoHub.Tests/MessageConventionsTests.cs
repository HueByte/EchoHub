using EchoHub.Core.Constants;
using Xunit;

namespace EchoHub.Tests;

public class MessageConventionsTests
{
    [Fact]
    public void FormatAction_RoundTripsThroughTryParse()
    {
        var wire = MessageConventions.FormatAction("waves at everyone");

        Assert.True(MessageConventions.TryParseAction(wire, out var text));
        Assert.Equal("waves at everyone", text);
    }

    [Fact]
    public void FormatAction_UsesCtcpDelimiters()
    {
        var wire = MessageConventions.FormatAction("waves");

        // Exact IRC CTCP ACTION wire shape: \x01ACTION waves\x01
        Assert.Equal("\u0001ACTION waves\u0001", wire);
    }

    [Fact]
    public void TryParseAction_PlainText_ReturnsFalse()
    {
        Assert.False(MessageConventions.TryParseAction("hello world", out _));
    }

    [Fact]
    public void TryParseAction_TextStartingWithWordAction_ReturnsFalse()
    {
        // A user typing "ACTION stations!" is not a /me
        Assert.False(MessageConventions.TryParseAction("ACTION stations!", out _));
    }

    [Fact]
    public void TryParseAction_EmptyAction_ReturnsFalse()
    {
        Assert.False(MessageConventions.TryParseAction("\u0001ACTION \u0001", out _));
    }
}
