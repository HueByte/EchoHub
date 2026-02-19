namespace EchoHub.Server.Irc;

/// <summary>
/// Parsed representation of an IRC protocol line.
/// Format: [:prefix] COMMAND [params...] [:trailing]
/// </summary>
public sealed class IrcMessage
{
    public string? Prefix { get; init; }
    public string Command { get; init; } = "";
    public List<string> Parameters { get; init; } = [];

    public string? Trailing => Parameters.Count > 0 ? Parameters[^1] : null;

    /// <summary>
    /// Parse a raw IRC line: [:prefix SPACE] command [SPACE params] CRLF
    /// </summary>
    public static IrcMessage Parse(string line)
    {
        var span = line.AsSpan().TrimEnd("\r\n");
        string? prefix = null;
        var pos = 0;

        // Parse optional prefix
        if (span.Length > 0 && span[0] == ':')
        {
            var spaceIdx = span.IndexOf(' ');
            if (spaceIdx == -1)
                return new IrcMessage { Prefix = span[1..].ToString() };

            prefix = span[1..spaceIdx].ToString();
            pos = spaceIdx + 1;
        }

        // Skip whitespace
        while (pos < span.Length && span[pos] == ' ') pos++;

        // Parse command
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
            Prefix = prefix,
            Command = command,
            Parameters = parameters,
        };
    }
}
