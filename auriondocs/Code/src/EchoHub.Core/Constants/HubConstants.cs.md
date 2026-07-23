# HubConstants

> **File:** `src/EchoHub.Core/Constants/HubConstants.cs`  
> **Kind:** class

```csharp
public static class HubConstants
```


HubConstants acts as the single source of truth for the chat hub’s configurable limits and defaults. It groups static, compile-time constants that govern where the hub is exposed, how sessions are identified (including the IRC gateway prefix), and the upper bounds for messages, attachments, avatars, and embeds, providing a centralized reference that other components consult for validation and formatting.

## Remarks
HubConstants isolates cross-cutting numerical constraints from business logic, ensuring all parts of the EchoHub system enforce the same rules. It enables tuning by operators—e.g., increasing `MaxMessageLength` or `MaxAttachmentsPerMessage`—without altering core workflows, while the IRC connection-id prefix helps the presence tracker distinguish IRC-based clients from native ones. The constants also centralize embed sizing and fetch behavior to maintain predictable link previews and resource usage across gateways and clients.

## Example
```csharp
// Validate message length against hub-wide limit
if (message.Text.Length > HubConstants.MaxMessageLength)
{
    // handle too long
}

// Build the path for the chat hub
var hubPath = HubConstants.ChatHubPath;
```

## Notes
- They are compile-time constants (const) and thus require a recompilation to change; runtime configuration is not supported.
- Changes to these values reflect architectural expectations across components (UI, gateway, presence tracker, and embeds) and should be coordinated to avoid breaking client assumptions.