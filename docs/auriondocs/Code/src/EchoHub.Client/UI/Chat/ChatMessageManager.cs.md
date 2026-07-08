# ChatMessageManager

> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** class

```csharp
public sealed class ChatMessageManager
```


Owns in-memory chat message storage, formatting and mutation for client UI code. Use this class when you need a single place to append formatted chat lines (regular messages, system messages, status messages), track per-channel unread counts, and notify the UI when a channel's message list changes via the MessagesChanged event.

## Remarks
ChatMessageManager centralizes responsibilities that the UI layer relies on: it keeps per-channel ordered ChatLine lists, applies formatting (timestamps, system styling, mention splitting via ChatColors), and maintains unread counters tied to the currently active channel. The MessagesChanged event is the primary integration point for the view layer — subscribers should refresh only the UI for the specified channel. The manager stores and returns live collections (lists and the unread dictionary) rather than defensive copies to keep memory and allocation costs low.

## Notes
- Threading: ChatMessageManager is not synchronized; callers must ensure mutations and reads happen on a consistent thread (UI thread) or apply their own synchronization.
- Live references: GetMessages and the internal GetUnreadCounts return references to the internal collections. Do not modify these collections from outside unless you intend to mutate the manager's state.
- Events and unread lifecycle: AddMessage always invokes MessagesChanged for the affected channel and increments the unread counter only when the channel is not the CurrentChannel. Clearing unread with ClearUnread does not raise MessagesChanged, so the UI should refresh explicitly if needed.