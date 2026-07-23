# ChatLine.cs

> **Source:** `src/EchoHub.Client/UI/Chat/ChatLine.cs`

## Contents

- [ChatLine](#chatline)
- [AttachmentActionSpan](#attachmentactionspan)
- [AttachmentAction](#attachmentaction)

---

## ChatLine
> **File:** `src/EchoHub.Client/UI/Chat/ChatLine.cs`  
> **Kind:** class

```csharp
public partial class ChatLine
```


A single visual line in the chat view composed of one or more colored [`ChatSegment`](ChatSegment.cs.md)s. Use `ChatLine` when preparing data for rendering or layout (wrapping, separators, attachments, and reply jump targets) rather than when working with raw message text; it carries both the display segments and metadata the view needs (attachment info, rule labels, continuation/indent hints, and navigation markers).

## Remarks
`ChatLine` models what the UI actually renders: a sequence of colored segments in `Segments` plus a small set of rendering hints and metadata. It centralizes information the view needs for word-wrapping (`TextLength`, `Wrap`, `ContinuationIndent`, `ContinuationPrefixSegments`), special-line rendering (`RuleLabel`, `RuleAttr`, `IsUnreadMarker`), and attachment/interactivity (`AttachmentUrl`, `AttachmentFileName`, [`AttachmentKind`](../../../EchoHub.Core/Models/AttachmentKind.cs.md), `ActionSpans`). The `Wrap` method produces multiple `ChatLine` instances that fit a given column `width`, and `JumpToMessageId` links reply-quote lines back to their source message when present in the loaded history.

## Notes
- `TextLength` is computed once in the constructors (via `GetColumns()` on the provided text/segments). Because `Segments` is a mutable `List<ChatSegment>`, mutating `Segments` after construction will not update `TextLength`; keep them consistent or recreate the `ChatLine`.
- If `ContinuationPrefixSegments` is set it takes precedence over `ContinuationIndent` when computing the indent for continuation lines; the prefix's column width is used instead of the plain-space indent.
- `ActionSpans` columns are relative to the unwrapped line, so only the first wrapped line preserves clickable sub-line targets; setting `ActionSpans` to `null` means the whole line should use the kind's default action.



---

## AttachmentActionSpan
> **File:** `src/EchoHub.Client/UI/Chat/ChatLine.cs`  
> **Kind:** record

```csharp
public readonly record struct AttachmentActionSpan(int StartCol, int EndCol, AttachmentAction Action)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `StartCol` | `int` | — |
| `EndCol` | `int` | — |
| `Action` | `AttachmentAction` | — |


Represents an inclusive range of columns on a single chat line that, when clicked, triggers the given `AttachmentAction`. This lightweight, immutable value type pairs a `StartCol`, an `EndCol`, and an `AttachmentAction` to describe what should happen if a user interacts with that span during chat rendering or interaction handling.

## Remarks
This abstraction decouples the definition of clickable regions from the actions they perform, allowing the chat UI to map user interactions to behavior without embedding logic in the rendering layer. As a `readonly record struct`, it is cheap to copy and supports value-based equality, which makes it convenient to accumulate multiple spans in collections or pass them through APIs without risking unintended mutation. The actual interpretation of the `AttachmentAction` is delegated to higher-level components that handle click events, enabling reuse across different chat layouts or themes.

## Notes
- The range is inclusive; ensure `EndCol >= StartCol` before constructing an instance.
- Being a `readonly` record struct, instances are immutable; treat them as value-identity objects rather than mutable state.
- The spans should align with the chat line rendering coordinate space; changes in layout or font metrics may require revalidation of column mappings to avoid misaligned interactions.

---

## AttachmentAction
> **File:** `src/EchoHub.Client/UI/Chat/ChatLine.cs`  
> **Kind:** enum

```csharp
public enum AttachmentAction
{
    OpenImage,
    SaveImage,
}
```


Represents the user action triggered by clicking an attachment line in the chat UI. It encodes the two currently supported outcomes for image attachments: opening the image for viewing or saving it to disk.

## Remarks
This enumeration decouples the click-handler from concrete UI behavior, enabling a single dispatch to determine what to do with an attachment. It also makes future extension easier; adding new actions (for example, copying a link or sharing) would be done by extending this enum and updating the handlers accordingly.

## Example
```csharp
AttachmentAction action = AttachmentAction.OpenImage;
if (action == AttachmentAction.OpenImage)
{
    // Open the image for viewing
}
else if (action == AttachmentAction.SaveImage)
{
    // Persist the image to disk
}
```

## Notes
- Adding new values requires revisiting all switch/if chains that enumerate the actions.
- Exhaustive checks are safer; consider a default fallback to surface unknown actions gracefully.

---