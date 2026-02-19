# EchoHub

<p align="center">
  <img src="https://cdn.voidcube.cloud/assets/hue_icon.svg" alt="EchoHub Logo" width="120" />
</p>

<h1 align="center">EchoHub</h1>

<p align="center">
  No tracking. No subscriptions. No "enhanced AI features". Just chat.
</p>

<p align="center">
  <a href="#what-is-this">What</a> •
  <a href="#getting-started">Setup</a> •
  <a href="#irc-gateway">IRC</a> •
  <a href="#deployment-with-nginx">Deploy</a> •
  <a href="#client-commands">Commands</a> •
  <a href="#configuration">Config</a> •
  <a href="#license">License</a>
</p>

<p align="center">
  <a href="https://echohub.voidcube.cloud/">Website</a> •
  <a href="https://echohub.voidcube.cloud/servers">Public Servers</a> •
  <a href="https://huebyte.github.io/EchoHub/">Documentation</a>
</p>

<p align="center">
  <img alt=".NET 10" src="https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet&logoColor=white" />
  <img alt="SignalR" src="https://img.shields.io/badge/SignalR-Real--time-0078D4?style=flat-square" />
  <img alt="SQLite" src="https://img.shields.io/badge/SQLite-EF%20Core-003B57?style=flat-square&logo=sqlite&logoColor=white" />
  <img alt="License" src="https://img.shields.io/badge/License-MIT-green?style=flat-square" />
  <img alt="Terminal.Gui" src="https://img.shields.io/badge/TUI-Terminal.Gui%20v2-yellow?style=flat-square" />
  <img alt="Electron" src="https://img.shields.io/badge/Electron-None-red?style=flat-square" />
</p>

---

## What is this?

Chat apps used to be simple. You connected to a server, joined a channel, and talked to people. No one was mining your messages for ad targeting, no one was selling your "engagement metrics", and the app didn't need 2GB of RAM to display text.

EchoHub is a return to that. Self-hosted, IRC-inspired chat that runs in your terminal. You own the server, you own the data, and the client won't try to upsell you on a premium tier.

Each server is fully independent — no central authority, no account federation, no corporate overlord. Just spin one up and go.

```mermaid
graph TD
    subgraph Server["Server"]
        ChatSvc["ChatService"]
        Hub["SignalR ChatHub"]
        IRC["IRC Gateway :6667"]
        Auth["JWT Auth"]
        DB["SQLite DB (EF Core)"]
        Files["File Storage"]
    end

    subgraph Clients["Clients"]
        TUI["Terminal GUI (TUI)"]
        IRCClient["IRC Client (irssi, WeeChat, ...)"]
    end

    TUI -- "WebSocket" --> Hub
    TUI -- "REST" --> Auth
    IRCClient -- "TCP" --> IRC
    Hub --> ChatSvc
    IRC --> ChatSvc
    ChatSvc --> DB
    Auth --> DB
    Files --> DB
```

## What you get

### Server

- **Self-hostable** — your server, your rules, your data
- **Real-time messaging** via SignalR WebSockets
- **IRC gateway** — native IRC clients connect alongside TUI users, full cross-protocol messaging
- **JWT auth** with short-lived access tokens and 30-day refresh tokens
- **Channels** — create, set topics, delete (no 47-step permission wizard required)
- **File & image uploads** with actual validation (magic bytes, not just trusting the extension)
- **Image-to-ASCII** — because images in a terminal is objectively cool
- **Presence tracking** — online/away/DND/invisible with custom status messages
- **Rate limiting** — in case someone gets too excited
- **Auto-restart** on crash with exponential backoff — it picks itself back up
- **Serilog logging** — console + rolling file, because `Console.WriteLine` isn't a logging strategy
- **Zero config first run** — generates its own JWT secret and config on launch

### Client

- **Runs in your terminal** — no browser, no Electron, no 500MB of bundled Chromium
- **13 built-in themes** — including `hacker` for when you want to feel like you're in a movie
- **Slash commands** — `/join`, `/send`, `/status`, `/theme`, etc.
- **Colored nicknames** — pick your hex color, express yourself
- **File/image sharing** — local files or URLs
- **Multi-server** — save and switch between servers
- **Auto-reconnect** — drops happen, it rejoins your channels automatically
- **Message history** on join — you won't miss context

