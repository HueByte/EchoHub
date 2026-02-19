using System.Text;
using EchoHub.Core.Constants;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace EchoHub.Server.Services;

public class ImageToAsciiService
{
    /// <summary>
    /// Returns (width, height) dimensions for the given size code.
    /// s = small (40x40), m = medium/default (80x80), l = large (120x120).
    /// </summary>
    public static (int Width, int Height) GetDimensions(string? size) => size?.ToLowerInvariant() switch
    {
        "s" => (40, 40),
        "l" => (120, 120),
        _ => (HubConstants.AsciiArtWidth, HubConstants.AsciiArtHeightHalfBlock),
    };

    /// <summary>
    /// Converts an image to ASCII art using half-block characters (▀▄█) with
    /// 24-bit ANSI foreground and background colors for 2x vertical resolution.
    /// Each character cell represents two vertical pixels.
    /// </summary>
    public string ConvertToAscii(Stream imageStream, int width = HubConstants.AsciiArtWidth, int height = HubConstants.AsciiArtHeightHalfBlock)
    {
        using var image = Image.Load<Rgba32>(imageStream);

        // Ensure height is even for pair processing
        if (height % 2 != 0) height++;

        image.Mutate(x => x.Resize(width, height));

        var sb = new StringBuilder();

        for (int y = 0; y < image.Height; y += 2)
        {
            byte lastFgR = 0, lastFgG = 0, lastFgB = 0;
            byte lastBgR = 0, lastBgG = 0, lastBgB = 0;
            bool hasLastColor = false;

            for (int x = 0; x < image.Width; x++)
            {
                var topPixel = image[x, y];
                var bottomPixel = (y + 1 < image.Height) ? image[x, y + 1] : topPixel;

                byte fgR, fgG, fgB, bgR, bgG, bgB;
                char blockChar;

                if (topPixel.R == bottomPixel.R && topPixel.G == bottomPixel.G && topPixel.B == bottomPixel.B)
                {
                    // Both pixels same color — full block
                    fgR = topPixel.R; fgG = topPixel.G; fgB = topPixel.B;
                    bgR = topPixel.R; bgG = topPixel.G; bgB = topPixel.B;
                    blockChar = '\u2588'; // █
                }
                else
                {
                    // Top pixel = foreground, bottom pixel = background, upper half block
                    fgR = topPixel.R; fgG = topPixel.G; fgB = topPixel.B;
                    bgR = bottomPixel.R; bgG = bottomPixel.G; bgB = bottomPixel.B;
                    blockChar = '\u2580'; // ▀
                }

                // Emit color codes only when they change
                bool fgChanged = !hasLastColor || fgR != lastFgR || fgG != lastFgG || fgB != lastFgB;
                bool bgChanged = !hasLastColor || bgR != lastBgR || bgG != lastBgG || bgB != lastBgB;

                if (fgChanged)
                    sb.Append($"\x1b[38;2;{fgR};{fgG};{fgB}m");
                if (bgChanged)
                    sb.Append($"\x1b[48;2;{bgR};{bgG};{bgB}m");

                sb.Append(blockChar);

                lastFgR = fgR; lastFgG = fgG; lastFgB = fgB;
                lastBgR = bgR; lastBgG = bgG; lastBgB = bgB;
                hasLastColor = true;
            }

            // Reset color at end of line
            sb.Append("\x1b[0m");
            hasLastColor = false;

            if (y + 2 < image.Height)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}
