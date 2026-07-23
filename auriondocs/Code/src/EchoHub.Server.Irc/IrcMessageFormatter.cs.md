# IrcMessageFormatter

> **File:** `src/EchoHub.Server.Irc/IrcMessageFormatter.cs`  
> **Kind:** class

```csharp
public static class IrcMessageFormatter
```


IrcMessageFormatter is a small, focused helper that converts a MessageDto into IRC PRIVMSG lines suitable for delivery in an IRC channel. It handles plain text and CTCP ACTION content, prefixes replies with the standard '> nick: snippet | ' format, and renders attachments as separate URL lines with concise type tags, using an absolute URL when a public base URL is supplied.

## Remarks

It centralizes the IRC-specific formatting and line-breaking logic used by the server when presenting messages to IRC clients, shielding callers from the quirks of the IRC protocol (such as per-line length limits and CTCP wrapping). The private FormatReplyPrefix creates a consistent context string for replies, including redaction of room ciphertext when needed and truncating long snippets to a safe length. Attachments are surfaced as individual lines with a small tag ([Image: ...], [Audio: ...], or [File: ...]) followed by an absolute URL, aligning with common IRC client behavior and improving link reliability. Embeds are appended using FormatEmbed, enabling rich previews where supported.

## Notes

- The FormatMessage path enforces line-length constraints via MaxIrcLineContentBytes, causing long content to be split across multiple PRIVMSG lines as needed.
- Encrypted-reply content is masked by the ciphertext-detection logic (e.g., [encrypted]) to avoid leaking room ciphertext in IRC.
- Absolute URL generation relies on ToAbsoluteUrl and the optional publicBaseUrl; without a base URL, attachments may render with their original (potentially relative) URLs.
