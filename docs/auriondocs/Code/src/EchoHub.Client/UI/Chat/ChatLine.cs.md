# ChatLine

> **File:** `src/EchoHub.Client/UI/Chat/ChatLine.cs`  
> **Kind:** class

```csharp
public partial class ChatLine
```


A single rendered chat line made of colored segments and optional metadata. Use ChatLine when you need to represent a chat message (or a line of one) as a sequence of ChatSegment values so it can be measured, wrapped to a column width while preserving segment colors, and carry metadata (message id, sender, attachment info) alongside the visible text.

## Remarks
ChatLine exists to separate presentation concerns (colored/attributed spans) from raw strings. It stores the visual segments and a precomputed TextLength (measured in display columns using grapheme clustering) so callers can decide whether wrapping is necessary without re-measuring. The Wrap method performs grapheme-aware line breaking, preserves segment coloring by grouping adjacent tokens of the same color, and injects a continuation indent as an explicit space segment when continuationIndent > 0. Metadata such as MessageId, AttachmentUrl, Type, and IsMention are propagated to wrapped lines so wrapped pieces remain clickable/identifiable in the UI.

## Example
```csharp
// Create a simple line from plain text and wrap it to 20 columns with a 4-space continuation indent
var line = new ChatLine("This is a long message that will be wrapped across multiple lines.");
var wrapped = line.Wrap(20, continuationIndent: 4);

// Print the wrapped lines using the class's ToString() which concatenates segment.Text
foreach (var l in wrapped)
    Console.WriteLine(l.ToString());
```

## Notes
- ChatLine stores the `List<ChatSegment>` instance you pass in; mutating that list (or the ChatSegment.Text values) after construction does not update the precomputed TextLength, so TextLength can become stale.
- Wrap returns the original ChatLine instance (the same reference) when no wrapping is needed (width <= 0 or TextLength <= width). Callers should not assume Wrap always produces new objects.
- ContinuationIndent is implemented by prepending a ChatSegment of literal space characters with a null color on continuation lines; styling code should handle null color appropriately.
- Width measurement is grapheme-aware (uses GraphemeHelper and GetColumns). Non-printing or zero-width graphemes are accounted for; the code treats any grapheme as at least one column when calculating fit.
- ChatLine is a lightweight presentation model; it does not perform synchronization — sharing and mutating instances across threads is not safe without external coordination.
