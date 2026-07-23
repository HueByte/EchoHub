# EmojiHelper

> **File:** `src/EchoHub.Client/UI/Helpers/EmojiHelper.cs`  
> **Kind:** class

```csharp
public static class EmojiHelper
```


EmojiHelper is a static utility that converts emoji grapheme clusters in a string into text shortcodes for safe rendering in terminal-based UIs. It scans input text, splits it into grapheme elements, and replaces any grapheme containing emoji with a corresponding shortcode from `EmojiShortcodes`; if no mapping exists for the full grapheme, it attempts the base emoji (the first rune) and uses its shortcode; if that also fails, it inserts a generic `[emoji]` placeholder. Non-emoji text passes through unchanged. This approach avoids inconsistent emoji rendering across terminals by providing fixed-width ASCII representations for display-only outputs.

## Remarks
EmojiHelper centralizes the emoji-to-shortcode conversion, isolating terminal rendering concerns from application logic. It relies on `EmojiShortcodes` for mapping and uses `StringInfo.GetTextElementEnumerator` to respect grapheme boundaries, ensuring sequences like complex emoji are treated coherently. The abstraction keeps emoji translation testable and swapable, so you can adjust shortcodes without touching UI code.

## Notes
- Unknown emoji yields a generic `[emoji]` placeholder; ensure `EmojiShortcodes` covers targets or plan fallback behavior.
- Emoji detection uses a set of Unicode ranges to decide whether a grapheme contains emoji; new or platform-specific emoji outside these ranges may be missed.
- This replacement is intended for display only; do not rely on reversibility for data persistence, and be aware that updates to `EmojiShortcodes` may change outputs.