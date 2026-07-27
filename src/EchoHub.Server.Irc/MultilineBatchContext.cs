namespace EchoHub.Server.Irc;

/// <summary>
/// Tracks an in-progress multiline batch (draft/multiline) being collected
/// from an IRC client before dispatching as a single EchoHub message.
/// </summary>
public sealed class MultilineBatchContext(string referenceTag, string target)
{
    public string ReferenceTag { get; } = referenceTag;
    public string Target { get; } = target;
    public List<string> Lines { get; } = [];
    public bool UsesConcat { get; set; }
    public Guid? ReplyToMessageId { get; set; }
}
