using System.Text;

namespace EchoHub.Server.Irc;

/// <summary>
/// Parsed representation of an IRC protocol line.
/// IRCv3 format: ['@' tags ' '] [':' prefix ' '] COMMAND [params...] [':' trailing]
/// </summary>
public sealed class IrcMessage
{
    public Dictionary<string, string?> Tags { get; init; } = new();
    public string? Prefix { get; init; }
    public string Command { get; init; } = "";
    public List<string> Parameters { get; init; } = [];

    public string? Trailing => Parameters.Count > 0 ? Parameters[^1] : null;

    /// <summary>
    /// Parse a raw IRC line with optional IRCv3 message tags.
    /// Format: ['@' tags ' '] [':' prefix ' '] COMMAND params CRLF
    /// </summary>
    public static IrcMessage Parse(string line)
    {
        var span = line.AsSpan().TrimEnd("\r\n");
        var pos = 0;

        if (span.Length == 0)
            return new IrcMessage { Command = "" };

        var tags = new Dictionary<string, string?>();

        // Parse optional IRCv3 message tags: @tag1;tag2=val2;...
        if (span[pos] == '@')
        {
            var spaceIdx = span.IndexOf(' ');
            if (spaceIdx > 1)
            {
                var tagSection = span[1..spaceIdx].ToString();
                foreach (var rawTag in tagSection.Split(';'))
                {
                    var eqIdx = rawTag.IndexOf('=');
                    if (eqIdx == -1)
                    {
                        tags[rawTag] = null;
                    }
                    else
                    {
                        var key = rawTag[..eqIdx];
                        var val = TagUnescape(rawTag[(eqIdx + 1)..]);
                        tags[key] = val;
                    }
                }
                pos = spaceIdx + 1;
            }
        }

        string? prefix = null;

        // Parse optional prefix
        if (pos < span.Length && span[pos] == ':')
        {
            var spaceIdx = span[pos..].IndexOf(' ');
            if (spaceIdx == -1)
                return new IrcMessage { Tags = tags, Prefix = span[(pos + 1)..].ToString(), Command = "" };

            prefix = span.Slice(pos + 1, spaceIdx - 1).ToString();
            pos = pos + spaceIdx + 1;
        }

        // Skip whitespace
        while (pos < span.Length && span[pos] == ' ') pos++;

        // Parse command
        if (pos >= span.Length)
            return new IrcMessage { Tags = tags, Prefix = prefix, Command = "" };

        var cmdStart = pos;
        while (pos < span.Length && span[pos] != ' ') pos++;
        var command = span[cmdStart..pos].ToString();

        // Parse parameters
        var parameters = new List<string>();
        while (pos < span.Length)
        {
            while (pos < span.Length && span[pos] == ' ') pos++;
            if (pos >= span.Length) break;

            if (span[pos] == ':')
            {
                // Trailing parameter (rest of line)
                parameters.Add(span[(pos + 1)..].ToString());
                break;
            }

            var paramStart = pos;
            while (pos < span.Length && span[pos] != ' ') pos++;
            parameters.Add(span[paramStart..pos].ToString());
        }

        return new IrcMessage
        {
            Tags = tags,
            Prefix = prefix,
            Command = command,
            Parameters = parameters,
        };
    }

    /// <summary>
    /// Builds the message tags prefix for outgoing lines.
    /// Returns "@key1=val1;key2=val2 " or empty string if no tags.
    /// Values are tag-escaped. Client-only tags (+prefix) pass through.
    /// </summary>
    public static string BuildTagPrefix(params (string Key, string? Value)[] tags)
    {
        if (tags.Length == 0) return "";

        var sb = new StringBuilder("@");
        var first = true;
        foreach (var (key, value) in tags)
        {
            if (!first) sb.Append(';');
            first = false;
            sb.Append(key);
            if (value is not null)
            {
                sb.Append('=');
                sb.Append(TagEscape(value));
            }
        }
        sb.Append(' ');
        return sb.ToString();
    }

    /// <summary>
    /// Escape a tag value per IRCv3 message-tags spec.
    /// ; → \:, SPACE → \s, \ → \\, CR → \r, LF → \n
    /// </summary>
    public static string TagEscape(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace(";", "\\:")
            .Replace(" ", "\\s")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }

    /// <summary>
    /// Unescape a tag value per IRCv3 message-tags spec.
    /// </summary>
    public static string TagUnescape(string value)
    {
        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '\\' && i + 1 < value.Length)
            {
                switch (value[i + 1])
                {
                    case ':': sb.Append(';'); i++; break;
                    case 's': sb.Append(' '); i++; break;
                    case '\\': sb.Append('\\'); i++; break;
                    case 'r': sb.Append('\r'); i++; break;
                    case 'n': sb.Append('\n'); i++; break;
                    default: sb.Append(value[i]); break;
                }
            }
            else
            {
                sb.Append(value[i]);
            }
        }
        return sb.ToString();
    }
}
