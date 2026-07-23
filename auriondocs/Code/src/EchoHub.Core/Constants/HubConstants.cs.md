# HubConstants

> **File:** `src/EchoHub.Core/Constants/HubConstants.cs`  
> **Kind:** class

```csharp
public static class HubConstants
```


HubConstants is a static container for global constants used by the chat hub to configure limits, paths, and feature boundaries. It provides values such as the hub path, default channel, and various size and constraint limits, ensuring consistent behavior across components and avoiding scattered magic numbers.

## Remarks
HubConstants centralizes cross-cutting, tunable values so changes propagate consistently across messaging validation, content embedding, and endpoint configuration. Because these are compile-time constants, they are not sourced from runtime configuration; if you need different behavior per deployment, introduce a separate configuration mechanism rather than altering these constants at runtime.

## Notes
- The distinction between MaxMessageNewlines (30) and MaxConsecutiveNewlines (1) matters: the first limits overall newline usage, the second limits consecutive newline runs.
- Size limits are per-file (e.g., MaxImageSizeBytes, MaxAudioFileSizeBytes, MaxFileSizeBytes) and guide validation and storage decisions; never assume a single cap covers all attachment types.
- IrcConnectionIdPrefix is used by the presence tracker to distinguish IRC gateway connections from native SignalR clients; ensure prefix checks rather than simple contains checks to avoid misclassification.