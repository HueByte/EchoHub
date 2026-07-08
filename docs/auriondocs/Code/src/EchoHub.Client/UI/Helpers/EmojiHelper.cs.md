# EmojiHelper

> **File:** `src/EchoHub.Client/UI/Helpers/EmojiHelper.cs`  
> **Kind:** class

```csharp
public static class EmojiHelper
```


EmojiHelper.ReplaceEmoji converts emoji grapheme clusters in text into :shortcode: equivalents for safe TUI rendering. Terminal width calculations for emoji are unreliable across terminals, so we replace them with fixed-width ASCII shortcodes for display-only purposes; non-emoji text passes through unchanged.

## Remarks
EmojiHelper operates on grapheme clusters to preserve user-perceived text while substituting emoji with stable placeholders. It performs a quick pre-check to skip processing when no emoji is present, then iterates text elements using StringInfo.GetTextElementEnumerator to respect grapheme boundaries. For each grapheme that contains emoji, it first attempts to map the entire grapheme via the EmojiShortcodes mapping; if that fails, it derives a base emoji (first rune with modifiers/ZWJ stripped) and tries again. If no mapping exists, a generic "[emoji]" placeholder is emitted. This design enables predictable text layout in text-based UIs while leveraging available emoji mappings when present.

## Notes
- EmojiShortcodes must be populated for meaningful replacements; otherwise many emoji will render as [emoji].
- The algorithm relies on grapheme boundaries and a base-emoji fallback to handle common composed sequences; edge cases may still produce placeholders.
- IsEmojiRune uses a broad, heuristic set of Unicode ranges; new emoji may not be detected until the ranges are updated.
- For very long inputs, there is a linear pass plus per-grapheme processing; keep an eye on performance in tight UI loops.