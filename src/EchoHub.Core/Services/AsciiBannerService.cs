using System.Text;

namespace EchoHub.Core.Services;

/// <summary>
/// Renders short text as a 5-row block-character banner (the <c>/banner</c> command).
/// Entirely local — a hand-rolled figlet-style font, no dependencies, no network.
/// Output is plain message content, so it travels (and encrypts) like any other text.
/// </summary>
public static class AsciiBannerService
{
    public const int MaxInputLength = 20;
    private const int Rows = 5;

    /// <summary>
    /// Renders <paramref name="text"/> as banner lines joined with '\n'.
    /// Returns null when the input is empty or contains no renderable characters.
    /// Characters outside the font (letters, digits, and basic punctuation) are skipped.
    /// </summary>
    public static string? Render(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        text = text.Trim();
        if (text.Length > MaxInputLength)
            text = text[..MaxInputLength];

        var glyphs = new List<string[]>();
        foreach (var ch in text.ToUpperInvariant())
        {
            if (Font.TryGetValue(ch, out var glyph))
                glyphs.Add(glyph);
        }

        if (glyphs.Count == 0 || glyphs.All(g => g == Font[' ']))
            return null;

        var lines = new StringBuilder();
        for (var row = 0; row < Rows; row++)
        {
            if (row > 0)
                lines.Append('\n');
            for (var i = 0; i < glyphs.Count; i++)
            {
                if (i > 0)
                    lines.Append(' ');
                lines.Append(glyphs[i][row].Replace('#', '█').Replace('.', ' '));
            }
        }

        // Trim trailing spaces per line — cheaper payload, identical rendering
        return string.Join('\n', lines.ToString().Split('\n').Select(l => l.TrimEnd()));
    }

    // Glyphs are authored with '#' (ink) and '.' (blank); all rows of a glyph share one width.
    private static readonly Dictionary<char, string[]> Font = new()
    {
        [' '] = ["..", "..", "..", "..", ".."],
        ['A'] = [".##.", "#..#", "####", "#..#", "#..#"],
        ['B'] = ["###.", "#..#", "###.", "#..#", "###."],
        ['C'] = [".###", "#...", "#...", "#...", ".###"],
        ['D'] = ["###.", "#..#", "#..#", "#..#", "###."],
        ['E'] = ["####", "#...", "###.", "#...", "####"],
        ['F'] = ["####", "#...", "###.", "#...", "#..."],
        ['G'] = [".###", "#...", "#.##", "#..#", ".###"],
        ['H'] = ["#..#", "#..#", "####", "#..#", "#..#"],
        ['I'] = ["###", ".#.", ".#.", ".#.", "###"],
        ['J'] = ["..##", "...#", "...#", "#..#", ".##."],
        ['K'] = ["#..#", "#.#.", "##..", "#.#.", "#..#"],
        ['L'] = ["#...", "#...", "#...", "#...", "####"],
        ['M'] = ["#...#", "##.##", "#.#.#", "#...#", "#...#"],
        ['N'] = ["#...#", "##..#", "#.#.#", "#..##", "#...#"],
        ['O'] = [".##.", "#..#", "#..#", "#..#", ".##."],
        ['P'] = ["###.", "#..#", "###.", "#...", "#..."],
        ['Q'] = [".##.", "#..#", "#..#", "#.##", ".###"],
        ['R'] = ["###.", "#..#", "###.", "#.#.", "#..#"],
        ['S'] = [".###", "#...", ".##.", "...#", "###."],
        ['T'] = ["###", ".#.", ".#.", ".#.", ".#."],
        ['U'] = ["#..#", "#..#", "#..#", "#..#", ".##."],
        ['V'] = ["#...#", "#...#", "#...#", ".#.#.", "..#.."],
        ['W'] = ["#...#", "#...#", "#.#.#", "##.##", "#...#"],
        ['X'] = ["#...#", ".#.#.", "..#..", ".#.#.", "#...#"],
        ['Y'] = ["#...#", ".#.#.", "..#..", "..#..", "..#.."],
        ['Z'] = ["####", "...#", "..#.", ".#..", "####"],
        ['0'] = [".##.", "#.##", "##.#", "#..#", ".##."],
        ['1'] = [".#.", "##.", ".#.", ".#.", "###"],
        ['2'] = [".##.", "#..#", "..#.", ".#..", "####"],
        ['3'] = ["###.", "...#", ".##.", "...#", "###."],
        ['4'] = ["#..#", "#..#", "####", "...#", "...#"],
        ['5'] = ["####", "#...", "###.", "...#", "###."],
        ['6'] = [".##.", "#...", "###.", "#..#", ".##."],
        ['7'] = ["####", "...#", "..#.", ".#..", ".#.."],
        ['8'] = [".##.", "#..#", ".##.", "#..#", ".##."],
        ['9'] = [".##.", "#..#", ".###", "...#", ".##."],
        ['!'] = ["#", "#", "#", ".", "#"],
        ['?'] = [".##.", "#..#", "..#.", "....", "..#."],
        ['.'] = [".", ".", ".", ".", "#"],
        [','] = ["..", "..", "..", ".#", "#."],
        ['-'] = ["....", "....", "####", "....", "...."],
        ['\''] = ["#", "#", ".", ".", "."],
        [':'] = [".", "#", ".", "#", "."],
    };
}
