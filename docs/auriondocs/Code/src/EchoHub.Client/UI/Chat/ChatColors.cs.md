# ChatColors

> **File:** `src/EchoHub.Client/UI/Chat/ChatColors.cs`  
> **Kind:** class

```csharp
public static partial class ChatColors
```


Shared color attributes and small text-processing helpers used by the chat UI. Use `ChatColors` when you need a consistent set of `Attribute` values for things like timestamps, system messages, mentions, channel references, embeds and file/audio accents, or when you need to split a message into [`ChatSegment`](ChatSegment.cs.md)s that mark `@`-mentions and `#`-channel references for rendering.

## Remarks
`ChatColors` centralizes the visual palette and simple parsing rules for chat rendering so callers don't duplicate color choices or regex logic. The static `Attribute` fields (for example `TimestampAttr`, `SystemAttr`, `MentionTextAttr`, `ChannelRefAttr`, `RailAttr`, `DateRuleAttr`, and `UnreadMarkerAttr`) are intended to be reused by rendering code. The `SplitMentions` method performs a two-pass split: it first extracts `@`-mentions (giving them `MentionTextAttr`) and then, only inside segments that were not already colored as mentions, highlights `#`-channel references with `ChannelRefAttr`. The regex helpers are implemented via `GeneratedRegex` methods (`MentionRegex` and `ChannelRefRegex`) so they are compiled at build time.

## Example
```csharp
// Split a message and inspect segments; mention and channel fragments receive attributes
var message = "Hey @alice, check #general and #123 -- also email alice@example.com";
var segments = ChatColors.SplitMentions(message, defaultColor: null);

foreach (var seg in segments)
    Console.WriteLine($"[{seg.Color}] {seg.Text}");
```

## Notes
- `SplitMentions` treats the optional `defaultColor` as the fallback attribute for non-special text; passing `null` means segment `Color` values may be `null` and the caller must handle that when rendering.
- The `MentionRegex` uses `(?<!\w)@...` to avoid matching emails, and the `ChannelRefRegex` requires at least one ASCII letter to avoid matching hex colors or bare numbers (so some international or non-ASCII usernames/channels may not match).
- Mentions take precedence: because `SplitMentions` colors `@`-matches in the first pass, any `#` inside an already-colored mention will not be reprocessed in the second pass.