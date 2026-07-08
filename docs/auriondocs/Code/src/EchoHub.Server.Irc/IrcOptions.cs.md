# IrcOptions

> **File:** `src/EchoHub.Server.Irc/IrcOptions.cs`  
> **Kind:** class

```csharp
public sealed class IrcOptions
```


IrcOptions is a small configuration object that holds all the settings required to establish an IRC connection for EchoHub. Use it when configuring the IRC integration—bind from app settings or construct it programmatically—instead of wiring each setting separately at call sites.

## Remarks
IrcOptions serves as the configuration boundary between the hosting application and the IRC transport. The SectionName constant suggests it can be bound directly from a configuration section named 'Irc', allowing defaults to be overridden by config. TLS-related fields are optional; when TlsEnabled is true, provide a certificate path and password as needed, otherwise they are ignored.

## Example
```csharp
var options = new IrcOptions
{
    Enabled = true,
    Port = 6667,
    TlsEnabled = true,
    TlsPort = 6697,
    TlsCertPath = "/path/to/irc-cert.pem",
    TlsCertPassword = "secret",
    ServerName = "echohub",
    Motd = "Welcome to EchoHub IRC"
};
```

## Notes
- If TLS is enabled, ensure the TLS certificate path (TlsCertPath) is valid and accessible; otherwise the TLS handshake may fail.
- The defaults reflect common IRC values (Port 6667, TLS port 6697, ServerName "echohub"); override as needed for your environment.
- Motd is optional and can be left null if no message should be advertised upon connect.