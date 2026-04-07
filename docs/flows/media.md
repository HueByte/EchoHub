# Media & Services

## File Upload

Files are uploaded via REST, validated by magic bytes, stored with GUID filenames,
and broadcast as a message with a download link.

```mermaid
sequenceDiagram
    participant Client as Client
    participant CC as ChannelsController
    participant FV as FileValidationHelper
    participant FS as FileStorageService
    participant CS as ChatService
    participant DB as SQLite

    Client->>CC: POST /api/channels/{channel}/upload (multipart)
    CC->>CC: Check file size limits
    CC->>FV: IsValidImage(stream) — magic byte check
    FV->>FV: Read header: JPEG(FFD8FF) / PNG(89504E47) / GIF / WebP(RIFF+WEBP)
    FV-->>CC: true/false
    CC->>FS: SaveFileAsync(stream, extension)
    FS->>FS: Generate GUID filename, write to uploads/
    FS-->>CC: fileId (GUID)
    CC->>CC: Determine MessageType (Image/Audio/File)
    CC->>CS: SendMessageAsync (with file URL + optional ASCII art)
    CS->>DB: INSERT Message
    CS->>CS: BroadcastToAllAsync → fan out to clients
```

---

## Link Embed Resolution

When a message contains URLs, the server fetches OpenGraph metadata for preview
embeds.

```mermaid
sequenceDiagram
    participant CS as ChatService
    participant LE as LinkEmbedService
    participant Web as External Website

    CS->>LE: TryGetEmbedsAsync(messageContent)
    LE->>LE: Extract URLs via regex
    LE->>LE: Filter: max URLs per message, skip duplicates

    loop For each URL
        LE->>LE: Validate: http(s) only, block private IPs
        LE->>Web: GET URL (timeout + size limit)
        Web-->>LE: HTML response
        LE->>LE: Parse og:title, og:description, og:site_name
        LE->>LE: Parse theme-color meta tag
        LE->>LE: Fallback to <title> if no og:title
    end

    LE-->>CS: List<EmbedDto> (or null)
    Note over CS: Attached to MessageDto before broadcast
```

---

## Server Directory Registration

Public servers register with the EchoHubSpace directory for discoverability.

```mermaid
sequenceDiagram
    participant SDS as ServerDirectoryService
    participant Dir as EchoHubSpace Directory
    participant PT as PresenceTracker

    SDS->>SDS: Check Server:PublicServer config
    SDS->>Dir: SignalR connect (echohub.voidcube.cloud/hubs/servers)
    SDS->>Dir: RegisterServer(name, description, host, userCount)
    Dir-->>SDS: Registered

    loop Every 30 seconds
        SDS->>PT: Get online user count
        alt Count changed
            SDS->>Dir: UpdateUserCount(count)
        end
    end

    Dir->>SDS: Ping
    SDS->>Dir: Heartbeat

    Note over SDS,Dir: Exponential backoff on disconnect (2s → 30s max)
```
