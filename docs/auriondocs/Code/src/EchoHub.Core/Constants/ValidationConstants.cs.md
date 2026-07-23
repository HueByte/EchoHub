# ValidationConstants

> **File:** `src/EchoHub.Core/Constants/ValidationConstants.cs`  
> **Kind:** class

```csharp
public static partial class ValidationConstants
```


ValidationConstants is a centralized repository of validation rules used across the codebase. It defines the canonical pattern strings for usernames, channel names, and hex colors, together with numeric bounds for various user-facing fields. Specifically, it exposes the strings `UsernamePattern`, `ChannelNamePattern`, `HexColorPattern`, and several limit constants such as `MaxPasswordLength`, `MinChannelPasswordLength`, `MaxDisplayNameLength`, `MaxBioLength`, `MaxStatusMessageLength`, `MaxChannelTopicLength`, and `MaxHistoryCount`. In addition, it provides precompiled Regex accessors via the `GeneratedRegex`-decorated methods `UsernameRegex()`, `ChannelNameRegex()`, and `HexColorRegex()`, enabling fast, centralized validation without scattering literal patterns across call sites.

## Remarks
By centralizing these constraints, `ValidationConstants` minimizes drift in validation rules across features (sign-up, profile updates, channel creation, etc.) and makes it easy to update rules in one place. The `UsernameRegex()`, `ChannelNameRegex()`, and `HexColorRegex()` methods are generated at compile time by the `GeneratedRegex` attribute, which yields ready-to-use, presumably cached `Regex` instances, reducing runtime regex compilation overhead at validation points.

## Notes
- GeneratedRegex-based accessors rely on C# source generation; ensure your project enables source generators and targets a compatible framework, otherwise these methods may not be produced.
- The constants define the canonical validation boundaries pharmacologically used by the system; changing them updates all consumers that reference these values.
