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


AccountPreset is a lightweight data container that groups three optional account identity properties—DisplayName, Bio, and NicknameColor—so callers can apply or persist a predefined persona for an account. It is intended for use in client configuration (ClientConfig.cs), enabling a consistent, reusable identity profile to be attached to account-related logic.

## Remarks
AccountPreset exists to keep related identity attributes together, reducing the surface area of APIs that need to accept or propagate persona data. It aligns with a configuration/templating pattern in the client, making it easier to serialize, store, and reuse account personas across components that render or modify user identity.

## Notes
- All properties are nullable; callers must define default behavior when a property is null (e.g., preserve existing values or apply a fallback).
- Null-valued properties may be serialized depending on the chosen serializer; configure to ignore nulls if you prefer a clean configuration payload.
- There is no validation here; enforce constraints instead in the surrounding configuration or UI logic.

---

## ClientConfig
> **File:** `src/EchoHub.Client/Config/ClientConfig.cs`  
> **Kind:** class

```csharp
public class ClientConfig
```


ClientConfig is the central container for a user's preferences and runtime state in the EchoHub client. It aggregates saved servers, the active account preset, the UI theme, notification settings, and attachment-handling options such as the download path and ASCII-rendering size.

## Remarks

It acts as a single source of truth for components that configure server connectivity, UI theming, and how attachments are stored and rendered. Centralizing defaults and user-specific values reduces duplication and helps ensure consistent behavior across sessions and test environments.

## Example

```csharp
var config = new ClientConfig
{
  SavedServers = new List<SavedServer>
  {
    new SavedServer { Name = "Work", Url = "https://work.example", RememberMe = true }
  },
  DownloadPath = @"C:\Downloads",
  DefaultAsciiSize = "m"
};
```

## Notes

- DownloadPath being null means attachments and saved images go to the OS Downloads folder. Ensure the application has write permissions to that location when relying on the default.
- DefaultAsciiSize accepts "s" (40×40), "m" (80×80), or "l" (120×120). This size applies to copy-paste/drag-drop attachments that do not carry a per-file size flag.


---

## NotificationConfig
> **File:** `src/EchoHub.Client/Config/ClientConfig.cs`  
> **Kind:** class

```csharp
public class NotificationConfig
```


NotificationConfig is a lightweight data container used by the EchoHub client to express how notifications should behave. It encapsulates three related knobs: Enabled, Volume, and SoundFile. Developers instantiate this class to configure or override the client's notification behavior when wiring up configuration (for example, within ClientConfig) or when configuring the notifier component. The defaults indicate that notifications are enabled by default, a modest default volume, and no custom sound file unless specified.

## Remarks

By grouping notification-related settings into a single object, NotificationConfig reduces coupling between components that render or play notification sounds and the rest of the configuration. It also provides a clean extension point: new knobs can be added in the future without scattering settings across call sites, since a single configuration object can be passed around.

## Example

```csharp
var config = new NotificationConfig
{
    Enabled = true,
    Volume = 40,
    SoundFile = "assets/notify.wav"
};
```

## Notes

- Volume is stored as a byte (0–255). If your UI operates in a 0–100 range, map or clamp values appropriately before consumption.
- SoundFile is nullable; when it is null, the consumer should handle the absence of a custom sound (e.g., fall back to a default sound or skip audible notification based on the environment).


---

## SavedServer
> **File:** `src/EchoHub.Client/Config/ClientConfig.cs`  
> **Kind:** class

```csharp
public class SavedServer
```


SavedServer is a client-side representation of a per-server configuration and its associated local state for the EchoHub client. It stores credentials and connection details (Name, Url, Username, RefreshToken), a RememberMe flag, and the last connection timestamp (LastConnected). It also holds per-channel state that remains on the client: ChannelKeys (end-to-end encrypted keys cached per channel), LeftChannels (channels the user explicitly left), and LastReadMessages (per-channel read markers). These keys live only on the user's machine; the server never sees them.

## Remarks
SavedServer acts as the single source of truth for a user's relationship to a particular server within the client. By keeping ChannelKeys and LastReadMessages client-side, the app can decrypt and present channel content and maintain read state even after restarts, without leaking sensitive information to the server. LeftChannels honors user intent by preventing auto-joining of channels the user has consciously left, until they rejoin. This abstraction fits alongside other per-server configuration objects and collates server identity, credentials, and per-channel metadata for efficient session restore and UX.

## Notes
- Sensitive data such as RefreshToken and ChannelKeys should be stored securely at rest; the server never holds these values. 
- These collections are mutable; ensure proper synchronization if accessed from multiple threads to avoid data races or inconsistent state.

---