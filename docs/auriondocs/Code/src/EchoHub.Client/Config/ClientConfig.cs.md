# ClientConfig.cs

> **Source:** `src/EchoHub.Client/Config/ClientConfig.cs`

## Contents

- [AccountPreset](#accountpreset)
- [ClientConfig](#clientconfig)
- [NotificationConfig](#notificationconfig)
- [SavedServer](#savedserver)

---

## AccountPreset
> **File:** `src/EchoHub.Client/Config/ClientConfig.cs`  
> **Kind:** class

```csharp
public class AccountPreset
```


AccountPreset is a lightweight data container that groups optional account presentation attributes used by client configuration. It encapsulates a DisplayName, Bio, and NicknameColor so a named preset can be stored, transferred, or reapplied as a unit to influence how an account is presented in the UI.

## Remarks
This type exists to package related display properties together, enabling reuse and persistence of account presentation presets. Since all properties are nullable, consumers can merge a preset with existing data and only override the attributes that are explicitly set.

## Example
```csharp
var preset = new AccountPreset
{
    DisplayName = "Nova",
    Bio = "Exploring the stars of code",
    NicknameColor = "#1E90FF"
};
```

## Notes
- Null properties indicate that the corresponding attribute should not override any existing value when applying the preset to an existing account.


---

## ClientConfig
> **File:** `src/EchoHub.Client/Config/ClientConfig.cs`  
> **Kind:** class

```csharp
public class ClientConfig
```


ClientConfig is a simple data container that groups the client’s preferences and runtime settings into a single object. It includes the list of configured servers (`SavedServers`), the default account preset (`DefaultPreset`), the currently selected theme (`ActiveTheme`), and the notification configuration (`Notifications`). It also carries optional application paths and rendering settings: `DownloadPath` specifies where attachments are saved (null means use the OS Downloads folder), and `DefaultAsciiSize` selects the ASCII-art rendering size for attached images (values 's', 'm', or 'l', defaulting to 'm').

## Remarks
ClientConfig centralizes user preferences and runtime settings, so components can rely on a single source of truth for initialization, persistence, and UI decisions. It folds server configuration (`SavedServers`) together with user-facing settings like the default preset (`DefaultPreset`), the active theme (`ActiveTheme`), and notification behavior (`Notifications`), reducing coupling between subsystems. By exposing `DownloadPath` and `DefaultAsciiSize`, it also captures file-management and rendering preferences that affect attachments across the app.

---

## NotificationConfig
> **File:** `src/EchoHub.Client/Config/ClientConfig.cs`  
> **Kind:** class

```csharp
public class NotificationConfig
```


The `NotificationConfig` class is a small, strongly-typed container for notification playback settings used by the client. It exposes `Enabled`, `Volume`, and an optional `SoundFile` to customize sound behavior. By default, `Enabled` is `true`, `Volume` is `30`, and `SoundFile` is unset, making it ready to bind from configuration sources.

## Remarks
This is a lightweight configuration object that decouples notification behavior from business logic and supports binding from JSON or other configuration providers. It keeps the surface minimal while making it easy to override defaults without code changes.

---

## SavedServer
> **File:** `src/EchoHub.Client/Config/ClientConfig.cs`  
> **Kind:** class

```csharp
public class SavedServer
```


SavedServer is a client-side representation of a configured server for the EchoHub client. It aggregates the server identity (Name and Url), optional user credentials (Username and RefreshToken), user preferences (RememberMe), and per-server state needed to restore a session across restarts. Notably, it includes per-channel encryption state (ChannelKeys), channel-level navigation state (LeftChannels), and per-channel read-tracking (LastReadMessages). These members are stored locally and are not exposed to the server; the server never sees the encryption keys, which are encrypted at rest and scoped to the local machine (see [`RoomKeyProtector`](../Services/RoomKeyProtector.cs.md)). At startup, the client can deserialize this object to rehydrate connections, rejoin channels (excluding those the user explicitly left), and persist unread counts and mentions across restarts.

## Remarks
The `SavedServer` acts as a simple data container that binds together server identity, user identity (when supplied), and user-driven state that enhances the reconnect experience. It sits at the boundary between the persistence layer and the networking layer: serialization of this object enables quick restoration of a user session without re-issuing authentication or resynchronizing channel state. The `ChannelKeys` field, in particular, represents sensitive data tied to end-to-end encrypted channels and is kept on the client; its lifecycle is intentionally scoped to the user’s device and is managed with the same care prescribed for the `RefreshToken`.

## Example
```csharp
var server = new SavedServer
{
    Name = "EchoHub",
    Url = "https://echo.example",
    Username = "alice",
    RememberMe = true,
    LastConnected = DateTimeOffset.UtcNow,
    ChannelKeys = new Dictionary<string, string>
    {
        { "general", "base64encryptedKeyHere" }
    },
    LeftChannels = new List<string> { "old-channel" },
    LastReadMessages = new Dictionary<string, string>
    {
        { "general", "12345" }
    }
};
```

## Notes
- Treat `ChannelKeys` as sensitive data: avoid logging them or exposing them to the UI; ensure at-rest encryption via the client’s security model. The keys are stored only on the client device and are not sent to `server` endpoints.
- This class is intended as a plain data carrier (DTO) used by the persistence and connection layers; do not embed domain logic here. When upgrading or migrating fields, consider versioning in the surrounding storage layer to preserve compatibility.

---