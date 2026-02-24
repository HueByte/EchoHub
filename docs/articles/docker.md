# Docker

## Quick Start

```bash
cp .env.example .env        # create your config
docker compose up -d         # start the server
```

On first run the server automatically generates JWT and encryption keys, creates the database, and seeds a `#general` channel. Connect with the EchoHub client to `http://localhost:5000`.

### Using a Pre-built Image

Instead of building locally, you can pull from GHCR. In `docker-compose.yml`, replace the `build` block:

```yaml
services:
  echohub-server:
    image: ghcr.io/huebyte/echohub-server:latest
    # build:
    #   context: ./src
    #   dockerfile: EchoHub.Server/Dockerfile
```

## Configuration

All settings are configured through the `.env` file. These are environment variables that override `appsettings.json` — the `__` (double underscore) maps to JSON nesting levels. For example, `Server__Name` overrides the `Server:Name` key in `appsettings.json`.

See the [Configuration](configuration.md) guide for the full reference of all available settings and how the override hierarchy works.

Common Docker-relevant variables:

| Variable | Default | Description |
| --- | --- | --- |
| `Server__Name` | My EchoHub Server | Display name for your server |
| `Server__Admins__0` | *(empty)* | Admin username (use `__1`, `__2` for more) |
| `Irc__Enabled` | `false` | Enable the IRC gateway |
| `Serilog__MinimumLevel__Default` | `Information` | Log level (`Debug`, `Warning`, etc.) |

## Persistent Data

All server state lives in a single Docker volume mounted at `/app/data`:

```text
/app/data/
├── appsettings.json   # generated config with JWT/encryption keys
├── echohub.db         # SQLite database
├── uploads/           # uploaded files and avatars
└── logs/              # rolling log files (14-day retention)
```

### Backup

```bash
# stop the server to ensure a consistent snapshot
docker compose stop
# copy the data volume to a local directory
docker cp echohub-server:/app/data ./backup
docker compose start
```

## IRC Gateway

To enable IRC, set these in your `.env`:

```env
Irc__Enabled=true
```

Then uncomment the port in `docker-compose.yml`:

```yaml
ports:
  - "5000:5000"
  - "6697:6697"   # IRC (TLS encrypted, preferred)
```

For TLS, also set:

```env
Irc__TlsEnabled=true
Irc__TlsCertPath=/app/data/cert.pfx
Irc__TlsCertPassword=your_password
```

Mount your certificate into the data volume or bind-mount it directly.

IRC users must have an existing EchoHub account. See [Getting Started](getting-started.md#connect-via-irc) for client connection examples.

## Updating

```bash
# if using pre-built images
docker compose pull
docker compose up -d

# if building locally
docker compose build
docker compose up -d
```

Data persists across updates since it lives in the named volume.

## Troubleshooting

**Port already in use** -- Another process is using port 5000. Change the host port in `docker-compose.yml`:

```yaml
ports:
  - "8080:5000"   # access via http://localhost:8080
```

**Permission denied on volume** -- The container runs as a non-root `echohub` user (UID 999). If using bind mounts instead of named volumes, ensure the directory is writable.

**View logs** -- Check the container output:

```bash
docker compose logs -f echohub-server
```

File-based logs are also available inside the volume at `/app/data/logs/`.
