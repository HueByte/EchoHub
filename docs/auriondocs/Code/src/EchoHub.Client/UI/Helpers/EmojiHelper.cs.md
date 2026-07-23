# EmojiHelper

> **File:** `src/EchoHub.Client/UI/Helpers/EmojiHelper.cs`  
> **Kind:** class

```csharp
public static class EmojiHelper
```


EmojiHelper converts emoji grapheme clusters to text shortcodes for safe TUI rendering. It replaces emoji with fixed-width ASCII shortcodes when available, falling back to a generic [emoji] placeholder for unknown symbols; non-emoji text passes through unchanged.

## Remarks

This utility uses grapheme-aware processing to handle complex emoji sequences (including ZWJ-joined glyphs and modifier-bearing emojis) by iterating over text elements rather than individual code points. It first attempts a full-grapheme shortcode lookup, then falls back to the base emoji (the first rune of the grapheme) if necessary, and finally uses the [emoji] placeholder when no mapping exists. An initial pass quickly determines whether any emoji exist in the input to avoid unnecessary work. The implementation relies on StringBuilder for efficient string construction, StringInfo for grapheme segmentation, and the EmojiShortcodes mapping as the source of truth for replacements.

## Notes

- Unknown or unmapped emoji are replaced with [emoji], which can reduce expressiveness if the shortcode dictionary is incomplete. Ensure EmojiShortcodes covers the emoji you expect to render in your UI.
