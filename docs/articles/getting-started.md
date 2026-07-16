# Getting Started

## Install the Client

### Windows (Chocolatey)

```bash
choco install echohub
```

### Linux / macOS

```bash
curl -sSfL https://raw.githubusercontent.com/HueByte/EchoHub/master/scripts/install.sh | sh
```

To install a specific version or to a custom directory:

```bash
curl -sSfL .../install.sh | sh -s -- --version 0.2.14
curl -sSfL .../install.sh | sh -s -- --install-dir /opt/echohub
```

### Manual Download

Grab a self-contained binary from [Releases](https://github.com/HueByte/EchoHub/releases) -- no runtime needed.

## Host a Server

### Docker

The quickest way to host a server:

```bash
cp .env.example .env
docker compose up -d
```

See the [Docker guide](docker.md) for configuration, pre-built images, and more.

### From Source

```bash
dotnet run --project src/EchoHub.Server
```

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download).

On first run, the server automatically:

1. Creates `appsettings.json` from the example config
2. Generates a secure JWT secret
3. Creates the SQLite database with a `#general` channel

## Usage

After installing the client, run `echohub` (or `dotnet run --project src/EchoHub.Client` from source).
Connect to a server, register an account, and start chatting.

## Connect via IRC

Enable the IRC gateway in the server's `appsettings.json`:

```json
{
  "Irc": {
    "Enabled": true,
    "Port": 6667
  }
}
```

Then connect with any standard IRC client:

```bash
irssi -c localhost -p 6667 -w <password> -n <username>
```

Your nick is your EchoHub username and the server password is your account password (`PASS`/`NICK`/`USER` or SASL PLAIN). Connecting with a new username registers the account. Messages flow bidirectionally between IRC and TUI clients.

For TLS, set `TlsEnabled: true`, `TlsPort: 6697`, and provide a PKCS#12 certificate path.

See the [IRC Gateway guide](irc-gateway.md) for command mapping, attachment rendering, and limitations, or [Architecture](architecture.md) for how the gateway integrates with the chat service.

## Configuration

Server configuration is in `appsettings.json` (auto-generated on first run). You can also use environment variables or command-line arguments to override settings.

See the [Configuration](configuration.md) guide for the full reference and how it all works.

## Build from Source

```bash
dotnet build src/EchoHub.slnx
```
