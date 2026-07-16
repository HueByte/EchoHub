using System.Text;

namespace EchoHub.Client.UI.Helpers;

/// <summary>
/// Recognizes a dragged-and-dropped file (or files) that a terminal delivers into the input as an
/// absolute path. Terminals differ: some paste the whole path at once, others send it character by
/// character; either way this checks whether the current input text resolves to existing file(s).
/// </summary>
public static class DroppedFileParser
{
    /// <summary>
    /// Cheap pre-check so callers only stat the filesystem when the input plausibly holds a path:
    /// a quoted path, a Windows drive path (<c>X:\</c>/<c>X:/</c>), a UNC path (<c>\\</c>), or a
    /// POSIX absolute path (<c>/</c>). Normal chat text never starts this way.
    /// </summary>
    public static bool LooksLikePath(string text)
    {
        var t = text.TrimStart();
        if (t.Length < 3)
            return false;
        if (t[0] is '"' or '/')
            return true;
        if (t.StartsWith(@"\\", StringComparison.Ordinal))
            return true;
        return char.IsLetter(t[0]) && t[1] == ':' && (t[2] == '\\' || t[2] == '/');
    }

    /// <summary>
    /// Returns true when <paramref name="text"/> resolves to one or more existing files.
    /// Handles a single path (quoted or not, possibly containing spaces) and multiple
    /// space-separated (optionally quoted) paths. <paramref name="fileExists"/> is injectable
    /// for testing; production passes <see cref="File.Exists"/>.
    /// </summary>
    public static bool TryGetFiles(string text, out List<string> files, Func<string, bool>? fileExists = null)
    {
        fileExists ??= File.Exists;
        files = [];

        var trimmed = text.Trim();
        if (trimmed.Length < 3 || trimmed.Length > 4096 || trimmed.Contains('\n'))
            return false;

        // Single path, possibly quoted and/or containing spaces.
        var unquoted = StripQuotes(trimmed);
        if (Path.IsPathFullyQualified(unquoted) && fileExists(unquoted))
        {
            files.Add(unquoted);
            return true;
        }

        // Multiple files: space-separated tokens, each optionally quoted.
        foreach (var token in TokenizeQuoted(trimmed))
        {
            if (!Path.IsPathFullyQualified(token) || !fileExists(token))
            {
                files.Clear();
                return false;
            }
            files.Add(token);
        }

        return files.Count > 0;
    }

    private static string StripQuotes(string s) =>
        s.Length >= 2 && ((s[0] == '"' && s[^1] == '"') || (s[0] == '\'' && s[^1] == '\''))
            ? s[1..^1]
            : s;

    private static IEnumerable<string> TokenizeQuoted(string input)
    {
        var current = new StringBuilder();
        var quote = '\0';

        foreach (var c in input)
        {
            if (quote != '\0')
            {
                if (c == quote) quote = '\0';
                else current.Append(c);
            }
            else if (c is '"' or '\'')
            {
                quote = c;
            }
            else if (c == ' ')
            {
                if (current.Length > 0)
                {
                    yield return current.ToString();
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
            yield return current.ToString();
    }
}
