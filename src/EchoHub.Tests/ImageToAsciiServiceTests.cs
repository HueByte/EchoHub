using EchoHub.Core.Constants;
using EchoHub.Server.Services;
using Xunit;

namespace EchoHub.Tests;

public class ImageToAsciiServiceTests
{
    [Fact]
    public void GetDimensions_Small_Returns40x40()
    {
        var (w, h) = ImageToAsciiService.GetDimensions("s");
        Assert.Equal(40, w);
        Assert.Equal(40, h);
    }

    [Fact]
    public void GetDimensions_Large_Returns120x120()
    {
        var (w, h) = ImageToAsciiService.GetDimensions("l");
        Assert.Equal(120, w);
        Assert.Equal(120, h);
    }

    [Fact]
    public void GetDimensions_Default_Returns80x80()
    {
        var (w, h) = ImageToAsciiService.GetDimensions("m");
        Assert.Equal(HubConstants.AsciiArtWidth, w);
        Assert.Equal(HubConstants.AsciiArtHeightHalfBlock, h);
    }

    [Fact]
    public void GetDimensions_Null_ReturnsDefault()
    {
        var (w, h) = ImageToAsciiService.GetDimensions(null);
        Assert.Equal(HubConstants.AsciiArtWidth, w);
        Assert.Equal(HubConstants.AsciiArtHeightHalfBlock, h);
    }

    [Fact]
    public void GetDimensions_CaseInsensitive()
    {
        var (w1, h1) = ImageToAsciiService.GetDimensions("S");
        var (w2, h2) = ImageToAsciiService.GetDimensions("s");
        Assert.Equal(w1, w2);
        Assert.Equal(h1, h2);

        var (w3, h3) = ImageToAsciiService.GetDimensions("L");
        var (w4, h4) = ImageToAsciiService.GetDimensions("l");
        Assert.Equal(w3, w4);
        Assert.Equal(h3, h4);
    }

    [Fact]
    public void GetDimensions_UnknownSize_ReturnsDefault()
    {
        var (w, h) = ImageToAsciiService.GetDimensions("xl");
        Assert.Equal(HubConstants.AsciiArtWidth, w);
        Assert.Equal(HubConstants.AsciiArtHeightHalfBlock, h);
    }
}
