# IrcOptions

> **File:** `src/EchoHub.Server.Irc/IrcOptions.cs`  
> **Kind:** class

```csharp
public sealed class IrcOptions
```


IrcOptions is a simple configuration container that aggregates the settings controlling EchoHub's IRC bridge. It exposes toggles and values for enabling IRC, selecting ports for non-TLS and TLS connections, TLS certificate details, the server identity, an optional MOTD, and how attachment URLs are resolved via a public base URL. An application binds this object from configuration to influence how the IRC integration is started and how clients connect securely.

## Remarks
This class acts as a plain data container that centralizes IRC-related settings, separating configuration concerns from connection logic. The SectionName constant indicates the configuration section used when binding settings, while PublicBaseUrl affects how attachment URLs are translated for IRC clients—absolute URLs when set, otherwise relative paths. It is designed to be a simple DTO bound from configuration rather than responsible for validation or side effects.

## Notes
- If TLS is enabled but a certificate path or password is missing or invalid, TLS connections may fail; ensure a valid certificate and credentials are supplied when TlsEnabled is true.
- PublicBaseUrl, when set, makes attachment URLs absolute for IRC clients; if left unset, attachment lines fall back to the relative path.
- The defaults describe typical behavior: Port = 6667, TlsPort = 6697, and ServerName = "echohub".