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
    /// printable color tags for 2x vertical resolution.
    /// Each character cell represents two vertical pixels.
    /// Format: {F:RRGGBB} foreground, {B:RRGGBB} background, {X} reset.
    /// Uses only printable ASCII — no terminal escape bytes.
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
                    fgR = topPixel.R; fgG = topPixel.G; fgB = topPixel.B;
                    bgR = topPixel.R; bgG = topPixel.G; bgB = topPixel.B;
                    blockChar = '\u2588'; // █
                }
                else
                {
                    fgR = topPixel.R; fgG = topPixel.G; fgB = topPixel.B;
                    bgR = bottomPixel.R; bgG = bottomPixel.G; bgB = bottomPixel.B;
                    blockChar = '\u2580'; // ▀
                }

                bool fgChanged = !hasLastColor || fgR != lastFgR || fgG != lastFgG || fgB != lastFgB;
                bool bgChanged = !hasLastColor || bgR != lastBgR || bgG != lastBgG || bgB != lastBgB;

                if (fgChanged)
                    sb.Append($"{{F:{fgR:X2}{fgG:X2}{fgB:X2}}}");
                if (bgChanged)
                    sb.Append($"{{B:{bgR:X2}{bgG:X2}{bgB:X2}}}");

                sb.Append(blockChar);

                lastFgR = fgR; lastFgG = fgG; lastFgB = fgB;
                lastBgR = bgR; lastBgG = bgG; lastBgB = bgB;
                hasLastColor = true;
            }

            sb.Append("{X}");
            hasLastColor = false;

            if (y + 2 < image.Height)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}
