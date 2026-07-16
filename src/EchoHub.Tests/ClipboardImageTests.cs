using EchoHub.Client.Services;
using EchoHub.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace EchoHub.Tests;

public class ClipboardImageTests
{
    /// <summary>
    /// Encodes an image as BMP and strips the 14-byte BITMAPFILEHEADER, producing exactly what
    /// the Windows clipboard hands out as CF_DIB.
    /// </summary>
    private static byte[] MakeDib(Image<Rgba32> image, BmpBitsPerPixel bpp)
    {
        using var ms = new MemoryStream();
        image.Save(ms, new BmpEncoder { BitsPerPixel = bpp });
        return ms.ToArray()[14..];
    }

    private static Image<Rgba32> MakeTestImage()
    {
        var image = new Image<Rgba32>(4, 3);
        image[0, 0] = new Rgba32(255, 0, 0);
        image[3, 2] = new Rgba32(0, 0, 255);
        return image;
    }

    [Theory]
    [InlineData(BmpBitsPerPixel.Pixel24)]
    [InlineData(BmpBitsPerPixel.Pixel32)]
    [InlineData(BmpBitsPerPixel.Pixel8)] // palette-based: exercises the palette offset math
    public void DibToPng_ConvertsDibToValidPng(BmpBitsPerPixel bpp)
    {
        using var original = MakeTestImage();
        var dib = MakeDib(original, bpp);

        var png = ClipboardImage.DibToPng(dib);

        Assert.NotNull(png);
        using var pngStream = new MemoryStream(png);
        Assert.True(FileValidationHelper.IsValidImage(pngStream));

        using var decoded = Image.Load<Rgba32>(png);
        Assert.Equal(original.Width, decoded.Width);
        Assert.Equal(original.Height, decoded.Height);
    }

    [Fact]
    public void DibToPng_PreservesPixels_For24Bpp()
    {
        using var original = MakeTestImage();
        var dib = MakeDib(original, BmpBitsPerPixel.Pixel24);

        var png = ClipboardImage.DibToPng(dib);

        Assert.NotNull(png);
        using var decoded = Image.Load<Rgba32>(png);
        Assert.Equal(new Rgba32(255, 0, 0), decoded[0, 0]);
        Assert.Equal(new Rgba32(0, 0, 255), decoded[3, 2]);
    }

    [Fact]
    public void DibToPng_ReturnsNull_ForTruncatedData()
    {
        Assert.Null(ClipboardImage.DibToPng([0x28, 0x00, 0x00]));
    }

    [Fact]
    public void DibToPng_ReturnsNull_ForGarbageData()
    {
        var garbage = new byte[256];
        new Random(42).NextBytes(garbage);
        Assert.Null(ClipboardImage.DibToPng(garbage));
    }
}
