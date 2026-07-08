# ChatColors

> **File:** `src/EchoHub.Client/UI/Chat/ChatColors.cs`  
> **Kind:** class

```csharp
public static partial class ChatColors
```


Shared, reuseable color attributes and a small text-splitting helper used when rendering chat. Use the static Attribute fields for a consistent palette (timestamps, system messages, mentions, channel references, embeds, etc.). Call SplitMentions when you need to break a single chat string into ChatSegment tokens where @mentions and #channels receive their accent colors and the remainder of the text is left with the supplied default color (or null if none is provided).

## Remarks
This class centralizes the visual accents used across the chat UI so renderers can apply consistent colors without constructing attributes inline. SplitMentions performs a two-pass split: it first finds @mentions (so email-like text isn't treated as mentions), marks those segments with the mention color, and then scans the remaining (non-mention) segments for #channel references. The regexes are intentionally conservative — mentions must not be preceded by a word character (avoids matching inside emails), and channel refs must include at least one letter (avoids matching hex color codes or numeric-only tokens).

## Example
```csharp
// Break a message into colored segments; provide a default color for normal text
var segments = ChatColors.SplitMentions(
    "Hello @alice, please check #general for updates",
    ChatColors.EmbedDescAttr);
// `segments` is a List<ChatSegment> with mention and channel tokens colored
```

## Notes
- SplitMentions accepts an optional defaultColor; if you pass null (or omit the parameter) the ChatSegment instances produced for ordinary text will carry a null color and renderers should handle that case.
- The method colors @mentions first and then scans non-mention text for #channels to avoid overlapping matches (e.g., a channel name inside a mention will not be double-colored).
- The channel regex requires at least one letter to reduce false positives (it will not match pure numbers or hex-like tokens).

