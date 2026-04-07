# Configuration

EchoHub Server generates an `appsettings.json` with sensible defaults on first run (including a random JWT secret), so you can launch and start chatting immediately. Tweak things later when you feel like it.

> [!NOTE]
> Under the hood, EchoHub Server is built on ASP.NET Core, so it inherits the standard .NET configuration system. If you're familiar with that, everything works exactly as you'd expect. If not — no worries, this page covers everything you need.

## How It Works

EchoHub Server loads settings from multiple sources. Each source **overrides** the previous one, so you can layer defaults with environment-specific values:

```text
1. appsettings.json                   (base defaults)
2. appsettings.{Environment}.json     (e.g. appsettings.Production.json)
3. Environment variables              (great for Docker / CI)
4. Command-line arguments             (highest priority)
```

The last one wins. If `appsettings.json` says `"Irc:Port": 6667` but you pass `--Irc:Port=7000` on the command line, port 7000 is what you get.

In practice this means you can leave `appsettings.json` alone and override just the settings you care about using environment variables or CLI flags — no need to edit JSON files if that's not your thing.

### Environment Variable Mapping

Environment variables use **double underscores** (`__`) in place of the JSON nesting. The rule is simple — replace every `:` (or each level of JSON nesting) with `__`:

| appsettings.json path | Environment variable |
| --- | --- |
| `Server:Name` | `Server__Name` |
| `Irc:Enabled` | `Irc__Enabled` |
| `Jwt:Secret` | `Jwt__Secret` |
| `Serilog:MinimumLevel:Default` | `Serilog__MinimumLevel__Default` |
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` |

Arrays use numeric indices: `Server:Admins:0` becomes `Server__Admins__0`, `Server:Admins:1` becomes `Server__Admins__1`, and so on.

This is why the Docker `.env` file uses `Server__Name=My Server` instead of JSON — Docker passes these as environment variables, and the server picks them up automatically.

### Examples

All three of these achieve the same thing — use whichever fits your setup.

**appsettings.json** (direct editing):

```json
{
  "Server": {
    "Name": "My EchoHub Server",
    "PublicServer": true
  }
}
```

**Environment variables** (Docker, systemd, shell export):

```bash
export Server__Name="My EchoHub Server"
export Server__PublicServer=true
```

**Command-line arguments** (quick overrides, highest priority):

```bash
./EchoHub.Server --Server:Name="My EchoHub Server" --Irc:Enabled=true
```

## Configuration Reference

The full `appsettings.json` is auto-generated on first run from the [example config](https://github.com/HueByte/EchoHub/blob/master/src/EchoHub.Server/appsettings.example.json). Here's every option:

### General

| Key | Default | Description |
| --- | --- | --- |
| `Urls` | `http://0.0.0.0:5000` | Listen address and port |
| `AllowedHosts` | `*` | Allowed host headers (leave `*` unless you need host filtering) |

### Database

| Key | Default | Description |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | *(empty)* | SQLite connection string. Empty = `echohub.db` in the app directory |

### Authentication

| Key | Default | Description |
| --- | --- | --- |
| `Jwt:Secret` | *(auto-generated)* | Signing key (min 32 chars). Auto-generated on first run |
| `Jwt:Issuer` | `EchoHub.Server` | JWT issuer claim |
| `Jwt:Audience` | `EchoHub.Client` | JWT audience claim |

Access tokens expire after 15 minutes, refresh tokens after 30 days with rotation on each use.

### Server Identity

| Key | Default | Description |
| --- | --- | --- |
| `Server:Name` | `My EchoHub Server` | Display name shown to clients |
| `Server:Description` | `A self-hosted EchoHub chat server` | Server description |
| `Server:PublicServer` | `false` | Register on the [public directory](https://echohub.voidcube.cloud/servers) |
| `Server:PublicHost` | *(empty)* | Public hostname for the directory listing (e.g. `chat.example.com:5000`) |
| `Server:Admins` | `[]` | Array of admin usernames (e.g. `["alice", "bob"]`) |

### Encryption

| Key | Default | Description |
| --- | --- | --- |
| `Encryption:Key` | *(auto-generated)* | AES key for message encryption in transit |
| `Encryption:EncryptDatabase` | `false` | Also encrypt message content at rest in SQLite |

### Storage

| Key | Default | Description |
| --- | --- | --- |
| `Storage:CleanupIntervalHours` | `1` | How often the cleanup job runs (hours) |
| `Storage:RetentionDays` | `30` | Days to keep uploaded files before cleanup |

### IRC Gateway

| Key | Default | Description |
| --- | --- | --- |
| `Irc:Enabled` | `false` | Enable the IRC protocol gateway |
| `Irc:Port` | `6667` | IRC plain-text listen port |
| `Irc:TlsEnabled` | `false` | Enable TLS termination for IRC |
| `Irc:TlsPort` | `6697` | IRC TLS listen port |
| `Irc:TlsCertPath` | *(empty)* | Path to a PKCS#12 (`.pfx`) certificate |
| `Irc:TlsCertPassword` | *(empty)* | Password for the certificate file |
| `Irc:ServerName` | `echohub` | IRC server name in protocol messages |
| `Irc:Motd` | `Welcome to EchoHub IRC Gateway!` | Message of the day |

### Logging

EchoHub uses [Serilog](https://serilog.net/) for structured logging — console output + daily rolling files with 14-day retention by default.

| Key | Default | Description |
| --- | --- | --- |
| `Serilog:MinimumLevel:Default` | `Information` | Global log level (`Debug`, `Information`, `Warning`, `Error`) |
| `Serilog:MinimumLevel:Override:Microsoft` | `Warning` | Suppress noisy framework logs |
| `Serilog:MinimumLevel:Override:Microsoft.AspNetCore` | `Warning` | Suppress request pipeline logs |
| `Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore` | `Warning` | Suppress database query logs |

Log files are written to `logs/echohub-server-YYYY-MM-DD.log`. To change the path or retention, edit the `Serilog:WriteTo` section in `appsettings.json`.

Want more verbose output for debugging? Set the minimum level to `Debug`:

```bash
# via environment variable
export Serilog__MinimumLevel__Default=Debug

# or command line
./EchoHub.Server --Serilog:MinimumLevel:Default=Debug
```
