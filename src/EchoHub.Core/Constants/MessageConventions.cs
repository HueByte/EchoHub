using System.Diagnostics.CodeAnalysis;

namespace EchoHub.Core.Constants;

/// <summary>
/// Cross-protocol message conventions. Action messages (<c>/me</c>) use the IRC CTCP ACTION
/// wire format 0x01 + "ACTION " + text + 0x01 as the stored content, so IRC clients
/// interoperate natively (irssi's /me arrives in exactly this shape) and the TUI renders
/// "* nick text". In end-to-end encrypted rooms the marker encrypts along with the text.
/// </summary>
public static class MessageConventions
{
    public const string ActionPrefix = "\u0001ACTION ";
    public const string ActionSuffix = "\u0001";

    public static string FormatAction(string text) => $"{ActionPrefix}{text}{ActionSuffix}";

    public static bool TryParseAction(string content, [NotNullWhen(true)] out string? actionText)
    {
        if (content.Length > ActionPrefix.Length + ActionSuffix.Length
            && content.StartsWith(ActionPrefix, StringComparison.Ordinal)
            && content.EndsWith(ActionSuffix, StringComparison.Ordinal))
        {
            actionText = content[ActionPrefix.Length..^ActionSuffix.Length];
            return actionText.Length > 0;
        }

        actionText = null;
        return false;
    }
}
