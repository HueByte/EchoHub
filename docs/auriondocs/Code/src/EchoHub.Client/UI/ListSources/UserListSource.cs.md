# UserListSource

> **File:** `src/EchoHub.Client/UI/ListSources/UserListSource.cs`  
> **Kind:** class

```csharp
public class UserListSource : IListDataSource
```


A data source implementation for a list view that presents online users with per-user nickname colors. Use this when you need a ready-made IListDataSource that holds tuples of display text, an optional nickname color (Attribute), and the username; it supplies item count, a maximal item width, batch updates via Update, and a Render implementation that paints a status/prefix in the normal role and the username portion in the configured nickname color while respecting selection and a fixed column width.

## Remarks
UserListSource is a UI-focused data source: it couples a small in-memory collection of user display tuples with a Render method tailored for a ListView consumer. It delegates grapheme-aware splitting to GraphemeHelper so prefix characters (status icon and optional role badge) are drawn in the list's normal attribute while the visible username text is drawn in the per-user nickname color unless the item is selected (selection forces the normal/Focus attribute). MaxItemLength is maintained as a convenience for layout calculations and is updated by Update.

## Notes
- Update replaces the entire contents; after calling Update the class raises NotifyCollectionChangedAction.Reset unless SuspendCollectionChangedEvent is true. If you set SuspendCollectionChangedEvent to batch multiple updates you are responsible for raising/triggering an appropriate collection changed notification afterward.
- MaxItemLength is computed using each entry's Text.GetColumns(), so wide characters and grapheme clusters affect reported width — MaxItemLength is a column/terminal-width measure, not a character count.
- Rendering is grapheme-aware and respects the provided width: text drawing stops when the accumulated column width reaches the requested width. This prevents partial grapheme rendering but means long names will be truncated to fit.
- Several IListDataSource members are intentionally trivial: IsMarked and SetMark are no-ops, ToList returns the visible Text values as objects, and Dispose is a no-op. Callers should not rely on any persistent marking or disposal behavior from this class.
- The implementation contains no internal synchronization; it is not inherently thread-safe. Ensure all access (especially Update and Render) is serialized by the caller when used from multiple threads.