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


Represents a configuration object describing how an account's presentation should appear in the EchoHub client. It groups three optional values—DisplayName, Bio, and NicknameColor—allowing a preset to customize the displayed name, biographical blurb, and nickname color for a given account. This type is typically instantiated when loading or constructing client configuration presets and is designed to be easily serialized to and from configuration sources. Because each property is nullable, callers can omit any field to indicate that it should use defaults or be provided by other sources.

## Remarks
This class is a lightweight data container that decouples account presentation details from identity. By bundling DisplayName, Bio, and NicknameColor into a single preset, the configuration subsystem can manage, persist, and reuse presentation settings across accounts or sessions. The nullable properties enable partial customization: you can specify only the fields you care about while leaving others untouched.

## Notes
- Null values are intentional; consumer code should treat them as unspecified and apply defaults or omit fields in the UI.
- When persisting to configuration, ensure your serializer is configured to omit nulls if you don’t want empty fields saved.

---

## ClientConfig
> **File:** `src/EchoHub.Client/Config/ClientConfig.cs`  
> **Kind:** class

```csharp
public class ClientConfig
```


ClientConfig is a centralized container for the EchoHub client’s runtime settings, grouping saved servers, the default account preset, the active UI theme, and notification preferences. Developers reach for it when they need to read or update the user’s configured servers, theme, or notification behavior in one place rather than scattering configuration across multiple components.

## Remarks
ClientConfig serves as the boundary between persistence and UI, enabling a cohesive experience by centralizing user-configurable data. It encapsulates server metadata via SavedServer entries, theme selection via ActiveTheme, and alert preferences via Notifications, so changes propagate in a consistent way across the application. The simple, plain-data nature of the class makes it straightforward to serialize to storage and hydrate on startup, and to extend with new configuration sections in the future. Because the class uses explicit defaults, the object is in a usable state immediately after construction.

## Notes
- Mutability caveat: all properties are public and settable. Do not assume the object will remain immutable; consider validating values or guarding against nulls when consuming the configuration.

---

## NotificationConfig
> **File:** `src/EchoHub.Client/Config/ClientConfig.cs`  
> **Kind:** class

```csharp
public class NotificationConfig
```


NotificationConfig is a lightweight, mutable container that encapsulates the settings controlling how notifications are presented by the EchoHub client. It groups three related options: Enabled, Volume, and SoundFile, with sensible defaults so a fresh instance behaves predictably out of the box. It is designed to be easily serialized and passed through the system as a single configuration object, enabling binding from UI forms, config files, or runtime decisions without scattering individual knobs.

## Remarks
By consolidating these values into a single object, the design reduces parameter churn and clarifies which aspects of notification behavior are configurable. The class is intentionally simple and mutable; it does not perform validation, so consumers should validate values before consumption and consider adding a wrapper that enforces invariants when necessary. The SoundFile being nullable communicates that a custom sound is optional; code should guard for null when acting on SoundFile.

## Example
```csharp
// Using defaults
var defaultConfig = new NotificationConfig();

// Custom configuration
var customConfig = new NotificationConfig
{
    Enabled = true,
    Volume = 60,
    SoundFile = "assets/notify.wav"
};
```

## Notes
- Volume is a byte, so values are constrained to the 0–255 range by the type itself.
- SoundFile is nullable; always check for null before attempting to load or play the file.

---

## SavedServer
> **File:** `src/EchoHub.Client/Config/ClientConfig.cs`  
> **Kind:** class

```csharp
public class SavedServer
```


SavedServer is a compact data model that represents a server entry saved by the EchoHub client. It stores the server's display name and endpoint URL (both required), optional authentication details (Username and RefreshToken), a RememberMe flag that signals whether credentials should be retained for future sessions, and a LastConnected timestamp tracking when the client last successfully connected to the server. This type is intended for persisting server configurations and is typically populated during configuration flows or deserialization of saved settings.

## Remarks

SavedServer serves as the per-server entry in the client configuration, decoupling identity (name and endpoint) from credentials and metadata. The required properties enforce that every saved entry has a distinct identity, while optional fields support different authentication modes and persistence strategies. It is designed to be serialized/deserialized as part of the client's configuration lifecycle.

## Example

```csharp
var server = new SavedServer
{
    Name = "Prod",
    Url = "https://prod.example.com",
    Username = "alice",
    RememberMe = true,
    LastConnected = DateTimeOffset.UtcNow
};
```

## Notes
- Name and Url are required; the compiler enforces initialization of these two properties when constructing an instance.
- Username and RefreshToken are nullable; code using them should handle null values.

---