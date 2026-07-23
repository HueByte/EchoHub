# IrcOptions

> **File:** `src/EchoHub.Server.Irc/IrcOptions.cs`  
> **Kind:** class

```csharp
public sealed class IrcOptions
```


IrcOptions is a lightweight configuration container for the EchoHub IRC integration. It groups together all IRC-related settings that govern whether the IRC feature is active, which ports to listen on for plain and TLS connections, optional TLS credentials, the IRC server identity, an optional Motd, and how attachment URLs are resolved for IRC clients. This class is typically populated from the `Irc` configuration section (as indicated by the `SectionName` constant) and consumed by the startup logic that initializes the IRC subsystem, allowing developers to tailor IRC behavior without touching runtime code.

## Remarks
IrcOptions is a pure data carrier with defaults that reflect common IRC conventions: `Port` defaults to 6667, `TlsPort` to 6697, and [`ServerName`](IrcCommandHandler.cs.md) to `echohub`. TLS-related fields (`TlsEnabled`, `TlsPort`, `TlsCertPath`, `TlsCertPassword`) indicate TLS support is optional and configured here; the runtime code uses these values to establish TLS-protected connections when enabled. The `PublicBaseUrl` property governs how attachment URLs are rendered for IRC clients: when set, it converts relative paths to absolute links using the provided base URL; when unset, attachments fall back to their relative paths. The `Motd` field exposes an optional IRC message of the day that can be surfaced to connected clients if the IRC subsystem is started.