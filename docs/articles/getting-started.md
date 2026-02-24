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
curl -sSfL .../install.sh | sh -s -- --version 0.2.8
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

IRC users must have an existing EchoHub account. Authentication works via `PASS`/`NICK`/`USER` or SASL PLAIN. Messages flow bidirectionally between IRC and TUI clients.

For TLS, set `TlsEnabled: true`, `TlsPort: 6697`, and provide a PKCS#12 certificate path.

See the [Architecture](architecture.md) page for details on how the IRC gateway integrates with the chat service.

## Configuration

Server configuration is in `appsettings.json` (auto-generated on first run). See the [example config](https://github.com/HueByte/EchoHub/blob/master/src/EchoHub.Server/appsettings.example.json) for all available options.

To list your server on the [public directory](https://echohub.voidcube.cloud/servers), set `Server:PublicServer` to `true` and `Server:PublicHost` to your server's public address.

## Build from Source

```bash
dotnet build src/EchoHub.slnx
```
