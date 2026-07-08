# RenderHelpers

> **File:** `src/EchoHub.Client/UI/Chat/RenderHelpers.cs`  
> **Kind:** class

```csharp
static class RenderHelpers
```


RenderHelpers offers shared rendering utilities for IListDataSource-based UI components. Its WriteText method writes a string into a ListView grapheme-by-grapheme, respecting a width limit and returning the number of columns drawn so far. It iterates over graphemes from GraphemeHelper.GetGraphemes, determines each grapheme's display width with GetColumns() (defaulting to at least 1), stops when adding another grapheme would exceed maxWidth, and appends the grapheme to the ListView via lv.AddStr.

## Remarks
RenderHelpers centralizes small, grapheme-aware rendering concerns for ListView-based UIs. It provides a single, testable path for interpreting text as graphemes and width, ensuring consistent rendering across IListDataSource implementations and reducing duplication. It also encapsulates the width-check logic so callers can focus on higher-level layout decisions.

## Example
```csharp
var lv = new ListView();
string text = "Sample text";
int drawn = RenderHelpers.WriteText(lv, text, 0, 80);
```

## Notes
- Grapheme width is derived from grapheme.GetColumns(); the code uses Math.Max(grapheme.GetColumns(), 1) to guarantee at least one column per grapheme.
- The method stops when adding the next grapheme would exceed maxWidth; it does not wrap to subsequent lines.
- The return value is the updated drawn columns count; callers can use it to render subsequent content.