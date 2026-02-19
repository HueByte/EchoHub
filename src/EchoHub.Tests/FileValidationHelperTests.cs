using EchoHub.Server.Services;
using Xunit;

namespace EchoHub.Tests;

public class FileValidationHelperTests
{
    [Fact]
    public void IsValidImage_JpegMagicBytes_ReturnsTrue()
    {
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
        using var stream = new MemoryStream(jpeg);
        Assert.True(FileValidationHelper.IsValidImage(stream));
    }

    [Fact]
    public void IsValidImage_PngMagicBytes_ReturnsTrue()
    {
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        using var stream = new MemoryStream(png);
        Assert.True(FileValidationHelper.IsValidImage(stream));
    }

    [Fact]
    public void IsValidImage_GifMagicBytes_ReturnsTrue()
    {
        byte[] gif = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61];
        using var stream = new MemoryStream(gif);
        Assert.True(FileValidationHelper.IsValidImage(stream));
    }

    [Fact]
    public void IsValidImage_WebpMagicBytes_ReturnsTrue()
    {
        byte[] webp = [0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50];
        using var stream = new MemoryStream(webp);
        Assert.True(FileValidationHelper.IsValidImage(stream));
    }

    [Fact]
    public void IsValidImage_RandomBytes_ReturnsFalse()
    {
        byte[] random = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05];
        using var stream = new MemoryStream(random);
        Assert.False(FileValidationHelper.IsValidImage(stream));
    }

    [Fact]
    public void IsValidImage_ResetsStreamPosition()
    {
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
        using var stream = new MemoryStream(jpeg);
        FileValidationHelper.IsValidImage(stream);
        Assert.Equal(0, stream.Position);
    }
}
