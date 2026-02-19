namespace EchoHub.Server.Services;

public static class FileValidationHelper
{
    private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] GifMagic = [0x47, 0x49, 0x46];
    private static readonly byte[] WebpRiff = [0x52, 0x49, 0x46, 0x46]; // "RIFF"
    private static readonly byte[] WebpTag = [0x57, 0x45, 0x42, 0x50]; // "WEBP"

    /// <summary>
    /// Validates that a stream contains a recognized image format by checking magic bytes.
    /// The stream position is reset to the beginning after validation.
    /// </summary>
    public static bool IsValidImage(Stream stream)
    {
        if (!stream.CanSeek)
            return false;

        var originalPosition = stream.Position;
        try
        {
            var header = new byte[12];
            var bytesRead = stream.Read(header, 0, header.Length);

            if (bytesRead < 3)
                return false;

            // JPEG: FF D8 FF
            if (StartsWith(header, bytesRead, JpegMagic))
                return true;

            // PNG: 89 50 4E 47
            if (bytesRead >= 4 && StartsWith(header, bytesRead, PngMagic))
                return true;

            // GIF: 47 49 46 (GIF87a or GIF89a)
            if (StartsWith(header, bytesRead, GifMagic))
                return true;

            // WebP: RIFF....WEBP
            if (bytesRead >= 12 && StartsWith(header, bytesRead, WebpRiff)
                && header[8] == WebpTag[0] && header[9] == WebpTag[1]
                && header[10] == WebpTag[2] && header[11] == WebpTag[3])
                return true;

            return false;
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }

    private static bool StartsWith(byte[] buffer, int length, byte[] magic)
    {
        if (length < magic.Length)
            return false;

        for (int i = 0; i < magic.Length; i++)
        {
            if (buffer[i] != magic[i])
                return false;
        }

        return true;
    }
}
