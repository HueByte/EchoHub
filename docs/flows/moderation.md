# Moderation

## Kick / Ban / Mute

Moderators and admins can kick, ban, or mute users via REST API or slash commands.
These actions force-disconnect the target and broadcast the event.

```mermaid
sequenceDiagram
    participant Mod as Moderator
    participant MC as ModerationController
    participant CS as ChatService
    participant PT as PresenceTracker
    participant DB as SQLite
    participant SRB as SignalRBroadcaster
    participant IRCB as IrcBroadcaster

    Mod->>MC: POST /api/moderation/kick/{user}

    alt Kick
        MC->>CS: Get user's channels
        MC->>CS: BroadcastToAllAsync(SendUserKickedAsync)
        MC->>CS: ForceDisconnectAndCleanupAsync(user)
    else Ban
        MC->>DB: Set user.IsBanned = true
        MC->>CS: BroadcastToAllAsync(SendUserBannedAsync)
        MC->>CS: ForceDisconnectAndCleanupAsync(user)
    else Mute
        MC->>DB: Set user.IsMuted = true, MutedUntil = now + duration
        Note over DB: MuteExpirationService auto-unmutes when timer expires
    end

    CS->>PT: ForceRemoveUser(username)
    PT->>PT: Remove from all connections + channels
    CS->>SRB: ForceDisconnectUserAsync(connectionIds, reason)
    CS->>IRCB: ForceDisconnectUserAsync(connectionIds, reason)
    CS->>DB: Set Status=Invisible, LastSeenAt=now
```
