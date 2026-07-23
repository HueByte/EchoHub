# CreateChannelDialog.cs

> **Source:** `src/EchoHub.Client/UI/Dialogs/CreateChannelDialog.cs`

## Contents

- [CreateChannelDialog](#createchanneldialog)
- [CreateChannelResult](#createchannelresult)

---

## CreateChannelDialog
> **File:** `src/EchoHub.Client/UI/Dialogs/CreateChannelDialog.cs`  
> **Kind:** class

```csharp
public sealed class CreateChannelDialog
```


Displays a modal Create Channel dialog that collects the details needed to create a new channel: a name, an optional topic, a password, and a public visibility setting. The name is trimmed and normalized to lower case; if it is empty, the dialog reports an error and stays open. On Create, it builds a CreateChannelResult containing the name, topic (nullable), isPublic, and the password; on Cancel it returns null. The dialog runs via the provided IApplication instance and returns after the user makes a choice.

## Remarks
Encapsulates all UI logic for channel creation into a single entry point, enabling consistent behavior across the app and isolating rendering from business logic. The class acts as a small, self-contained UX widget that constructs the result object, ensuring callers need only handle the CreateChannelResult or null.

## Notes
- Name validation is minimal in code: the name is trimmed and lowercased, and non-empty; there is no explicit enforcement of length or allowed character patterns at runtime beyond what the UI hints suggest.
- Password handling appears behind-the-scenes (the UI labels redact the password, yet the password value is captured and returned as part of the result); ensure secure handling and minimize exposure of the plaintext password.
- The snippet references passwordField and publicCheckbox, which must exist in the full class scope; if you modify the UI composition, ensure these controls are present and wired consistently with the password retrieval and public visibility logic.

---

## CreateChannelResult
> **File:** `src/EchoHub.Client/UI/Dialogs/CreateChannelDialog.cs`  
> **Kind:** record

```csharp
public record CreateChannelResult(string Name, string? Topic, bool IsPublic, string? Password)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Name` | `string` | — |
| `Topic` | `string?` | — |
| `IsPublic` | `bool` | — |
| `Password` | `string?` | — |


CreateChannelResult is an immutable data carrier that represents the outcome of creating a channel in the EchoHub client UI. It carries the channel's Name, an optional Topic, a flag IsPublic indicating whether the channel is public, and an optional Password.

## Remarks
As a record, CreateChannelResult participates in value-based equality, making comparisons straightforward without manual field checks. The positional constructor provides a concise, immutable payload that is easy to pass through layers (UI, services, or view models). You can deconstruct a result into its components, or derive a modified copy with a with-expression if you need a slightly different result without mutating the original. This type is intended to be produced by the channel-creation flow and consumed by UI code and downstream components.

## Example
```csharp
// Common case: create a public channel with a topic and password
var result = new CreateChannelResult("General", "Team discussions", true, "s3cr3t");

// Access fields
string name = result.Name;
string? topic = result.Topic;
bool isPublic = result.IsPublic;
string? password = result.Password;

// Deconstruct for convenience
var (n, t, pub, pwd) = result;

// Create a modified copy
var updated = result with { Topic = "New topic" };
```

---