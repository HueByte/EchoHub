# RenderHelpers

> **File:** `src/EchoHub.Client/UI/Chat/RenderHelpers.cs`  
> **Kind:** class

```csharp
static class RenderHelpers
```


RenderHelpers is a small static utility class that centralizes rendering concerns for `IListDataSource` implementations. It currently provides a single method, `WriteText`, which writes text to a `ListView` grapheme by grapheme, respecting a maximum width. It returns the updated drawn-columns count, enabling callers to track horizontal placement as multiple fields are rendered on a single line.

## Remarks
RenderHelpers abstracts the grapheme-aware rendering logic so all list-rendering code shares the same boundary checks and column accounting. It couples the `GraphemeHelper.GetGraphemes` iteration with a safe width calculation, reducing the chance of off-by-one errors when composing UI rows. In short, it’s the single place responsible for safe, width-bound text rendering to a `ListView` in this UI layer.

## Notes
- The width of each grapheme is determined by `grapheme.GetColumns()`, clamped to at least 1 with `Math.Max(grapheme.GetColumns(), 1)`.
- Rendering stops when adding the next grapheme would exceed `maxWidth`; partial graphemes are not drawn.
- The method delegates actual drawing to `ListView.AddStr`, so callers should ensure the `ListView` state is appropriate for incremental writes.