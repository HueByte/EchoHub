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

        byte lastR = 0, lastG = 0, lastB = 0;
        bool hasLastColor = false;

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var pixel = image[x, y];
                var brightness = 0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B;

                // Map brightness (0-255) to ASCII char index
                var index = (int)((brightness / 255.0) * (AsciiChars.Length - 1));

                // Emit ANSI 24-bit color only when it changes
                if (!hasLastColor || pixel.R != lastR || pixel.G != lastG || pixel.B != lastB)
                {
                    sb.Append($"\x1b[38;2;{pixel.R};{pixel.G};{pixel.B}m");
                    lastR = pixel.R;
                    lastG = pixel.G;
                    lastB = pixel.B;
                    hasLastColor = true;
                }

                sb.Append(AsciiChars[index]);
            }

            // Reset color at end of line
            sb.Append("\x1b[0m");
            hasLastColor = false;

            if (y < image.Height - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}
