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


Represents a single rendered chat line made up of colored ChatSegment pieces and associated display metadata. Use ChatLine when preparing or manipulating a line for rendering in the chat view (layout, wrapping, attachment actions, separators, mention/highlight state) rather than working with raw strings or segments directly.

## Remarks
ChatLine is the view-level unit for a message or a rule separator: it aggregates ChatSegment instances (text + color), stores metadata such as MessageId, sender, attachment info and clickable action spans, and exposes logic to break the line into multiple display lines that fit a viewport width. It centralizes presentation concerns (continuation indentation, colored continuation prefixes, non-wrapping rule lines, and unread-marker behavior) so the chat rendering layer can ask a ChatLine to produce the wrapped pieces it needs rather than implementing wrapping and metadata handling itself.

## Example
```csharp
// Construct from plain text
var line = new ChatLine("Hello, world!");
// Optional metadata
line.MessageId = Guid.NewGuid();
line.SenderUsername = "alice";

// Wrap to a viewport width of 40 columns, with a 4-space continuation indent
var wrapped = line.Wrap(40, continuationIndent: 4);

// Construct from explicit segments (preserves per-segment color attributes)
var segments = new List<ChatSegment>
{
    new ChatSegment("[alice] ", ChatColors.RailAttr),
    new ChatSegment("This is a message", null)
};
var coloredLine = new ChatLine(segments);
```

## Notes
- RuleLabel makes the line a separator rule; such lines are not word-wrapped and are regenerated to the viewport width by the view.
- If ContinuationPrefixSegments is set, it overrides ContinuationIndent: continuation lines use the prefix segments' column width instead of plain-space indentation.
- ActionSpans (when present) are column positions relative to the unwrapped line; only the first wrapped line preserves those spans — subsequent wrapped continuation lines do not.
- Wrapping respects grapheme clusters and column widths (uses GetGraphemes and GetColumns), so wide characters and combining sequences are handled when measuring width. If width <= 0 or the line already fits, Wrap returns the original line in a single-element list.

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


It encodes an inclusive horizontal span on a chat line that maps to an AttachmentAction when clicked. This readonly record struct pairs StartCol and EndCol (both inclusive) with an Action to designate a specific clickable region that triggers an attachment operation.

## Remarks
Because it's a value type with immutable fields, AttachmentActionSpan is cheap to copy and compare, which helps with hit-testing and rendering across frames. It expresses the intent of interactive regions alongside their coordinates and associated action, keeping the UI layer decoupled from how actions are executed. This symbol complements other line-rendering data structures that describe clickable spans, enabling straightforward collection, filtering, and application during rendering.

## Notes
- EndCol is inclusive; ensure range checks treat EndCol as inclusive to avoid off-by-one errors.
- Overlapping spans may require careful resolution logic at render or hit-test time to determine which action should fire.

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


An enum that represents the action a click on an attachment line can trigger in the chat UI. It lets the click handler distinguish between opening the image for viewing and saving the image to disk, promoting explicit, testable logic rather than ad-hoc behavior.

## Remarks
By codifying the possible outcomes as an enum, AttachmentAction defines a clear contract for how attachment clicks should be handled. It decouples the UI event from the concrete actions, making it easy to extend with new options (for example, ShareImage) without changing call sites. This abstraction supports consistent behavior across different chat lines and simplifies testing by allowing mocks or verifications based on the enum value.

## Example
```csharp
AttachmentAction action = /* determined by UI context */;
switch (action)
{
    case AttachmentAction.OpenImage:
        // Open the image in a viewer
        break;
    case AttachmentAction.SaveImage:
        // Persist the image to disk
        break;
}
```

## Notes
- If you later add actions to the enum, remember to handle them in all switch expressions and tests.
- Prefer explicit enum-based logic over string-based representations to avoid misinterpretation.
- Ensure UI-to-action mappings are consistent across chat lines to prevent user confusion.

---