# ValidationConstants

> **File:** `src/EchoHub.Core/Constants/ValidationConstants.cs`  
> **Kind:** class

```csharp
public static partial class ValidationConstants
```


ValidationConstants is a centralized, static container for validation constraints used throughout the EchoHub.Core domain. It defines reusable patterns for usernames, channel names, and hex color codes, as well as a set of length limits governing passwords, display names, bios, statuses, channel topics, and chat history. The included GeneratedRegex methods expose precompiled Regex instances derived from those patterns, enabling fast, consistent validation without incurring per-call regex compilation.

## Remarks
ValidationConstants provides a single source of truth for input validation. By offloading regex compilation to source generation, it avoids runtime overhead while keeping the validation rules easily discoverable and consistent across the codebase.

The class is static and partial, so callers simply reference ValidationConstants.UsernameRegex(), ValidationConstants.ChannelNameRegex(), and ValidationConstants.HexColorRegex() to obtain ready-to-use Regex instances.

## Notes
- GeneratedRegex provides compile-time-compiled Regex instances, which improves performance by avoiding repeated regex compilation at runtime.
- Updating any constraint here propagates the change to all validation sites, ensuring consistency; do not duplicate rules elsewhere.