## Getting Started

### Download

Grab a self-contained binary from [Releases](../../releases) — no runtime needed, just run it.

### Prerequisites (for development)

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Run the Server

```bash
dotnet run --project src/EchoHub.Server
```

First run does everything for you:

1. Creates `appsettings.json` from the example config
2. Generates a secure JWT secret
3. Creates the database with a `#general` channel

### Run the Client

```bash
dotnet run --project src/EchoHub.Client
```

Connect, register, chat. That's the whole onboarding flow.

### Build from Source

```bash
dotnet build src/EchoHub.slnx
```

## IRC Gateway

EchoHub includes a built-in IRC protocol gateway. Any standard IRC client can connect to the same server and chat alongside TUI users — messages flow both ways in real time.

### Enable It

In the server's `appsettings.json`:

```json
{
  "Irc": {
    "Enabled": true,
    "Port": 6667,
    "ServerName": "echohub",
    "Motd": "Welcome to EchoHub IRC Gateway!"
  }
}
```

### Connect

```bash
# irssi
irssi -c your-server.com -p 6667 -w <password> -n <username>

# WeeChat
/server add echohub your-server.com/6667 -password=<password> -nicks=<username>
/connect echohub
```

IRC users must have an existing EchoHub account (no registration via IRC). Auth works via `PASS`/`NICK`/`USER` or SASL PLAIN.

### What Works

| Feature | How it maps to IRC |
| ------- | ------------------ |
| Text messages | Standard `PRIVMSG` (long messages split at ~400 byte chunks) |
| Images | `[Image: filename]` + download URL + ASCII art line-by-line |
| File uploads | `[File: filename] /api/files/{id}` |
| Channels | `JOIN`, `PART`, `NAMES`, `TOPIC`, `LIST` |
| Presence | `AWAY`, `WHO`, `WHOIS` |
| Status | Maps to IRC away/here |

### TLS

