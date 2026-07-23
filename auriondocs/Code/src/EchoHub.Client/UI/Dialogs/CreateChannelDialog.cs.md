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


CreateChannelDialog.Show renders a modal 'Create Channel' dialog via the supplied `IApplication`, collecting a channel `name`, an optional `topic`, and an optional `password`, validating inputs, and returning a `CreateChannelResult` when the user confirms, or `null` if canceled. The entered `name` is trimmed and converted to lowercase; the `topic` is optional, and a blank `password` yields a `null` password in the result.

## Remarks
By encapsulating the dialog in a single static entry point, this symbol isolates the UI workflow from callers and centralizes its validations and layout. It coordinates several UI components (`Dialog`, `Label`, `TextField`, `Button`) and user input handling so that changes to the channel-creation UX don't ripple through the rest of the codebase.

## Notes
- Name normalization: the code lowercases and trims the input before use; beware that the original casing is not preserved in the result.
- Password handling: the password is optional; if left blank, the resulting `password` becomes `null`.
- Redacted password placeholder: the label uses a redacted placeholder `[REDACTED:CONNECTION_STRING_PASSWORD]`, indicating the actual password source isn't visible in the snippet; ensure the real value is supplied by the surrounding application context.

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


Represents the outcome of a channel-creation operation in the UI. The `CreateChannelResult` type carries the channel's `Name`, an optional `Topic`, a boolean `IsPublic` indicating if the channel is public, and an optional `Password` for password-protected channels, enabling downstream UI logic to respond to the created channel.

---