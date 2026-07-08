# SearchDialog.cs

> **Source:** `src/EchoHub.Client/UI/Dialogs/SearchDialog.cs`

## Contents

- [SearchDialog](#searchdialog)
- [SearchResult](#searchresult)
- [SearchResultType](#searchresulttype)

---

## SearchDialog
> **File:** `src/EchoHub.Client/UI/Dialogs/SearchDialog.cs`  
> **Kind:** class

```csharp
public static class SearchDialog
```


Provides a modal command-palette style search dialog (Ctrl+K) for quickly navigating channels and triggering app actions within the EchoHub client. It presents a single searchable list that combines channel entries with a set of default actions, filters as you type, and returns the chosen item as a SearchResult to drive either channel switching or command execution. Call Show with the current application context and the list of channels to display the palette, and it will block until the user selects an item or cancels.

## Remarks
SearchDialog acts as a focused UI component that encapsulates the command-palette experience. It wires together a Dialog, a hint label, a text field, a list view, and a cancel button, delegating item presentation and filtering to SearchListSource and representing user intent via SearchResult. By returning a SearchResult of type Channel or Action, it cleanly separates navigation concerns from command execution and makes the palette reusable across different parts of the client.

---

## SearchResult
> **File:** `src/EchoHub.Client/UI/Dialogs/SearchDialog.cs`  
> **Kind:** record

```csharp
public record SearchResult(SearchResultType Type, string Key, string Label)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Type` | `SearchResultType` | — |
| `Key` | `string` | — |
| `Label` | `string` | — |


Represents a single item returned by a search operation in the EchoHub client UI. Each SearchResult carries the result kind (Type), a stable identifying key (Key) used to fetch details or navigate to the item, and a user-facing label (Label) for display in search results. As a record, it is immutable and provides value-based equality and convenient deconstruction, making it a small, transport-friendly value object for the UI layer.

## Remarks
This symbol acts as a lightweight projection of search results. It decouples the UI from the underlying search implementation, allowing the dialog to render, sort, or group results without exposing internal data structures. The Type guides presentation (for example, iconography or per-type actions), the Key enables retrieval of full details, and the Label provides the text shown to the user.

## Notes
- Immutability means you cannot change its properties after creation; create a new instance instead.
- The deconstruction feature (via the positional record) makes it easy to extract the three values in a single statement.
- If you need more information, prefer introducing a new type rather than extending this record, to preserve its role as a simple data carrier.

---

## SearchResultType
> **File:** `src/EchoHub.Client/UI/Dialogs/SearchDialog.cs`  
> **Kind:** enum

```csharp
public enum SearchResultType
{
    Channel,
    Action
}
```


SearchResultType defines the category of a single search result item produced by the EchoHub client’s search feature. It exposes two categories, Channel and Action, allowing the UI to distinguish how to render and respond to each item in the SearchDialog. Using this enum keeps type-level semantics explicit and enables consistent handling of results without inspecting strings or broader payloads.

## Remarks
By modeling the kind of result as a dedicated enum, the code centralizes decision points about rendering, icons, and navigation. It also makes future extension safer, so new result kinds can be added without changing the calling code that already switches on SearchResultType.

## Notes
- When you add new enum members, audit all switch expressions and rendering paths that consume the value to ensure no unhandled cases exist.
- Prefer comparing against SearchResultType.Channel / SearchResultType.Action rather than raw integers or string literals to preserve type safety and readability.

---