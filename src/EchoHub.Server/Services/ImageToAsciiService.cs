using System.Text;
using EchoHub.Core.Constants;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace EchoHub.Server.Services;

public class ImageToAsciiService
{
    private static readonly char[] AsciiChars = " .:-=+*#%@".ToCharArray();

    public string ConvertToAscii(Stream imageStream, int width = HubConstants.AsciiArtWidth, int height = HubConstants.AsciiArtHeight)
    {
        using var image = Image.Load<Rgba32>(imageStream);

        image.Mutate(x => x.Resize(width, height));

        var sb = new StringBuilder();

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var pixel = image[x, y];
                var brightness = 0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B;

                // Map brightness (0-255) to ASCII char index (inverted: dark pixels get dense chars)
                var index = (int)((brightness / 255.0) * (AsciiChars.Length - 1));
                sb.Append(AsciiChars[index]);
            }

            if (y < image.Height - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}
