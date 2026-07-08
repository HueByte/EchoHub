# ValidationConstants

> **File:** `src/EchoHub.Core/Constants/ValidationConstants.cs`  
> **Kind:** class

```csharp
public static partial class ValidationConstants
```


ValidationConstants is a static, partial container of shared validation rules for EchoHub.Core. It centralizes string patterns for usernames, channel names, and hex colors, alongside maximum lengths for common text fields, and provides precompiled Regex accessors (UsernameRegex, ChannelNameRegex, HexColorRegex) generated from those patterns for fast, reusable validation.

## Remarks
This abstraction consolidates validation policy in one place, enabling updates to rules without touching multiple validators scattered across the codebase. The GeneratedRegex-annotated accessors provide compiled Regex instances, reducing allocation and improving startup performance; the partial class allows extending the constants elsewhere without modifying the original file.

## Notes
- The Regex accessors are generated at compile time; changes to the underlying patterns require a rebuild to take effect.
- The length constants define domain boundaries that should be enforced in UI and storage to prevent invalid input and potential abuse.