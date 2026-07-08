# HubConstants

> **File:** `src/EchoHub.Core/Constants/HubConstants.cs`  
> **Kind:** class

```csharp
public static class HubConstants
```


HubConstants centralizes all chat hub configuration values used throughout EchoHub. It exposes static, compile-time constants for the hub path, default channel, message/history limits, file and media size restrictions, ASCII art dimensions, and embed-related thresholds. This single source of truth prevents scattered literals and makes global policy changes straightforward.

## Remarks

Because the class is static and non-instantiable, consumers reference HubConstants directly to read invariant values. This design enforces consistency across validation, UI, and transport layers that rely on the same limits and identifiers (for example, MaxMessageLength and EmbedMaxHtmlBytes). It also communicates intent: these values are intended to be globally applicable defaults rather than per-call configuration, and any changes should be coordinated with client expectations.

## Notes

- Changing a constant can impact both server-side validation and client UX; coordinate with frontend teams and tests.
- Some limits are expressed in bytes; when calculating from user input, be mindful of encoding length differences (e.g., UTF-8).
- Constants are not environment-configurable at runtime; for per-environment overrides, introduce a separate configuration layer.