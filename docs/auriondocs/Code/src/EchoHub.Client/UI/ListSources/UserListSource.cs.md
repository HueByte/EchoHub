# UserListSource

> **File:** `src/EchoHub.Client/UI/ListSources/UserListSource.cs`  
> **Kind:** class

```csharp
public class UserListSource : IListDataSource
```


Custom list data source used to render the online users panel where each user's nickname can be shown in a per-user color. Use `UserListSource` when you need a simple, read-only data source that supplies visible text, optional nickname coloring via `Attribute? NameColor`, and username lookup for a `ListView`-style UI; it encapsulates how items are drawn and when the list notifies listeners of wholesale changes.

## Remarks
`UserListSource` stores a list of tuples of the shape `(string Text, Attribute? NameColor, string Username)` and exposes that collection through the `IListDataSource` contract: `Count`, `ToList()`, the `CollectionChanged` event and `Render(...)`. The `Update(...)` method replaces the entire internal list, recomputes `MaxItemLength` using each item's `Text.GetColumns()`, and raises a single `NotifyCollectionChangedAction.Reset` notification unless `SuspendCollectionChangedEvent` is set. Rendering is handled by `Render(...)`: it asks `GraphemeHelper.GetGraphemes(...)` for grapheme clusters, finds where the visible username starts (skipping a leading status icon and optional role badge), draws the prefix in the normal attribute and the username in the per-user `NameColor` (unless the row is `selected`), and fills the remainder of the requested `width` with spaces.

## Notes
- `Update(...)` replaces the entire backing list and always fires a `Reset` change notification (not incremental add/remove events). Consumers that rely on fine-grained collection changes should account for that.
- `SuspendCollectionChangedEvent` prevents `Update(...)` from raising `CollectionChanged`. This is a simple way to batch updates, but callers are responsible for firing or forcing a refresh later if needed.
- `IsMarked(...)` and `SetMark(...)` are no-ops; `UserListSource` does not track per-item marks. Callers expecting mark semantics must manage marks externally.
- `GetUsername(...)` returns `null` when `index` is out of range; callers should check for `null` before using the result.
- `MaxItemLength` is computed from `Text.GetColumns()` for each item; it reflects display column width rather than raw `string.Length` and becomes `0` when the source is empty.
- `Render(...)` uses `GraphemeHelper.GetGraphemes(...)` and per-grapheme `GetColumns()` calls and will truncate output when `drawnChars + cols > width`. This ensures column-consistent drawing for wide or combining characters but may be relatively expensive if called frequently — consider caching grapheme data or avoiding per-frame allocations if rendering many items each frame.
- When `selected` is `true`, the code uses the `Focus`/`Normal` role mapping (`normalAttr`) for both prefix and username; the `NameColor` is ignored while selected. This is an intentional styling choice but may surprise callers who expect nickname coloring even for selected rows.
- `Dispose()` is empty; there are no unmanaged resources to free. The class is not explicitly thread-safe — concurrent calls to `Update(...)` and `Render(...)` without external synchronization may race.