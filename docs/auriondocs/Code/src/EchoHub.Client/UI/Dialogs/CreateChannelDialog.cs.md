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


Provides a single entry point to display a small Create Channel dialog in a text-based UI. It collects a channel name, an optional topic, and a visibility flag, then returns a CreateChannelResult when the user confirms, or null if the user cancels.

## Remarks
This symbol wraps the UI workflow for creating a channel into a reusable unit. It centralizes user input collection and the conversion of that input into a domain object (CreateChannelResult), decoupling the rest of the application from the details of the terminal UI. The Show method performs minimal validation (the name must not be empty after trimming and is normalized to lowercase) and closes the dialog by signaling the application to stop.

## Example
```csharp
var result = CreateChannelDialog.Show(app);
if (result != null)
{
    Console.WriteLine($"Created channel '{result.Name}' (topic: {result.Topic ?? "none"}; public: {result.IsPublic})");
}
else
{
    Console.WriteLine("Channel creation cancelled.");
}
```

## Notes
- The channel name is trimmed and lowercased before producing the result; there is no enforced length or character validation in this implementation beyond the hint text.
- The dialog relies on an active IApplication run loop; calling Show outside of a running app will fail and the UI won't render.

---

## CreateChannelResult
> **File:** `src/EchoHub.Client/UI/Dialogs/CreateChannelDialog.cs`  
> **Kind:** record

```csharp
public record CreateChannelResult(string Name, string? Topic, bool IsPublic)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Name` | `string` | — |
| `Topic` | `string?` | — |
| `IsPublic` | `bool` | — |


CreateChannelResult is an immutable data carrier that represents the outcome of creating a channel via the UI dialog. It carries the channel's Name, an optional Topic, and a flag IsPublic indicating whether the channel is publicly visible. Use this type to pass the created channel data from the Create Channel dialog to other UI components or services that need to know the new channel's identity and visibility.

## Remarks
Because it is a positional-record, it benefits from concise construction and value-based equality. The Topic property is nullable, so callers may omit it or set it to null to indicate 'no topic'. This type acts as a lightweight, UI-facing contract that decouples the dialog's input capture from downstream consumers, enabling safer refactoring and clearer data flow.

## Example
```csharp
var result = new CreateChannelResult("General", "Channel for general discussion", true);
```

## Notes
- Topic may be null; handle nulls when displaying or persisting.
- The constructor parameter order is Name, Topic, IsPublic; passing arguments in the wrong order can lead to mismatched data.

---