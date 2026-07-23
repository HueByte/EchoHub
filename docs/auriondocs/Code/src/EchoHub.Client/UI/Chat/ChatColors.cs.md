# ChatColors

> **File:** `src/EchoHub.Client/UI/Chat/ChatColors.cs`  
> **Kind:** class

```csharp
public static partial class ChatColors
```


Shared color attributes and a small parsing helper for chat rendering. Use this class when rendering chat UI elements (timestamps, system messages, mentions, channel references, embeds, attachments, etc.) so all parts of the UI use a consistent set of Attribute values. Call SplitMentions when you need to break a message into colored segments so mentions (@user) and channel references (#channel) can be rendered with their accent colors while non-special text uses a supplied default.

## Remarks
ChatColors centralizes the visual styling for chat components and includes a utility to split text into ChatSegment pieces that carry color information. SplitMentions performs a two-pass parse: first it finds @mentions (avoiding emails by requiring no preceding word character) and marks them with MentionTextAttr; then it examines the remaining, non-mention segments to find #channel references (the regex requires at least one letter to avoid matching hex colors or numeric issue references) and marks those with ChannelRefAttr. All Attribute instances are readonly and intended as shared, immutable style tokens that renderers can reuse.

## Notes
- The mention regex uses a negative lookbehind (?<!\w) so strings like "me@domain" are not treated as @mentions.  
- The channel regex requires at least one ASCII letter to avoid matching plain hex colors or purely numeric tokens.  
- SplitMentions accepts a nullable defaultColor; callers should handle null when rendering (null means "no explicit attribute supplied").