If running behind **nginx** (recommended), let nginx handle TLS -- see [Deployment with nginx](#deployment-with-nginx) below.

For direct TLS without a reverse proxy, the IRC gateway can terminate TLS itself:

```json
{
  "Irc": {
    "TlsEnabled": true,
    "TlsPort": 6697,
    "TlsCertPath": "/path/to/cert.pfx",
    "TlsCertPassword": "your-password"
  }
}
```

## Client Commands

| Command | Description |
| ------- | ----------- |
| `/join <channel>` | Join a channel |
| `/leave` | Leave current channel |
| `/topic <text>` | Set channel topic (creator only) |
| `/send <file or URL>` | Upload a file or image |
| `/status <online\|away\|dnd\|invisible>` | Set your status |
| `/status <message>` | Set a status message |
| `/nick <name>` | Set display name |
| `/color <#hex>` | Set nickname color |
| `/theme <name>` | Switch theme |
| `/profile` | Open profile editor |
| `/users` | List online users in channel |
| `/servers` | Manage saved servers |
| `/help` | Show help |
| `/quit` | Exit |

## Themes

`/theme <name>` to switch:

| Theme | Vibe |
| ----- | ---- |
| `default` | Gray on black — clean and quiet |
| `transparent` | White on black — for fancy transparent terminals |
| `classic` | White on blue — IRC nostalgia |
| `light` | Black on white — for the brave |
| `hacker` | Green on black — *I'm in* |
| `solarized` | Cyan/yellow on dark gray — for the refined |
| `dracula` | Purple accents on black — the classic dark theme |
| `monokai` | Yellow highlights on black — warm and familiar |
| `nord` | Cool blues — arctic vibes |
| `gruvbox` | Earthy yellows on black — retro warmth |
| `ocean` | Cyan on deep blue — underwater aesthetics |
| `highcontrast` | Bright yellow on black — maximum readability |
| `rosepine` | Muted pinks on black — cozy and soft |

## Configuration

`appsettings.json` is auto-generated on first run. Tweak what you need:

| Key | Default | Description |
| --- | ------- | ----------- |
| `Urls` | `http://0.0.0.0:5000` | Listen address and port |
| `ConnectionStrings:DefaultConnection` | *(empty — app directory)* | SQLite connection string |
| `Jwt:Secret` | *(auto-generated)* | JWT signing key |
| `Server:Name` | `My EchoHub Server` | Server display name |
| `Server:Description` | `A self-hosted EchoHub chat server` | Server description |
| `Server:PublicServer` | `false` | List on the [public directory](https://echohub.voidcube.cloud/servers) |
| `Server:PublicHost` | *(empty)* | Public hostname for directory listing |
| `Irc:Enabled` | `false` | Enable the IRC gateway |
| `Irc:Port` | `6667` | IRC listen port |
| `Irc:TlsEnabled` | `false` | Enable TLS for IRC |
| `Irc:TlsPort` | `6697` | IRC TLS port |
| `Irc:ServerName` | `echohub` | IRC server name in protocol messages |
| `Irc:Motd` | `Welcome to EchoHub IRC Gateway!` | Message of the day |
| `Cors:AllowedOrigins` | *(all origins)* | CORS whitelist |

Logging uses Serilog — console + daily rolling files with 14-day retention. Configure in the `Serilog` section.

## Deployment with nginx

Most production deployments run behind nginx. Here's a config that handles both the HTTP/WebSocket server and the IRC gateway:

```nginx
# HTTP + WebSocket (EchoHub Server API + SignalR)
server {
    listen 443 ssl;
    server_name echohub.example.com;

    ssl_certificate     /etc/letsencrypt/live/echohub.example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/echohub.example.com/privkey.pem;

    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # Required for SignalR WebSocket
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection $http_connection;

        proxy_read_timeout 86400s;
        proxy_send_timeout 86400s;
    }

    # Increase max upload size for file sharing
    client_max_body_size 10m;
}

# HTTP → HTTPS redirect
server {
    listen 80;
    server_name echohub.example.com;
    return 301 https://$host$request_uri;
}

# IRC TLS (port 6697 → plain IRC on 6667)
stream {
    upstream irc_backend {
        server 127.0.0.1:6667;
    }

    server {
        listen 6697 ssl;
        proxy_pass irc_backend;

        ssl_certificate     /etc/letsencrypt/live/echohub.example.com/fullchain.pem;
        ssl_certificate_key /etc/letsencrypt/live/echohub.example.com/privkey.pem;
    }
}
```

With this setup, keep the EchoHub IRC gateway's `TlsEnabled` set to `false` — nginx terminates TLS. See the full example at [`examples/nginx.conf`](examples/nginx.conf).

## Project Structure

```text
src/
├── EchoHub.Core/            # Shared models, DTOs, contracts, validation
│   ├── Constants/           # ValidationConstants, HubConstants
│   ├── Contracts/           # IChatService, IChatBroadcaster, IEchoHubClient
│   ├── DTOs/                # Record DTOs
│   └── Models/              # Entity models
│
├── EchoHub.Server/          # ASP.NET Core server
│   ├── Auth/                # JWT token service
│   ├── Controllers/         # REST API endpoints
│   ├── Data/                # EF Core DbContext + migrations
│   ├── Hubs/                # SignalR ChatHub
│   ├── Services/            # ChatService, presence, file storage, image processing
│   └── Setup/               # First-run setup, DB initialization
│
├── EchoHub.Server.Irc/      # IRC protocol gateway
│   ├── IrcGatewayService    # TCP listener (BackgroundService)
│   ├── IrcCommandHandler    # IRC command dispatch (JOIN, PRIVMSG, etc.)
│   ├── IrcBroadcaster       # Fans chat events to IRC connections
│   └── IrcMessageFormatter  # MessageDto → IRC PRIVMSG lines
│
├── EchoHub.Client/          # Terminal.Gui TUI client
│   ├── Config/              # Client configuration
│   ├── Services/            # API client, SignalR connection
│   ├── Themes/              # 13 built-in themes
│   └── UI/                  # MainWindow, dialogs, chat renderer
│
└── EchoHub.slnx             # Solution file
```

## License

[MIT](LICENSE) — do whatever you want with it.
