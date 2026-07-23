# RenderHelpers

> **File:** `src/EchoHub.Client/UI/Chat/RenderHelpers.cs`  
> **Kind:** class

```csharp
static class RenderHelpers
```


RenderHelpers is a small, shared utility for rendering IListDataSource content. Its WriteText method writes text to a ListView grapheme-by-grapheme while respecting a maximum width, returning the updated count of drawn columns. It iterates over grapheme clusters obtained from GraphemeHelper.GetGraphemes(text); for each grapheme, it computes the display width with GetColumns() (falling back to 1 if necessary). If adding the grapheme would exceed maxWidth, rendering stops. Otherwise, it appends the grapheme to the ListView via lv.AddStr(grapheme) and increments the drawn count. This centralizes grapheme-aware rendering logic so multiple IListDataSource implementations share consistent width handling and avoid duplicating rendering concerns.