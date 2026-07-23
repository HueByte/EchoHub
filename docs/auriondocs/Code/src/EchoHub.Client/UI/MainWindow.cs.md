# MainWindow.cs

> **Source:** `src/EchoHub.Client/UI/MainWindow.cs`

## Contents

- [MainWindow](#mainwindow)
  - [MainWindow (constructor)](#mainwindow-constructor)
  - [CurrentChannel](#currentchannel)
  - [HasPendingReplyIndicator](#haspendingreplyindicator)
  - [IsCurrentChannelReadOnly](#iscurrentchannelreadonly)
  - [IsTransitionalStatus](#istransitionalstatus)
  - [ApplyColorSchemes](#applycolorschemes)
  - [BuildMenuBar](#buildmenubar)
  - [ClearAll](#clearall)
  - [ConfirmDeleteMessage](#confirmdeletemessage)
  - [CopyToClipboard](#copytoclipboard)
  - [EnsureChannelInList](#ensurechannelinlist)
  - [ExpandRule](#expandrule)
  - [FocusInput](#focusinput)
  - [FocusMessageList](#focusmessagelist)
  - [GetChannelNames](#getchannelnames)
  - [GuardedClipboardAction](#guardedclipboardaction)
  - [MentionUser](#mentionuser)
  - [OnChannelListSelectionChanged](#onchannellistselectionchanged)
  - [OnChatViewportChanged](#onchatviewportchanged)
  - [OnHistoryPrepended](#onhistoryprepended)
  - [OnInputContentsChanged](#oninputcontentschanged)
  - [OnInputKeyDown](#oninputkeydown)
  - [OnMessageListAccepting](#onmessagelistaccepting)
  - [OnMessageListKeyDown](#onmessagelistkeydown)
  - [OnMessageListMouseEvent](#onmessagelistmouseevent)
  - [OnMessageListVerticalScrollBarScrolled](#onmessagelistverticalscrollbarscrolled)
  - [OnMessagesChanged](#onmessageschanged)
  - [OnStatusBarDrawContent](#onstatusbardrawcontent)
  - [OnUsersListAccepting](#onuserslistaccepting)
  - [OnWindowKeyDown](#onwindowkeydown)
  - [RefreshChannelList](#refreshchannellist)
  - [RefreshMenuBar](#refreshmenubar)
  - [RefreshMessages](#refreshmessages)
  - [RemoveChannel](#removechannel)
  - [ScrollToMessage](#scrolltomessage)
  - [SetChannelTopic](#setchanneltopic)
  - [SetChannels](#setchannels)
  - [SetCurrentUser](#setcurrentuser)
  - [SetReplyingTo](#setreplyingto)
  - [SetStagedAttachments](#setstagedattachments)
  - [ShowError](#showerror)
  - [ShowMessageContextMenu](#showmessagecontextmenu)
  - [ShowSearchDialog](#showsearchdialog)
  - [StageFiles](#stagefiles)
  - [SwitchToChannel](#switchtochannel)
  - [ToggleUsersPanel](#toggleuserspanel)
  - [TryAutocompleteCommand](#tryautocompletecommand)
  - [UpdateInputReadOnly](#updateinputreadonly)
  - [UpdateInputTitle](#updateinputtitle)
  - [UpdateLayout](#updatelayout)
  - [UpdateOnlineUsers](#updateonlineusers)
  - [UpdateSpinner](#updatespinner)
  - [UpdateStatusBar](#updatestatusbar)
  - [UpdateTopicBar](#updatetopicbar)
  - [AltQKey](#altqkey)
  - [CtrlCKey](#ctrlckey)
  - [CtrlVKey](#ctrlvkey)
  - [CtrlXKey](#ctrlxkey)
  - [CtrlYKey](#ctrlykey)
  - [EnterKey](#enterkey)
  - [F2Key](#f2key)
  - [SlashCommands](#slashcommands)
  - [SpinnerFrames](#spinnerframes)
  - [StatusActivityAttr](#statusactivityattr)
  - [StatusBrandAttr](#statusbrandattr)
  - [StatusConnectedAttr](#statusconnectedattr)
  - [StatusDisconnectedAttr](#statusdisconnectedattr)
  - [StatusMentionAttr](#statusmentionattr)
  - [StatusTransitionalAttr](#statustransitionalattr)
  - [TabKey](#tabkey)
  - [UsersPanelWidth](#userspanelwidth)
- [ClickChannelRegex](#clickchannelregex)
- [ClickMentionRegex](#clickmentionregex)
- [AppVersion](#appversion)
- [CtrlKKey](#ctrlkkey)
- [DefaultInputTitle](#defaultinputtitle)
- [F6Key](#f6key)
- [NewlineKey](#newlinekey)

---

## MainWindow
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** class

```csharp
public sealed partial class MainWindow : Runnable
```


Main Terminal.Gui window for the EchoHub chat client: composes channel list, message list, input field, status/topic labels, users panel and menu bar, and exposes events used by the application to react to user actions (channel selection, message submit, file/image paste, connect requests). Reach for this class when you need a ready-made, full-featured chat UI that integrates with an application orchestrator rather than building the UI pieces and event plumbing yourself.

## Remarks
MainWindow encapsulates the Terminal.Gui controls and client-facing UI state for the chat client and acts as the bridge between user interactions and the application logic. It keeps local collections of channels, topics and metadata, hosts a ChatMessageManager for message lifecycle, and exposes a small set of events (OnChannelSelected, OnMessageSubmitted, OnFilesStaged, OnImagePasted, OnConnectRequested) so the rest of the app can respond to user actions without reaching into control internals. Key bindings are represented as KeyCode constants to allow straightforward switch/case handling of raw key codes.

## Notes
- Key bindings are defined as raw KeyCode constants (e.g. EnterKey, CtrlKKey, F6Key). The code compares KeyCode values directly so handlers should compare against those constants rather than relying on Key.Equals semantics.
- The users panel defaults to visible and has a fixed width (UsersPanelWidth = 22). Toggle/resize behavior is managed internally by the window layout.
- SlashCommands contains the list of available client-side slash commands used for Tab autocomplete; update DefaultInputTitle if you change key bindings or common hints.
- File and image events convey concrete payloads: OnFilesStaged provides the channel plus absolute paths of existing files; OnImagePasted provides the channel plus PNG-encoded image bytes. Consumers should validate and process those payloads appropriately.

---

### MainWindow (constructor)
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** constructor

```csharp
public MainWindow(IApplication app, ChatMessageManager messageManager)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `app` | `IApplication` | — |
| `messageManager` | `ChatMessageManager` | — |


Initializes the main application window and assembles EchoHub’s client UI: a four-panel layout with a top menu bar, a left channels pane, a center chat area, a bottom input region, and a right online-users pane. It wires data sources to their views, subscribes to message and history events, binds selection and input handlers so the UI stays in sync with channel changes, and includes a small UX safeguard by rebinding Ctrl+W to delete-word-left to avoid clipboard exceptions.

## Remarks
Serves as the UI composition root for the EchoHub client, carefully placing panels with fixed coordinates and dimensions to provide a stable, predictable layout across interactions. It connects ChannelListSource, ChatListSource, and UserListSource to their respective ListView controls, enabling efficient incremental rendering and per-item styling. Event wiring ensures the view updates reflect incoming messages, channel changes, and user activity without polluting business logic. The Ctrl+W rebound is a pragmatic, user-facing compatibility tweak that prevents clipboard-related crashes during text editing.

## Notes
- Be mindful of unsubscribing event handlers if this window is ever disposed to avoid memory leaks.
- Layout relies on fixed panel dimensions; changing UsersPanelWidth or Y offsets could disrupt alignment.

---

### CurrentChannel
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** property

```csharp
public string CurrentChannel => _messageManager.CurrentChannel
```


Get the name of the currently active channel by delegating to the underlying message manager via _messageManager.CurrentChannel. This read-only property provides a convenient, UI-friendly way to display or react to the active channel without coupling callers to the message manager.

## Remarks
This property serves as a small abstraction that decouples the UI from the messaging subsystem. It simply forwards to the message manager, so changes in how the active channel is determined won't require changes at call sites. If the active channel can change over time, retrieve it as needed (e.g., for bindings or status displays) rather than caching the value.

## Notes
- Read-only property; to change the channel, use the message manager's API rather than assigning to this property. If the channel value can be null, callers should handle null values appropriately.

---

### HasPendingReplyIndicator
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** property

```csharp
public bool HasPendingReplyIndicator => _replyTitleFragment is not null
```


Indicates whether there is a pending reply by evaluating whether the private field `_replyTitleFragment` is non-null. Use this property when the UI or business logic needs to know if a reply is currently being prepared, without directly touching private fields.

## Remarks

By exposing the condition as a public property, callers express intent clearly—whether a reply is pending—without relying on the internal field’s exact name or lifecycle. It also centralizes the decision, so changes to the internal representation (for example, signaling a pending state with a different fragment) require updates only to this property.

## Notes

- The value reflects the internal condition at the moment of access; it may not guarantee that a UI indicator has already been rendered if the underlying state changes without notification.
- If `_replyTitleFragment` is updated from a background thread, ensure changes to the UI that depend on this property are observed on the appropriate UI thread to avoid threading issues.

---

### IsCurrentChannelReadOnly
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** property

```csharp
private bool IsCurrentChannelReadOnly => _systemChannels.Contains(_messageManager.CurrentChannel)
```


IsCurrentChannelReadOnly is a read-only computed property that indicates whether the active channel is a system channel and should be treated as read-only in the UI.
It returns true when _systemChannels.Contains(_messageManager.CurrentChannel), and false otherwise, enabling UI logic to gate actions like message composition accordingly.

## Remarks
IsCurrentChannelReadOnly centralizes the concept of a read-only channel, preventing scattered checks throughout the UI logic. It ties together the system-channel collection and the current channel reported by the message manager, ensuring consistent behavior whenever the active channel changes.

## Notes
- The property is evaluated on access; callers should react to state changes (e.g., by refreshing the UI) to avoid presenting stale read-only state.

---

### IsTransitionalStatus
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** property

```csharp
private bool IsTransitionalStatus => _connectionStatus is not ("Connected" or "Disconnected")
```


IsTransitionalStatus is a private boolean property that indicates whether the current internal connection status is not one of the stable terminal states 'Connected' or 'Disconnected'. It evaluates the underlying _connectionStatus and yields true for any other value, signaling a transitional or in-progress state (for example 'Connecting', 'Reconnecting', or similar custom statuses).

## Remarks
By encapsulating this check, the class centralizes the notion of a transitional connection state and avoids repeating string comparisons across the UI logic. The predicate relies on the internal _connectionStatus field, so changes to how statuses are named or stored can be addressed in one place. This abstraction clarifies intent—code that reads IsTransitionalStatus expresses 'we are in flux' without caring about the exact status value.

## Notes
- Null or unexpected _connectionStatus yields true; ensure initialization or guard against null.
- It uses exact string literals ('Connected' and 'Disconnected'); if localization or status naming changes, update accordingly.
- It is private; external components cannot rely on it. If you need external access, consider exposing a public wrapper or moving the logic to a shared utility.

---

### ApplyColorSchemes
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void ApplyColorSchemes()
```

**Returns:** `void`


Applies the currently registered color schemes to all views and updates the UI to reflect the active theme. Call this after changing themes to refresh colors across the interface, ensuring base, menu, and border colors are propagated to the appropriate controls.

## Remarks
Centralizes theming so color changes are applied consistently through a single call. It applies the base scheme to most subviews and intentionally excludes the menu bar and a few chrome labels that have their own styling. Border colors come from a separate border scheme if available, allowing borders to be tinted independently from text, while the menu-related chrome receives the menu scheme.

## Notes
- Requires a non-null Base scheme to perform theming; otherwise the method performs no action.
- Border color updates occur only if a FrameView is encountered and a non-null border scheme is available; otherwise borders are left unchanged.
- Some subviews are excluded from the base scheme (the menu bar, status label, and topic label) and are updated via the menu scheme instead.

---

### BuildMenuBar
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private MenuBar BuildMenuBar()
```

**Returns:** `MenuBar`


BuildMenuBar constructs the top-level MenuBar for EchoHub’s main window by assembling the File, Server, and User menus and a dynamic Theme submenu derived from ThemeManager. It wires user actions to corresponding events, conditionally includes a Rollback option when a backup exists, and always exposes update and quit actions; the returned bar is positioned at the origin and stretched to fill width, with a small mouse-handling workaround to ensure clicks target the menu items themselves.

## Remarks

It centralizes the composition of the app’s menu, guaranteeing a consistent structure and separator placement between groups. By drawing the theme options from ThemeManager.GetAvailableThemes and conditionally showing a backup rollback, the menu reflects runtime state without scattering logic across call sites. Each action is wired to an event or callback (OnThemeSelected, OnProfileRequested, OnCheckForUpdatesRequested, OnConnectRequested, etc.), making the symbol a single integration point for user interactions. The post-creation mouse-transparency hack fixes input handling in the CommandView so user clicks reliably hit the intended MenuBarItem.

## Notes

- The theme list is built from ThemeManager.GetAvailableThemes(); if it returns an empty collection, the Theme submenu will be effectively empty aside from any inserted separators.
- Actions like OnThemeSelected are invoked via null-conditional operators; ensure callers subscribe to handle the events.
- The mouse transparency workaround relies on internal subview types (MenuBarItem and CommandView) and may require adjustment if the UI framework changes.

---

### ClearAll
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void ClearAll()
```

**Returns:** `void`


Clears all chat state and UI elements when disconnecting. It resets channel and topic collections, clears the message data, clears and rebinds the channel and user lists to empty sources, restores default frame titles, hides the topic label, and refreshes the message view to reflect an empty chat.

## Remarks
Centralizes disconnect cleanup into a single method to guarantee a clean, predictable UI state between sessions. It coordinates multiple UI components and data stores (channel names, topics, channel groups, and the chat/users panes) so no stale data remains. Because it mutates UI elements, callers should ensure it runs on the UI thread to avoid cross-thread exceptions.

## Notes
- This method mutates several UI elements and internal lists; call it only on disconnect to avoid partial state resets if used mid-session.

---

### ConfirmDeleteMessage
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void ConfirmDeleteMessage(Guid messageId)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `messageId` | `Guid` | — |

**Returns:** `void`


This private helper displays a confirmation prompt when the user attempts to delete a message. It presents a dialog titled "Delete Message" with the prompt "Delete this message?" and two actions: a "Delete" button and a "Cancel" button. If the user confirms (the dialog returns 0), it raises the OnDeleteMessageRequested event, passing the provided messageId so the actual deletion logic can be executed by subscribers elsewhere.

## Remarks
This method serves as the UI-facing contract for message deletion: it asks for user confirmation and, only upon explicit consent, notifies the rest of the system via OnDeleteMessageRequested. By delegating the actual deletion to subscribers, it remains agnostic of how messages are stored or removed, enabling different layers to handle the operation (e.g., in-memory vs. remote deletion) without duplicating the confirmation prompt. The method is private, indicating it is an internal implementation detail of the MainWindow and not part of the public API.

## Notes
- The deletion work runs on the UI thread; long-running handlers should dispatch work to the background to avoid UI freezes.
- If there are no subscribers to OnDeleteMessageRequested, the delete operation will not run; ensure you subscribe to handle the deletion.

---

### CopyToClipboard
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void CopyToClipboard(string text)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `text` | `string` | — |

**Returns:** `void`


Copies the provided text to the application's clipboard by delegating to the clipboard interface when available. It calls TrySetClipboardData(text) on _app.Clipboard and swallows any exceptions, logging a warning instead of propagating errors to the caller. This makes clipboard operations a best-effort UI nicety that won't disrupt the user experience if the clipboard is unavailable or causes an exception.

## Remarks

This helper centralizes clipboard access and shields callers from clipboard-related failures. The use of the null-conditional operator on Clipboard prevents a hard failure when the clipboard is not present, and the surrounding try-catch ensures UI responsiveness by converting errors into lightweight warnings. In short, it provides a robust, non-blocking way to attempt copying text.

## Notes

- This method is best-effort: it never throws to callers; a failure is recorded in logs instead.
- If the application clipboard is unavailable (null or unsupported), the call simply does nothing.

---

### EnsureChannelInList
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void EnsureChannelInList(string channelName, bool? isPublic = null, bool? isProtected = null,
        bool? isSystem = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `isPublic` | `bool?` | `null` |
| `isProtected` | `bool?` | `null` |
| `isSystem` | `bool?` | `null` |

**Returns:** `void`


Ensures a channel is present in the left-hand channel list, coordinating several internal collections that track channel visibility and categorization. This helper is typically used after joining a private channel (e.g., via /join) to guarantee the UI reflects the channel’s existence and attributes. By accepting nullable flags for isPublic, isProtected, and isSystem, callers can apply or adjust metadata without touching unrelated state; provided values are written to the corresponding in-memory structures and the UI is refreshed as needed.

If the channel already exists, changes to isProtected or isSystem trigger a refresh of the channel list; if the channel is new, it is added to the list and the UI is refreshed. Note that updates to isPublic alone may modify internal mappings without causing a refresh in the existing-channel path.

## Remarks
Centralizes the logic that coordinates multiple internal data structures: _channelNames, _channelPublic, _channelProtected, and _systemChannels. This reduces the risk of inconsistencies between the stored channel attributes and their presentation in the UI, especially for channels surfaced by /join. It also provides a single point of truth for how the left panel should react when a channel’s category or visibility changes.

## Notes
- Only updates to isProtected or isSystem on an existing channel trigger a UI refresh; updates to isPublic alone may not refresh in that code path.
- This method mutates internal state and calls RefreshChannelList; call on the UI thread to avoid threading issues.
- Nullable flags allow partial updates; pass null to skip updating a given attribute.

---

### ExpandRule
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private static ChatLine ExpandRule(ChatLine line, int width)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `line` | [`ChatLine`](Chat/ChatLine.cs.md) | — |
| `width` | `int` | — |

**Returns:** [`ChatLine`](Chat/ChatLine.cs.md)


Re-generates a separator rule (date change / unread marker) to span the current viewport width: "── label ────────…". It builds a new ChatLine containing a single ChatSegment whose text is composed from a fixed decorative prefix, the rule label, and a tail of dashes whose length adapts to the provided width, then applies the line's color attribute (falling back to the default date rule color) and preserves the IsUnreadMarker flag from the source.

## Remarks
This function centralizes the layout logic for date/unread separators in the chat UI, ensuring consistent visuals as the viewport changes. By deriving the tail length from the width and the label’s width, it maintains a balanced appearance that scales with available space and avoids clipping. Because it derives its color from the source line (or a sensible default), it remains visually cohesive with surrounding UI rules and respects the line’s unread state.

## Example
```csharp
// Example usage: adapt a rule line to the current viewport width
// 'line' represents a ChatLine configured as a date/unread separator elsewhere in the codebase
ChatLine expanded = ExpandRule(line, 120);
```

## Notes
- The method assumes line.RuleLabel is non-null (the code uses line.RuleLabel!); if RuleLabel is null, a exception may be thrown at runtime.
- tailLen is clamped to a minimum of 2 to ensure a visible tail even on narrow widths.
- The returned ChatLine is a new instance; the input line is not mutated, preserving functional purity for rendering.

---

### FocusInput
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void FocusInput()
```

**Returns:** `void`


FocusInput moves keyboard focus to the primary input field by delegating to the underlying input control's SetFocus method. Use this when you want to programmatically place the typing cursor into the main input, such as after the window is shown or after an action that should prepare for typing.

## Remarks
This method centralizes focus behavior for the main window's input area, providing a single, discoverable point to direct user input. It abstracts away the details of how focus is delegated to the input control, making future UI changes easier to apply. Naming it FocusInput conveys intent clearly, improving readability wherever the UI needs to programmatically direct typing.

---

### FocusMessageList
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void FocusMessageList()
```

**Returns:** `void`


Moves focus to the message list so keyboard users can navigate with arrow keys and delete messages with Delete. If nothing is selected, or if the current selection is out of range, the most recent message is selected. If the channel has no messages, this method is a no-op. After adjusting the selection, it focuses the list and requests a redraw to reflect the change.

## Remarks
Centralizes the focus logic for the chat message list, ensuring predictable keyboard navigation and a consistent starting point for message management. It relies on the underlying ChatListSource's Count and the _messageList control to guard against invalid selections and to trigger a UI refresh.

## Notes
- Intended to run on the UI thread since it manipulates UI controls.
- This is a private helper; external callers should not depend on its behavior beyond what the class exposes.

---

### GetChannelNames
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public IReadOnlyList<string> GetChannelNames() => _channelNames.AsReadOnly()
```

**Returns:** `IReadOnlyList<string>`


Returns an `IReadOnlyList<string>` containing the names of all channels that have message buffers, which are used to broadcast status changes. This method is typically used when a caller needs to enumerate the channels to notify about status updates without taking ownership of or mutating the internal collection.

## Remarks
By wrapping the internal list with AsReadOnly, this method preserves encapsulation: callers can observe which channels exist without gaining permission to modify the collection. It also centralizes how channel names are exposed, so changes to the underlying storage need only be updated here. If the set of channels changes over time, the returned view will reflect those changes; if a stable, unchanging snapshot is required, consider copying the values to a new list.

## Notes
- The returned view is not a deep freeze; it's a live wrapper over the internal list. For a stable snapshot, copy to a new list.

---

### GuardedClipboardAction
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private static void GuardedClipboardAction(Action action, string operation)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `action` | `Action` | — |
| `operation` | `string` | — |

**Returns:** `void`


GuardedClipboardAction executes the supplied edit action and swallows transient clipboard failures by catching all exceptions and logging a warning that the clipboard operation failed, including the operation name. Use it when performing clipboard-related edits to prevent OS clipboard contention from propagating to the input loop, keeping the application responsive even if the clipboard is momentarily unavailable.

## Remarks
GuardedClipboardAction encapsulates the resilience policy for clipboard interactions, separating error handling from the business logic that mutates the clipboard. By treating clipboard failures as non-fatal, it reduces boilerplate at call sites and provides a consistent user experience when the clipboard is busy or locked by another process.

## Notes
- It catches all exceptions, potentially hiding bugs if used in contexts where failures should propagate.
- No retry logic is performed; if a retry is desired, implement it at the call site or extend this helper.
- Logs a warning with the operation name to aid debugging without interrupting the user flow.

---

### MentionUser
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void MentionUser(string username)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string` | — |

**Returns:** `void`


MentionUser inserts a user mention into the chat composer by placing "@{username} " at the current cursor position and then returning focus to the input field. Use it when the UI needs to prefill a mention (for example after the user selects a contact) so the user can continue typing immediately with a properly formatted mention.

## Remarks
By centralizing the mention formatting (leading '@' and trailing space) and focus restoration, this method ensures all mentions are inserted consistently across the chat UI. It depends on the _inputField control, so it should be invoked on the UI thread where that control exists.

## Example
```csharp
// When a user is selected for a mention
MentionUser("alice");
```

## Notes
- No validation or escaping is performed; the value is inserted as-is after '@'.
- Should be called on the UI thread when the input field is accessible; otherwise UI interactions may fail.
- Assumes _inputField is non-null; if not, it may throw.

---

### OnChannelListSelectionChanged
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void OnChannelListSelectionChanged(object? sender, ValueChangedEventArgs<int?> e)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `sender` | `object?` | — |
| `e` | `ValueChangedEventArgs<int?>` | — |

**Returns:** `void`


Handles the channel list selection change by reading the new index from the event args, validating that it points to a valid channel, and switching to that channel only if it's not already the current channel. It then raises OnChannelSelected with the chosen channel name to propagate the change.

## Remarks
Serves as the UI-to-state bridge for channel navigation. It centralizes the validation of the selection index and guards against unnecessary channel switches, ensuring the application only changes channels when asked by the user. It relies on the _channelNames collection as the authoritative mapping from list indices to channel identifiers, and on _messageManager.CurrentChannel to determine whether a switch is needed. By publishing the new channel through OnChannelSelected, other components can react (e.g., updating status, logging, or triggering related UI updates) without directly coupling to the list control.

## Notes
- Ensure _channelNames stays in sync with the channels presented in the UI; a mismatch can lead to incorrect mappings or no operation.
- The method assumes _messageManager.CurrentChannel reflects the current channel state; if not, behavior may not switch as expected.
- The OnChannelSelected event is invoked using the null-conditional operator, so there may be zero subscribers without causing a crash.

---

### OnChatViewportChanged
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void OnChatViewportChanged()
```

**Returns:** `void`


The method reacts to changes in the chat viewport width by reading the current width from the chat list’s viewport. If the width is positive and differs from the last recorded width, it updates the cached value, propagates the new width to the message manager, and refreshes the messages to align the display with the new size.

## Remarks

This is a focused resize handler that isolates width-change logic from general rendering. By guarding against no-ops (width <= 0 or unchanged width) it avoids unnecessary layout work and keeps the chat display in sync with the viewport through the message manager and a refresh cycle. It relies on the UI-related components (_messageList, _messageManager, and RefreshMessages) and should be invoked within the appropriate UI thread context.

## Example

```csharp
// Common case: the chat viewport has been resized to a new positive width
OnChatViewportChanged();
```

## Notes

- The method only acts when newWidth > 0 and newWidth != _lastChatWidth, preventing redundant work on unchanged sizes.
- It updates internal state before triggering a layout refresh, ensuring subsequent calls see the updated width.
- Since it touches UI-related components, ensure invocation occurs on the UI thread to avoid cross-thread access issues.

---

### OnHistoryPrepended
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void OnHistoryPrepended(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `void`


Maintains the user's reading position when historical messages are prepended to the current channel's message list. If the incoming channel matches the currently displayed channel, it loads the channel's messages, records how many items were visible before the refresh, refreshes the list, computes how many items were prepended, and, if any, selects the item at that offset to keep the view from jumping to the top. This behavior helps the user remain oriented while older messages are loaded above the current view.

## Remarks

This method exists to decouple the UI from the underlying data refresh when history is added to the top of the chat. By capturing the pre-refresh count and re-selecting based on the number of newly prepended items, it preserves the visual position in the list and avoids a disruptive jump to the beginning. It relies on the current channel check, the ChatListSource.Count, and the SelectedItem property to compute and apply the offset during the refresh cycle.

## Notes

- Guard clauses ensure the method is a no-op when the channelName is not the current channel or when there are no messages for the channel.
- The offset calculation assumes RefreshMessages updates the list by prepending items above the existing ones; if the underlying data source changes differently, the preserved position may be off by one or more items.
- This is an internal handler; external callers should not invoke it directly.

---

### OnInputContentsChanged
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void OnInputContentsChanged(object? sender, ContentsChangedEventArgs e)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `sender` | `object?` | — |
| `e` | `ContentsChangedEventArgs` | — |

**Returns:** `void`


OnInputContentsChanged handles content changes in the main window's input field. It respects a guard (_suppressEmojiReplace) to avoid re-entrant edits caused by programmatic text changes. If the current input text appears to be a dropped file path and DroppedFileParser can resolve one or more files, and there is a non-empty _messageManager.CurrentChannel, it clears the input and stages the discovered files for sending instead of leaving the raw path text to be dispatched. Otherwise, it runs EmojiHelper.ReplaceEmoji to substitute emoji sequences. If no replacement occurs, the method returns. If replacements occur, it computes the delta in length, repositions the cursor to the corresponding column after the edit, and updates the input field accordingly, wrapping the change in a guard to re-enable emoji processing afterwards.

---

### OnInputKeyDown
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void OnInputKeyDown(object? sender, Key e)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `sender` | `object?` | — |
| `e` | `Key` | — |

**Returns:** `void`


OnInputKeyDown is the keyboard shortcut handler for the chat message input area. It interprets specific keys to drive UI behaviors such as autocompletion, reply cancellation, message submission, and clipboard-based attachments, ensuring keyboard users can perform common actions without leaving the input context.

Shortcuts supported include Tab for autocompletion; Esc to cancel a pending reply (when such a reply exists); Newline to insert a line break; Enter to submit the message when there is content or staged attachments and there is a valid channel; Alt+Q to quit; Ctrl+K to open the search dialog; F6 to focus the message list; Ctrl+V/Ctrl+Y to paste, with clipboard-aware behavior: paste files from the clipboard as attachments, paste PNG image data as an image, or fall back to a normal text paste; Ctrl+X to cut; Ctrl+C to copy. Unrecognized keys are ignored and the event is left unhandled.

## Remarks
OnInputKeyDown centralizes input-related keyboard interactions for the main window, so the user experience remains consistent between typing, editing, and navigation. It delegates the actual actions to collaborators (the input field, the message manager, and the event publishers like OnMessageSubmitted and OnImagePasted), keeping key handling isolated from business logic. It also respects channel permissions by bypassing text-entry actions when the current channel is read-only, preventing invalid edits.

## Notes
- Enter submission guard: the message is submitted only if there is text (after trimming) or there are staged attachments, and the current channel is non-empty.
- Clipboard behavior: Ctrl+V/Ctrl+Y first try to stage clipboard files as attachments; if none are files, they attempt to paste PNG image data from the clipboard; if neither applies, they fall back to a standard text paste via the input field.
- Read-only channels: text entry and attachment staging are skipped when the channel is read-only, ensuring the UI adheres to channel permissions.


---

### OnMessageListAccepting
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void OnMessageListAccepting(object? sender, CommandEventArgs e)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `sender` | `object?` | — |
| `e` | `CommandEventArgs` | — |

**Returns:** `void`


Activates the selected chat line in the message list, validating the source and index, then dispatching a context-appropriate action based on what the line represents. It first resolves quote targets for replies, then handles attachments (audio, file, or image), detects @mentions and #channels within the line text, and finally falls back to opening the sender's profile. Each path marks the event as handled to stop further processing.

## Remarks
All user interactions for a list entry funnel through this method, providing a single, predictable entry point for keyboard or programmatic activation. It keeps UI concerns decoupled from the data model by routing actions through dedicated callbacks (OnAudioPlayRequested, OnFileDownloadRequested, OnImageOpenRequested, OnUserProfileRequested, OnChannelJoinRequested) and by inspecting ChatLine properties such as AttachmentKind, JumpToMessageId, and SenderUsername. This design minimizes scattered conditional logic across the UI layer and centralizes the decision-making about what happens when a line is activated.

## Notes
- Early returns guard invalid state (null source, invalid index) and keep no-op paths contained.
- Attachment priority: if an attachment exists, media actions take precedence over textual interactions (mentions/channels).
- Fallback behavior: if no attachment, mention, channel, or sender username applies, the method completes without invoking any callbacks.

---

### OnMessageListKeyDown
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void OnMessageListKeyDown(object? sender, Key e)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `sender` | `object?` | — |
| `e` | `Key` | — |

**Returns:** `void`


This event handler processes keyboard input for the chat message list. It provides two primary shortcuts: F6 moves focus back to the input box to enable quick replies, and Delete or Backspace initiates the deletion flow for the currently selected message. If F6 is pressed, focus is transferred to the input field and the event is marked as handled. If Delete or Backspace is pressed, the code first validates that the message list source is a ChatListSource, that a valid item is selected, and that the selected line exposes a MessageId. When these conditions are met, the handler invokes ConfirmDeleteMessage with the message's ID and marks the event as handled, delegating the actual permission check to the server. The server enforces real permissions (own message or Mod+ over a lower role); the client simply confirms intent and relies on the server to reject disallowed actions.

---

### OnMessageListMouseEvent
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void OnMessageListMouseEvent(object? sender, Mouse e)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `sender` | `object?` | — |
| `e` | `Mouse` | — |

**Returns:** `void`


Handles mouse interactions for items in the chat message list. The method processes left- and right-clicks to either activate attachment actions on a line or present a context menu; if the click doesn't map to a known action, it returns without side effects. It determines the targeted line from the list's TopItem and the mouse position, validates bounds, and, for left-button clicks, dispatches OpenImage or SaveOriginal actions when the click occurs on a matching ActionSpan with a non-null AttachmentUrl and AttachmentFileName. It invokes OnImageOpenRequested(url, name) when the Action is OpenImage, otherwise OnImageSaveRequested(url, name). It then marks the event as handled and returns. For right-clicks, it selects the row, focuses the list, and shows the message context menu at the screen position. If none of these conditions apply, the method simply returns, leaving other listeners to handle the event as appropriate.

## Remarks
By centralizing this logic in OnMessageListMouseEvent, the UI separates interaction details from the concrete actions (open/save). The event-based callbacks (OnImageOpenRequested, OnImageSaveRequested) enable the host to implement corresponding behavior without the control needing to know how to present or fetch attachments. It coordinates among the message list, its source lines, and the context menu, ensuring left-clicks only trigger attachment-related actions and right-clicks prepare the selection and menu.

## Notes
- Left-click triggers only when the target line has both an AttachmentUrl and an AttachmentFileName and the click lies within an ActionSpan; otherwise the method returns without performing the action.
- When an action path is taken, OnImageOpenRequested or OnImageSaveRequested is invoked with the attachment's URL and file name, and e.Handled is set to true to suppress further processing.
- Right-click path selects the row, focuses the list, and shows the context menu; the event is marked as handled in those cases.

---

### OnMessageListVerticalScrollBarScrolled
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void OnMessageListVerticalScrollBarScrolled(object? sender, EventArgs<int> e)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `sender` | `object?` | — |
| `e` | `EventArgs<int>` | — |

**Returns:** `void`


This private method serves as the Scroll event handler for the message list's vertical scrollbar. It detects when the scrollbar has been scrolled to the top (VerticalScrollBar.Value == 0) and, in that case, raises the OnLoadMoreRequested event to fetch older messages, if any subscriber is attached.

## Remarks

By centralizing the trigger for loading more messages behind a single, UI-focused event, this abstraction keeps the data-loading flow decoupled from the scrollbar mechanics. It relies on the scrollbar's Value to determine the top position, rather than on the event payload, making the behavior straightforward to test and reason about in isolation. This pattern enables incremental history loading without scattering load logic across the UI code.

## Notes

- The OnLoadMoreRequested invocation is guarded by the null-conditional operator, so no action occurs if there are no subscribers.
- Scrolling to the top may re-trigger loads if the user lingers at the top or repeatedly reaches it; consider adding a guard (e.g., a loading flag or debounce) in the handler or subscriber to prevent concurrent or duplicate fetches.

---

### OnMessagesChanged
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void OnMessagesChanged(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `void`


Handles the event when the message set changes for a channel. If the changed channel matches the currently displayed channel, it refreshes the visible messages; otherwise, it refreshes the channel list. Finally, it marks the status bar for redraw to reflect background activity.

## Remarks
This method acts as the UI glue between the message data and the main window. It centralizes the decision about what to refresh, reducing unnecessary work by only reloading the current channel’s messages when needed. The unconditional SetNeedsDraw ensures the status indicator is updated after any message activity.

## Notes
- Assumes _messageManager and _statusLabel are initialized before invocation; otherwise a NullReferenceException could occur.
- The comparison channelName == _messageManager.CurrentChannel is a straightforward string equality check; if channel naming becomes more complex, consider normalization.
- This method is private and intended to be invoked by the class itself in response to message-change events, not by external callers.

---

### OnStatusBarDrawContent
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void OnStatusBarDrawContent(object? sender, DrawEventArgs e)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `sender` | `object?` | — |
| `e` | `DrawEventArgs` | — |

**Returns:** `void`


OnStatusBarDrawContent renders the EchoHub status bar by composing branding, version, connection state (with an animated spinner for transitional states), the current user, channel details, and unread activity into the status label. It respects the available width by measuring grapheme columns and cancels the default drawing to ensure full control over layout and color.

## Remarks
This method centralizes the visual composition of the status bar, ensuring consistent theming and typography through the various status attributes and color selectors. It reads live state from the application's managers (such as the current user, current channel, and unread activity) and adapts visuals (including a spinner for transitional connection states and color-coded channel/user indicators) to reflect real-time context. By isolating this logic, the UI remains cohesive even as underlying state sources evolve, and it guarantees a stable, width-aware rendering surface for the status line.

## Notes
- The function early-exits if the status bar width is non-positive, so ensure the status bar has a meaningful width before rendering.
- It uses a Resolve step to substitute a None background with the normal background, preserving a consistent look across segments.
- The handler takes full control of drawing for the status bar (it may set e.Cancel = true and fill remaining space with spaces); callers should not rely on the default rendering path.

---

### OnUsersListAccepting
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void OnUsersListAccepting(object? sender, CommandEventArgs e)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `sender` | `object?` | — |
| `e` | `CommandEventArgs` | — |

**Returns:** `void`


This private event handler responds to the user activating a selection in the users list. It first validates that there is a selected item within bounds, resolves the corresponding username from the list source, and if a username exists, raises the OnUserProfileRequested event with that username and marks the command as handled to prevent further processing.

## Remarks
This method acts as the bridge between the UI interaction (selecting a user and accepting the action) and the navigation to that user’s profile. It encapsulates the guard logic for a valid selection and a non-null username, ensuring the rest of the UI remains robust against invalid state. By emitting OnUserProfileRequested, it decouples the act of selecting a user from the actual navigation/presentation of the profile, allowing subscribers to decide how to present the profile (e.g., in a panel, a new window, or a details view). The null-conditional invocation protects against the absence of subscribers without forcing a null check at every call site.

## Notes
- The method returns early when there is no valid selection (null index, negative index, or index outside the source bounds).
- A username must be non-null to trigger navigation; otherwise, no event is raised and no handling is marked.
- e.Handled is set to true only when a username is successfully resolved and the profile request is emitted, indicating to the caller that the command was consumed.

---

### OnWindowKeyDown
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void OnWindowKeyDown(object? sender, Key e)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `sender` | `object?` | — |
| `e` | `Key` | — |

**Returns:** `void`


Handles the main window's KeyDown events and implements a small set of global keyboard shortcuts. When one of the predefined keys is pressed, it invokes the corresponding action (stop the application, toggle the users panel, or show the search dialog) and marks the event as handled to prevent further processing; for unrecognized keys, the method returns without taking action, allowing normal input behavior.

## Remarks
By consolidating keyboard shortcuts in OnWindowKeyDown, the class centralizes input concerns for the window and makes it easier to adjust global hotkeys in one place. The handler relies on private members such as _app, ToggleUsersPanel, and ShowSearchDialog to perform the actions, which keeps the event handling decoupled from UI details. Marking the event as Handled ensures that no other key handlers react to these shortcuts, avoiding conflicting behavior.

## Notes
- Only triggers for the specific KeyCode values (AltQKey, F2Key, CtrlKKey); other keys are passed through.
- Because it is a private method, wiring must happen in the same class (e.g., via the KeyDown event).
- Setting e.Handled = true happens only after a known shortcut is processed; if the handler returns early for unrecognized keys, Handled remains false.


---

### RefreshChannelList
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void RefreshChannelList()
```

**Returns:** `void`


RefreshChannelList refreshes the channel list in the UI, showing unread counts next to channel names. It pins system channels to the top, preserving the relative order of non-system channels, and then determines which channels are private (excluding system channels). The method updates the UI data source with the latest channel list, unread counts, current channel, privacy flags, and mentions, and finally restores the user's current channel selection.

## Remarks
Centralizes the channel-list choreography: ordering, privacy flags, unread counts, and selection state are prepared here before binding to the UI. It relies on _channelNames as the source of truth for selection, uses a stable sort to preserve alphabetical order within system and non-system groups, and computes privateChannels by consulting both _systemChannels and _channelPublic. By separating data preparation from UI binding (_channelListSource.Update and _channelList.Source), the code remains easier to reason about and yields deterministic refresh behavior.

## Notes
- The ordering is stable: system channels are pinned to the top, and non-system channels retain their original alphabetical order within their group.
- System channels are private by nature but do not receive the private glyph; they are explicitly excluded from the privateChannels set.
- The method mutates _channelNames in place; ensure it is invoked on the UI thread to avoid race conditions during updates.


---

### RefreshMenuBar
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void RefreshMenuBar()
```

**Returns:** `void`


RefreshMenuBar rebuilds and replaces the menu bar to reflect changes such as updated themes. Call it when theme lists or related UI chrome have changed to ensure the bar is rebuilt and redrawn, instead of trying to mutate the existing bar in place.

## Remarks
This method centralizes the UI update required after theme changes, ensuring the menu bar is rebuilt from a fresh state and consistently themed. It delegates bar construction to BuildMenuBar() and handles the lifecycle: removing the old bar, inserting the new one, applying color schemes, and requesting a redraw. By encapsulating this sequence, the UI remains coherent even as themes and related styling evolve, reducing the risk of stale or inconsistent visuals.

## Notes
- Potential UI flicker: the bar is removed and rebuilt; rapid theme changes could cause a brief visual flash.
- Must be called on the UI thread to safely manipulate UI controls; crossing threads can lead to exceptions.

---

### RefreshMessages
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void RefreshMessages()
```

**Returns:** `void`


RefreshMessages updates the chat ListView by transforming the current channel’s messages into a renderable ChatListSource, wrapping lines to the available viewport width and expanding rules when needed; it then binds the source to the UI and places the selection on the latest item. It caches the last measured width (falling back to it when the viewport hasn’t been laid out yet) to avoid flicker on initial layout. If no channel is selected, it displays a welcome banner instead of messages.

## Remarks
This method centralizes the layout-sensitive conversion from raw messages to UI presentation, isolating wrapping and rule-expansion logic from the ListView rendering code. It also guarantees the view shows the most recent content by selecting the last item after building the source, and it gracefully switches to a welcome banner when there is no active channel.

## Notes
- If the viewport width is not yet reported (width <= 0) and there is no cached width, the method adds messages without wrapping, potentially producing non-wrapped lines until layout occurs.
- Requires access to UI elements (_messageList, _messageManager, WelcomeBanner); should be invoked from the UI thread to avoid threading issues.

---

### RemoveChannel
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void RemoveChannel(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `void`


Removes a channel from all internal channel collections and then refreshes the left panel. Use this when deleting a channel to ensure the in-memory state and the UI stay in sync, rather than mutating each collection separately.

## Remarks
This method serves as a single, centralized deletion boundary for channel metadata. By encapsulating removal across multiple collections (_channelNames_, _channelTopics_, _channelPublic_, _channelProtected_, _systemChannels_), it guarantees consistent state and avoids partial leftovers, and it ensures the UI is refreshed to reflect the current channel set.

## Notes
- Removal is best-effort; if a channel name isn't present in a given collection, that collection simply omits it without throwing.
- If called from a non-UI thread, ensure thread affinity when RefreshChannelList needs to update the UI.

---

### ScrollToMessage
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void ScrollToMessage(Guid messageId)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `messageId` | `Guid` | — |

**Returns:** `void`


Scrolls the message list to the first line of the specified message, a helper used by the reply-quote workflow to align quotes with the original content. If the message is not present in the currently loaded buffer, the method is a no-op and makes no UI changes.

## Remarks
This method isolates a small but important piece of UI choreography: given a messageId, it locates the corresponding line in the ChatListSource, selects it, and adjusts the scroll so the line appears near the top of the view (with a three-line context). It avoids impacting other lines or quote lines by requiring JumpToMessageId to be null, ensuring we jump to the message's own line. Keeping this logic in one place simplifies the quote rendering flow and makes behavior consistent across different message states.

## Notes
- Linear search through the current source; costs are proportional to the number of lines in view.
- No action if Source is not a ChatListSource or if no matching line is found.
- TopItem is set to Math.Max(0, i - 3) to preserve a little context above the target line.
- Mutates UI state (SelectedItem, TopItem, focus, and redraw) and should be invoked on the UI thread.

---

### SetChannelTopic
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void SetChannelTopic(string channelName, string? topic)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `topic` | `string?` | — |

**Returns:** `void`


Updates the topic for a specific channel by assigning the provided topic to the channel's entry in the internal _channelTopics dictionary. If the channel being updated is the one currently displayed in the UI (as indicated by _messageManager.CurrentChannel), it also refreshes the topic display by calling UpdateTopicBar.

## Remarks

By centralizing topic mutations here, callers don’t need to manipulate UI state directly. The method ensures consistency between the channel topics data model and the user interface by triggering a UI update only when the changed channel is the active one.

## Example

```csharp
// Update the topic for the "general" channel; the UI will refresh if that channel is currently active
SetChannelTopic("general", "Welcome to the general discussion channel!");
```

## Notes

- If topic is null, the topic for the specified channel is cleared in the _channelTopics dictionary. The entry is created or overwritten as needed.
- Changes to topics for non-active channels do not immediately affect the UI; the topic bar updates only when the affected channel is the current/active channel.


---

### SetChannels
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void SetChannels(List<ChannelDto> channels)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channels` | `List<ChannelDto>` | — |

**Returns:** `void`


SetChannels takes a list of ChannelDto, clears the local channel state, fills in names, topics, and visibility, and marks protected and system channels, before refreshing the channel list UI. Use this when you receive a full snapshot of channels from the server and want the UI to reflect that snapshot in one operation rather than mutating individual channels.

## Remarks
By converting the ChannelDto data into the UI state, this method isolates the UI from the raw DTOs and centralizes the mapping logic. It ensures a consistent representation of channel topics and visibility, while grouping channels by protected or system roles to simplify downstream behavior.

## Notes
- Null input will cause an exception; callers should pass an empty list or validate before calling.
- Assumes channel names are unique identifiers; duplicate names will create duplicates in _channelNames and may overwrite entries in _channelTopics or misclassify in protected/system lists.

---

### SetCurrentUser
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void SetCurrentUser(string username)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string` | — |

**Returns:** `void`


Sets the current user name and delegates to the message manager for @mention detection. Use this when the active user changes (for example, after login or user switch) so that subsequent messages and mentions are evaluated against the correct user.

## Remarks
This method is a thin wrapper around _messageManager.SetCurrentUser; it centralizes the UI's interaction with the messaging subsystem and keeps the rest of the UI agnostic about how mentions are detected. By routing the update through the message manager, changes to mention resolution are applied consistently across the system and encapsulated in a single component. In practice, the UI can continue to call SetCurrentUser without needing to know about the underlying messaging implementation.

## Notes
- No input validation is performed here; ensure the provided username meets the expectations of the message manager and is non-null.
- As a pass-through, any validation or side effects originate from _messageManager; callers should be prepared to handle its behavior.

---

### SetReplyingTo
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void SetReplyingTo(string? label)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `label` | `string?` | — |

**Returns:** `void`


Shows/clears the tiny contextual indicator in the input frame's title that reflects the current reply target. When you pass a non-null label, the title displays a fragment like "↩ Replying to {label} │ Esc=cancel" to provide context for the reply; passing null clears the indicator. After computing the fragment, the method calls UpdateInputTitle() to refresh the UI immediately so the change is visible to the user.

## Remarks

By centralizing this UI state, SetReplyingTo encapsulates the presentation detail of the reply workflow. It separates the action of initiating a reply from the mechanics of rendering the updated title, and ensures a consistent reply-indicator string is used across the input area.

## Example

```csharp
// Within the same class context
SetReplyingTo("Alice");
SetReplyingTo(null);
```

## Notes

- This method mutates UI-related state and should be invoked on the UI thread to avoid cross-thread access issues when updating the window chrome.

---

### SetStagedAttachments
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void SetStagedAttachments(IReadOnlyList<string> fileNames, string asciiSizeLabel)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `fileNames` | `IReadOnlyList<string>` | — |
| `asciiSizeLabel` | `string` | — |

**Returns:** `void`


Updates the attachment staging indicator shown on the input frame's title, including the current ASCII-art size for images. Passing an empty list restores the default hint.

## Remarks
Centralizes attachment-state presentation in the input UI, so the title reflects what is staged without scattering formatting logic across callers. It updates a staged flag and builds a compact label that shows the count, a short preview of names, and the ASCII size label, then refreshes the title via UpdateInputTitle(). This makes it easy to adjust formatting (e.g., truncation length) in one place while keeping the input frame in sync.

## Notes
- The method assumes fileNames is non-null; passing null will throw a NullReferenceException when accessing Count.

---

### ShowError
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void ShowError(string message)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `message` | `string` | — |

**Returns:** `void`


Shows an error message to the user by presenting a modal dialog with the title 'Error' and the provided message. It delegates to MessageBox.ErrorQuery to render the dialog, using the current application context (_app) to display the UI. This lightweight wrapper ensures a consistent, centralized way to surface user-facing errors across the EchoHub client UI.

## Remarks
By consolidating error presentation here, callers don't need to know about the underlying message box details; the wrapper encapsulates the UI intent. This helps ensure a consistent dialog structure (title 'Error', an 'OK' button) while leaving room to enhance behavior later (e.g., localization, logging) without touching every call site.

## Example
```csharp
ShowError("Unable to load user data.");
```

## Notes
- Ensure _app is non-null and this call runs on the UI thread.
- This method is synchronous and blocks until the user dismisses the dialog; avoid calling from long-running background tasks.


---

### ShowMessageContextMenu
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void ShowMessageContextMenu(ChatLine line, System.Drawing.Point screenPosition)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `line` | [`ChatLine`](Chat/ChatLine.cs.md) | — |
| `screenPosition` | `System.Drawing.Point` | — |

**Returns:** `void`


It builds and shows a per-message right-click context menu for a chat line at a given screen position, combining attachment actions, reply/mention/profile options, copy operations, and a delete action subject to server-side permissions. Use it to provide a consistent, feature-rich set of line-specific actions when a user invokes the context menu on a message line (e.g., via right-click).

## Remarks
This method centralizes the per-line action surface for chat messages, deriving available options directly from the message state. It conditionally adds actions based on the presence and type of attachments, existence of a sender, and whether the message can be replied to or deleted. By assembling a flat list of MenuItem actions and delegating to a PopoverMenu, it decouples the action presentation from the rest of the UI and ensures a uniform user experience across message kinds. The method also demonstrates how user interactions trigger higher-level callbacks (e.g., opening attachments, replying, mentioning, viewing profiles, copying text, or requesting deletion) while managing UI focus and popover lifecycle.

## Notes
- The context menu is short-circuited if there are no actionable items, avoiding an empty popover.
- Reply content is derived by stripping the leading header from the line's textual representation; if the header pattern is absent, the full line is used.
- Delete actions route through ConfirmDeleteMessage, reflecting server-side permission checks rather than performing client-side deletion.
- Attachment-related actions are selected based on AttachmentKind, with sensible defaults for unknown kinds.


---

### ShowSearchDialog
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void ShowSearchDialog()
```

**Returns:** `void`


Signals the UI to display the search dialog by invoking OnSearchRequested, but only if there are subscribers. This private helper is called when a search action is requested, decoupling the action from the actual dialog presentation and allowing the UI to respond without the ShowSearchDialog method needing to know how the dialog is shown.

## Remarks
This method acts as a small abstraction layer between the user action and the dialog presentation. By centralizing the emission of the OnSearchRequested event, the MainWindow class remains focused on orchestration rather than UI rendering specifics. Subscribers can provide or adjust the search behavior without modifying callers, aiding testability and future UI changes.

## Notes
- The invocation is synchronous on the calling thread; a long-running event handler will block the caller unless it offloads work.
- The method does not itself create or show any UI; it merely signals interested parties via OnSearchRequested.

---

### StageFiles
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void StageFiles(IReadOnlyList<string> files)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `files` | `IReadOnlyList<string>` | — |

**Returns:** `void`


Stages the specified files as a single batch of attachments for the active channel. If there is no current channel, the method exits without staging. When a channel exists, it triggers the OnFilesStaged event to hand off the batch to the sending workflow; the next Enter press will transmit these attachments together with any caption the user has typed.

## Remarks
StageFiles acts as a tiny adapter between the user action (dropping or pasting files) and the message-sending pipeline. By emitting OnFilesStaged instead of performing the send itself, it keeps the UI concerns separate from transport mechanics and allows multiple components to react to the staging event. The method relies on the presence of a current channel to decide whether staging is meaningful, embodying a guard that prevents accidental attachment uploads when not in a channel.

## Notes
- If there is no active channel, this method is a no-op.
- OnFilesStaged is invoked only when there is a channel; if there are no subscribers, nothing happens.
- There is no in-method validation of file paths; validation is delegated to downstream handlers.

---

### SwitchToChannel
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void SwitchToChannel(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `void`


SwitchToChannel switches the chat view to the specified channel. It updates the underlying message state to reflect the new current channel, updates the chat window title to show the channel (prefixed with a #), clears the unread count for that channel, and triggers a sequence of UI refreshes to keep the display in sync: the channel list, the displayed messages, the topic bar, and the input’s read-only state. It also marks the status area for redraw and, if the channel exists in the known list, selects it in the channel list to align the selection with the active channel.

---

### ToggleUsersPanel
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void ToggleUsersPanel()
```

**Returns:** `void`


ToggleUsersPanel flips the panel visibility flag and then refreshes the layout to reflect the new state. This method is typically bound to the F2 keyboard shortcut, enabling users to quickly show or hide the online users panel without interacting with UI controls.

## Remarks
Encapsulating the state change and the layout refresh in a single method keeps the UI logic cohesive and discoverable. Callers can rely on this method to perform the complete show/hide action, rather than mutating internal fields directly, which helps prevent inconsistent presentation. If the binding for F2 changes, the toggle behavior remains centralized here.

## Notes
- Ensure calls occur on the UI thread; invoking from a background thread may require marshaling to the UI thread before touching UI state.

---

### TryAutocompleteCommand
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void TryAutocompleteCommand()
```

**Returns:** `void`


Tab-complete slash commands in the input field. This method provides a lightweight command-entry UX by auto-completing a slash command when the user types a leading slash with no spaces, using the available SlashCommands list. If exactly one match exists, it completes to that command plus a trailing space; if multiple matches exist, it computes the longest common prefix among them and applies it when it extends beyond the current input. Finally, it moves the caret to the end of the input to prepare for continued typing.

## Remarks
Provides an in-place UX enhancement for command entry; it reads from the in-memory SlashCommands collection and does not trigger any external calls. It treats matches case-insensitively and updates the input field accordingly, leaving the user to continue typing after the completion.

## Notes
- Mutates only when the input begins with '/' and contains no spaces; otherwise it exits without changes.
- When multiple matches exist, the prefix calculation starts from the first match in the list; the resulting auto-prefix can depend on the list ordering.
- The caret is always moved to the end of the text, regardless of whether any text was changed.

---

### UpdateInputReadOnly
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void UpdateInputReadOnly()
```

**Returns:** `void`


Disables the input for read-only (system) channels so nothing can be typed there, and reflects the state in the input frame title. It does this by setting the input field's ReadOnly flag based on IsCurrentChannelReadOnly and then calling UpdateInputTitle to synchronize the title with the current state.

## Remarks
This small helper centralizes UI state synchronization: the input interactivity and the title reflect the channel's read-only status from a single source of truth (IsCurrentChannelReadOnly). By keeping UpdateInputReadOnly as the single place that applies this policy, changes to channel permissions automatically propagate to the input control and its label, ensuring consistent feedback to the user.

## Notes
- Ensure this runs on the UI thread to avoid cross-thread access issues when manipulating UI controls.
- If IsCurrentChannelReadOnly changes, callers should ensure UpdateInputReadOnly is invoked so the input state and title stay in sync.

---

### UpdateInputTitle
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void UpdateInputTitle()
```

**Returns:** `void`


Updates the input frame title to reflect the current editing state. When the current channel is read-only, the method forces the title to a fixed read-only message and requests a redraw; otherwise it derives the title from the reply and staged title fragments using a small switch expression: if both fragments are absent it uses the default title; if only one exists it uses that one; if both exist it concatenates them with a separator (" │ "). The method ends by signaling the input frame that it needs to redraw.

## Remarks

Centralizes the logic for how the input title is computed from the live fragments, ensuring consistent UI behavior in both read-only and writable channels. It prevents scattering of title-construction logic across the window code and makes it easier to adjust the title policy in one place. By honoring the read-only constraint at this point, it guarantees users always see an accurate, explicit hint about their ability to type.

## Notes

- The read-only branch takes precedence over any fragment values.
- SetNeedsDraw() is invoked after updating the title to refresh the UI; callers should not rely on drawing happening elsewhere.

---

### UpdateLayout
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void UpdateLayout()
```

**Returns:** `void`


Reflows the main UI by resizing the chat, topic, and input frames based on whether the users panel is visible. It computes a right margin equal to the panel width when visible, applies Dim.Fill(rightMargin) to the relevant frames, toggles the users panel frame visibility, and then requests a redraw.

## Remarks

Centralizes the layout reflow logic so callers toggle _usersPanelVisible without duplicating width calculations. It coordinates the main content frames and the users panel visibility, ensuring the UI stays visually aligned whenever the panel appears or disappears. Because it's private, it's meant to be invoked by internal state changes rather than external consumers.

## Example

```csharp
// Example: toggle the Users panel and refresh layout
_usersPanelVisible = true;
UpdateLayout();

_usersPanelVisible = false;
UpdateLayout();
```

## Notes

- Must be called on the UI thread after changing _usersPanelVisible.
- Relies on UsersPanelWidth; ensure it is defined and non-negative when the panel is shown.
- Invoking SetNeedsDraw() schedules a redraw; avoid rapid, consecutive calls from non-UI threads.

---

### UpdateOnlineUsers
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void UpdateOnlineUsers(List<UserPresenceDto> users)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `users` | `List<UserPresenceDto>` | — |

**Returns:** `void`


UpdatesOnlineUsers updates the online users list display by transforming a `List<UserPresenceDto>` into the UI representation used by the user panel. For each user it computes a status icon from their UserStatus, selects a display name (DisplayName if present, otherwise Username), and applies a role tag derived from their ServerRole. If the user is connected through IRC, it appends a [irc] suffix to convey feature context. The name color is determined by first attempting to parse NicknameColor as a hex color; if that fails, it falls back to a deterministic per-nick color from NickColorHelper.GetAttribute, ensuring colors stay consistent with the user's chat messages. The resulting collection is written to the underlying _usersListSource and the frame title is updated to reflect the current number of online users.


---

### UpdateSpinner
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void UpdateSpinner()
```

**Returns:** `void`


Starts the spinner timer when entering a transitional connection state (Connecting, Reconnecting, …); the timer stops itself once the state settles.

The method immediately returns if there is no transitional status or if a spinner timer is already running, ensuring only a single spinner animation is active at a time. When invoked in the proper state, it schedules a recurring callback via _app.AddTimeout with a 120-millisecond interval. Each tick first checks whether the status remains transitional; if it does not, the method clears the timer token and stops frequency updates. If the status is still transitional, it advances the spinner frame by one, wraps around using the length of SpinnerFrames, and requests a redraw of the status label. This creates a smooth, looping spinner animation that runs only as long as the transitional state persists.

Dependencies: TimeSpan, SpinnerFrames

Dependency APIs (verified signatures)

- field SpinnerFrames (src/EchoHub.Client/UI/MainWindow.cs)


---

### UpdateStatusBar
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void UpdateStatusBar(string status)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `status` | `string` | — |

**Returns:** `void`


Updates the connection status displayed in the status bar by updating the internal state, refreshing the spinner, and invalidating the status label for redraw. Call this method whenever the connection state changes (for example, during connecting, when connected, or on disconnection) to keep the status bar in sync with the actual connection status without duplicating UI update logic elsewhere.

## Remarks
Centralizes the status bar update sequence in one place, ensuring the internal state and the visual feedback stay in sync. By encapsulating the setter of _connectionStatus, the spinner refresh, and the redraw trigger, callers avoid partially updated UI states and reduce the risk of stale visuals.

## Example
```csharp
// Example: update the status bar to reflect a connected state
UpdateStatusBar("Connected");
```

## Notes
- Should be invoked on the UI thread to safely update UI elements.
- Frequent updates will trigger spinner refreshes and redraws; consider batching rapid status changes.

---

### UpdateTopicBar
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void UpdateTopicBar()
```

**Returns:** `void`


Show or hide the topic bar based on the current channel's topic.

The UpdateTopicBar method reads the topic associated with the current channel from the _channelTopics collection and updates the topic label and chat layout accordingly. If a non-empty topic is found, it sets the label text to " Topic: {topic}", makes the label visible, and adjusts the chat frame position (Y = 2) to accommodate the topic bar. If there is no topic (or the topic is whitespace), it hides the topic label and resets the chat frame position (Y = 1).

This method uses TryGetValue to avoid exceptions when a channel has no entry and relies on string.IsNullOrWhiteSpace to determine whether a topic should be shown. It encapsulates the small but important UI logic that bridges channel-topic data with the visual layout, ensuring a consistent presentation whenever the current channel or topic changes.

---

### AltQKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode AltQKey = KeyCode.Q | KeyCode.AltMask
```


AltQKey is a private constant KeyCode that encodes the keyboard shortcut Alt+Q by OR-ing KeyCode.Q with KeyCode.AltMask. It’s intended for input checks in the class, allowing a single, readable comparison (e.g., Input.GetKeyDown(AltQKey)) instead of reassembling the combination at every usage.

## Remarks
This abstraction centralizes the Alt+Q shortcut so changes to the shortcut can be made in one place rather than scattered across the codebase. It also communicates intent more clearly than a scattered KeyCode.Q | KeyCode.AltMask in multiple checks. Because it’s a compile-time constant, it’s cheap to inline in input checks.

## Notes
- The Alt+Q shortcut relies on the legacy input system’s KeyCode and AltMask semantics; if migrating to a different input system, this approach may need adjustment.
- Since AltQKey is private, it cannot be referenced from outside this class; expose a public/internal alias or a helper if cross-class usage is required.

---

### CtrlCKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode CtrlCKey = KeyCode.C | KeyCode.CtrlMask
```


CtrlCKey defines the keyboard shortcut Ctrl+C as a single KeyCode value by bitwise OR-ing the C key with the Ctrl modifier. Use this constant when your input handling needs to react to the Ctrl+C shortcut, instead of composing KeyCode.C and KeyCode.CtrlMask in every check.

## Remarks
Centralizes the copy shortcut into a single, reusable symbol. It prevents duplicated magic-number logic across input checks and makes future changes to the shortcut trivial. Because the field is private const, it stays encapsulated within its containing class and provides a stable value for all internal listeners that rely on the same representation. This relies on KeyCode supporting modifier flags, enabling concise expression of keyboard shortcuts.

---

### CtrlVKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode CtrlVKey = KeyCode.V | KeyCode.CtrlMask
```


Represents the Ctrl+V keyboard shortcut used to trigger paste-like actions within the UI. Implemented as a private const KeyCode that combines KeyCode.V with KeyCode.CtrlMask into a single composite value. Use CtrlVKey in input handling within MainWindow to detect paste attempts instead of duplicating the key-check logic in multiple handlers.

## Remarks
Centralizing the shortcut reduces duplication and keeps the paste-trigger logic consistent across the UI layer. The private visibility confines the behavior to the MainWindow class, supporting cohesive input handling without leaking implementation details. If cross-platform consistency is required, consider exposing a platform-aware abstraction (for example, mapping Cmd on macOS to Ctrl on Windows) to avoid surprising users.

## Notes
- This is a compile-time constant; it cannot be reconfigured at runtime, so any need to support dynamic key bindings would require a different approach (e.g., a settings-backed binding).
- Relying on KeyCode.CtrlMask ties the value to the framework's modifier encoding; ensure it aligns with input handling elsewhere in the app to prevent mismatches.

---

### CtrlXKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode CtrlXKey = KeyCode.X | KeyCode.CtrlMask
```


CtrlXKey encodes the Ctrl+X keyboard shortcut as a single KeyCode value by performing a bitwise OR between KeyCode.X and KeyCode.CtrlMask. This provides a readable, centralized way for the main window's input handling to detect the Ctrl+X combination, avoiding scattered modifier checks throughout the code.

## Remarks
Centralizes the shortcut definition to reduce duplication and make future changes easier. Keeping the constant private confines the shortcut to the UI input logic, preventing misuse from unrelated parts of the codebase. If the underlying input system evolves to treat modifiers separately from keys, this constant may need to be revisited to ensure Ctrl+X is still detected correctly.

## Notes
- The approach relies on KeyCode being a flags-like enum so that combining X with the Ctrl modifier via a bitwise OR yields a meaningful single value. If the input API changes, CtrlXKey may no longer reflect the intended shortcut.
- Because CtrlXKey is private, always reference this constant within the class that handles keyboard input to avoid diverging shortcuts; duplicating the literal elsewhere risks inconsistency.

---

### CtrlYKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode CtrlYKey = KeyCode.Y | KeyCode.CtrlMask
```


Represents the Ctrl+Y keyboard shortcut as a single KeyCode value. Use this constant in the MainWindow's input handling to detect the Ctrl+Y combination without duplicating the modifier logic at each call site.

## Remarks
Centralizes the hotkey for the main window, ensuring consistent behavior and making future changes easy to propagate. Keeping it private encapsulates the shortcut within the MainWindow class, reducing the risk of misuse and enabling compiler-level inlining for performance.

## Notes
- Platform and input-system differences can affect how modifiers are interpreted; verify behavior on all target platforms.
- If other components need the same shortcut, avoid duplication by exposing a controlled API instead of re-declaring the same KeyCode combination.

---

### EnterKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode EnterKey = KeyCode.Enter
```


EnterKey is a private constant field of type KeyCode that represents the Enter key. It exposes KeyCode.Enter as a named value so input-handling code can compare against EnterKey directly, for example in switch statements, without relying on the raw KeyCode.Enter literal or invoking Key.Equals (which also accounts for a Handled state). This small alias keeps the Enter binding centralized and makes the code's intent clearer.

## Remarks
Using EnterKey communicates intent and reduces the use of magic constants in input logic. Since it's private, only members within the containing type can reference it, keeping the binding decision encapsulated. If you later need to reuse the same binding from other types, consider exposing a non-private alias or extracting this pattern to a shared helper.

## Notes
- Because it is const, its value is inlined at compile time; changing it requires recompilation of all assemblies that reference it.
- Encapsulation matters: private scope confines use to this type; expose it with a public/internal alias if cross-type reuse is required.
- The alias assumes the Enter key maps to KeyCode.Enter; if the underlying enum changes, update this constant accordingly.

---

### F2Key
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode F2Key = KeyCode.F2
```


Defines a private, compile-time alias for the F2 keyboard key as a KeyCode value. Within the MainWindow class, this F2Key field is used instead of sprinkling KeyCode.F2 directly in input handling, improving readability and making future key-rebinding easier to manage in one place.

## Remarks
This symbol centralizes the F2-key representation, reducing duplication and making the code intent clear when handling keyboard input. As a private const, it is inlined at all call sites and cannot be reassigned at runtime, preserving a stable mapping inside the class. It helps decouple the key's meaning from its concrete enum value, so refactoring the underlying KeyCode reference requires changing only this single declaration.

## Notes
- Const fields are implicitly static and are inlined at compile time. If you need to support runtime reconfiguration of the key binding, convert this to a visible, non-const field or a configurable option.

---

### SlashCommands
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly string[] SlashCommands =
    [
        "/status", "/nick", "/color", "/theme", "/send", "/me", "/banner",
        "/avatar", "/profile", "/servers", "/join", "/passwd", "/leave", "/clear", "/size", "/downloadpath",
        "/topic", "/users", "/kick", "/ban", "/unban",
        "/mute", "/unmute", "/role", "/invite", "/export", "/deleteaccount",
        "/nuke", "/test-sound", "/quit", "/help"
    ]
```


SlashCommands is a private static readonly array of strings that enumerates the available slash commands used by the tab-autocomplete in the main window. The UI consults this list to offer command suggestions as the user types a leading slash.

## Remarks
This centralized catalog ensures consistency across the autocomplete experience and acts as the single source of truth for which commands are supported. Its private scope keeps coupling tight to the UI implementation, and the readonly modifier prevents reassigning the array reference at runtime, preserving the integrity of the command set. The list includes commands like /status, /nick, /color, /theme, /send, /me, /banner, /avatar, /profile, /servers, /join, /passwd, /leave, /clear, /size, /downloadpath, /topic, /users, /kick, /ban, /unban, /mute, /unmute, /role, /invite, /export, /deleteaccount, /nuke, /test-sound, /quit, and /help.

## Notes
- The initializer syntax shown in the snippet uses square brackets [], which is not valid in C# for an array initializer; the actual source should use a braces-based initializer such as: private static readonly string[] SlashCommands = new[] { "/status", "/nick", ... } or private static readonly string[] SlashCommands = { "/status", "/nick", ... }.

---

### SpinnerFrames
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly string[] SpinnerFrames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"]
```


SpinnerFrames defines the frames of the Braille spinner shown while the connection is in a transitional state. Use this sequence as the source of frames for a timer-driven animation in the UI when signaling a transitional connection state; external code should not rely on this private field directly.

## Remarks
SpinnerFrames centralizes the frame sequence for the connection-status animation, enabling a single place to tweak the visual rhythm without touching animation logic in multiple places. Its private, static readonly nature encapsulates the detail of the glyphs from consumers and ensures consistency across any UI updates that rely on this spinner. The glyphs are Unicode braille patterns, chosen to render as a compact and legible motion, but their appearance depends on font support in the host UI.

## Notes
- Because the field is readonly, you cannot reassign SpinnerFrames to a new array, but its contents can still be mutated if code within the class changes elements; to enforce true immutability consider exposing as `IReadOnlyList<string>` or copying to an immutable collection.
- Braille glyph rendering depends on font support; ensure the UI uses a font that includes these characters, otherwise fallback glyphs will appear.

---

### StatusActivityAttr
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Attribute StatusActivityAttr = new(new Color(80, 200, 220), Color.None)
```


StatusActivityAttr is a private static readonly field that holds a preconfigured Attribute instance used to style status-activity indicators in the UI. It is initialized with a Color(80, 200, 220) and a secondary color of Color.None, providing a consistent visual token that other UI components can apply without duplicating construction logic.

## Remarks
It centralizes the styling token for status activity, ensuring a uniform look across the MainWindow's status displays. Because the field is static and readonly, the same Attribute instance is reused by all usages within its declaring type, reducing allocations and keeping styling decisions centralized. If the Attribute type is mutable, modifications to the instance would propagate to every consumer; prefer treating StatusActivityAttr as effectively immutable or clone it when variations are required.

## Notes
- This field is private; it is intended for internal use within its containing type.
- Mutating the underlying Attribute would affect all referents of StatusActivityAttr if allowed.
- If you need a different color or variant, instantiate a new Attribute rather than adjusting this shared field.

---

### StatusBrandAttr
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Attribute StatusBrandAttr = new(new Color(218, 165, 32), Color.None)
```


This private static readonly field holds a pre-constructed Attribute instance used to apply the application's status branding color in the UI. It centralizes the gold brand color (RGB 218,165,32) and uses Color.None for the secondary color, enabling consistent styling of status indicators across MainWindow without repeatedly allocating new Attribute objects.

## Remarks
StatusBrandAttr serves as a small branding primitive: it encapsulates the branding color data in a single, shared value so UI rendering code can consistently decorate status indicators. As a private member, its usage is confined to the class, reducing risk of styling drift and making updates to the brand color straightforward. The static readonly pattern also avoids per-instance allocations, which helps keep the UI responsive during frequent status refreshes.

## Notes
- This field is private; external code cannot access StatusBrandAttr. If you need external reuse, add an accessor or an API that exposes the color or attribute.
- Color.None is used as the secondary color; callers should not rely on a non-null accent color being provided through this attribute.
- Because it's static and initialized inline, the field is created once per AppDomain; changes to the initializer are global to the class consumers.

---

### StatusConnectedAttr
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Attribute StatusConnectedAttr = new(new Color(0, 200, 0), Color.None)
```


StatusConnectedAttr is a private static readonly field that provides a prebuilt Attribute instance representing the UI styling for a connected status. It is constructed with a green primary color (0, 200, 0) and Color.None as the secondary color, and it is intended to be reused wherever a connected indicator is needed in the MainWindow UI.

## Remarks
Centralizes the appearance of the connected-state styling to avoid duplicating color values across the UI. Being static readonly, it is allocated once and reused, which reduces allocations during frequent UI updates. As a private member, it keeps concerns localized to MainWindow, making it easy to swap or adjust the connected appearance in one place if the theme changes.

---

### StatusDisconnectedAttr
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Attribute StatusDisconnectedAttr = new(new Color(220, 50, 50), Color.None)
```


StatusDisconnectedAttr is a private static readonly Attribute that encapsulates the UI styling used to communicate a disconnected state in MainWindow. It specifies a reddish foreground color (RGB 220, 50, 50) and no background, enabling a clear, consistent visual cue when the application loses its connection.

## Remarks
This member centralizes the visual representation of the 'disconnected' state, ensuring all indicators share the same look. Its static readonly nature guarantees the attribute is created once and reused, promoting performance and consistency across the UI. Because the field is private, it is not directly reusable by other components; if cross-component reuse is needed, consider extracting the styling into a shared resource or exposing a controlled accessor.

## Notes
- Not accessible outside the declaring type; if you need to reuse this styling elsewhere, factor it into a shared resource or provide a public accessor.

---

### StatusMentionAttr
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Attribute StatusMentionAttr = new(new Color(230, 140, 60), Color.None)
```


StatusMentionAttr defines a shared, immutable Attribute instance used to render status mentions with a warm orange color in the UI. A developer would reference this field when they need a consistent highlight for status mentions instead of constructing a new Attribute each time.

## Remarks
Centralizes the visual treatment for status mentions, ensuring consistent appearance across the UI and simplifying future theming. Because the field is static readonly, the color choice is determined at type initialization and cannot be changed at runtime, which prevents accidental mutation. Keeping the field private confines its usage to the containing type, making the intended styling an internal concern that can be adjusted without leaking implementation details.

## Notes
- The field is private, so external consumers cannot reference it directly; reuse must occur within the declaring class or through a controlled API.
- The color is baked into the initialization; updates require recompilation, so plan color theming accordingly.

---

### StatusTransitionalAttr
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Attribute StatusTransitionalAttr = new(new Color(220, 180, 0), Color.None)
```


StatusTransitionalAttr is a private static readonly Attribute that encapsulates the visual styling for elements representing a transitional state in the EchoHub client’s main window. It is initialized with an amber color (RGB 220, 180, 0) and a secondary color of Color.None, providing a single, reusable styling token to ensure consistent amber emphasis for in-progress or transitioning UI elements rather than sprinkling color literals throughout the code.

## Remarks
By centralizing the transitional-state styling in a single field, the codebase gains a clear semantic signal for 'in-progress' statuses and can adapt to theme changes in one place. Because StatusTransitionalAttr is static and readonly, it acts as a stable styling token that can be applied wherever a transitional state needs highlighting without risking inconsistencies. The amber color choice communicates a cautionary or temporary state to users, and the absence of a secondary color keeps the emphasis on the primary transition cue.

## Notes
- The field is private to MainWindow.cs; external code cannot reuse StatusTransitionalAttr directly.
- The second constructor argument is Color.None; its exact meaning depends on the Attribute API—consult its documentation if you need to extend this with a secondary color or outline.

---

### TabKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode TabKey = KeyCode.Tab
```


TabKey is a private constant alias for KeyCode.Tab used within the class to refer to the Tab key in a more readable and centralized way. It maps directly to the Tab key code and is typically used wherever the code needs to detect or respond to tab-navigation input without scattering KeyCode.Tab throughout the logic.

## Remarks

By providing a private const alias, this symbol communicates intent (tab-navigation handling) while keeping the public surface area uncluttered. It also ensures the tab key value is inlined at call sites for performance, without exposing the alias to consumers.

## Notes

- Const inlining: TabKey's value is baked into call sites; changing KeyCode.Tab in the framework requires recompilation to reflect the new value.
- Scope: TabKey is private; it's not accessible outside this class. If cross-class usage is needed, consider exposing it or using KeyCode.Tab directly.

---

### UsersPanelWidth
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const int UsersPanelWidth = 22
```


Defines the fixed width of the Users panel in the main window UI. Use this constant when sizing or laying out the Users panel to ensure a consistent width without sprinkling magic numbers in the UI code; it is a private const within MainWindow.cs, so its value is inlined at compile time and encapsulated from external code.

## Remarks
Centralizing this width as a private constant prevents magic numbers from scattering through layout logic and clarifies the intent behind the panel's sizing. It keeps the responsibility for the UI's sizing localized to MainWindow.cs, reducing cross-cutting dependencies. When design changes are needed, updating this single constant updates all layout paths that reference it, lowering the risk of inconsistent widths. It also communicates that this width is a design-time decision rather than a user-configurable setting.

## Notes
- Because it's const, the value is baked into compiled code at each usage, so changing it requires recompilation of all assemblies that reference it.
- Being private, external classes cannot rely on this constant; if sharing is needed, consider making it internal or exposing it via a property.

---

## ClickChannelRegex
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
[GeneratedRegex(@"(?<!\w)#((?=.*[a-zA-Z])[\w-]+)")]
    private static partial Regex ClickChannelRegex()
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `@"(?<!\w)#((?=.*[a-zA-Z])[\w-]+)"` | — | — |


ClickChannelRegex is a source-generated helper that returns a pre-compiled Regex instance configured to detect channel-style hashtags in text. It matches tokens that start with a '#' and are not immediately preceded by a word character, limits the token to word characters or hyphens, and requires at least one letter to be present. This private static partial method is typically invoked to obtain a compiled Regex at runtime for parsing UI text (for example, to identify clickable channel mentions) without incurring the cost of compiling the Regex on every use.

## Remarks
Centralizing the channel-hashtag detection logic in a single place provides a consistent rule for identifying channel mentions. Using the GeneratedRegex attribute ensures the pattern is compiled at build time, offering fast, repeated matching without runtime compilation overhead. The negative lookbehind (?<!\w) prevents matching a hashtag that is part of a larger word, and the [\w-]+ token constrains channel names to word characters and hyphens, aligning with common channel naming conventions. Keeping this method private preserves its role as an internal parsing helper and avoids leaking implementation details.

## Notes
- The method is private; external code cannot call it directly. Use it through internal logic or expose a public API that consumes its matches. 
- The pattern requires at least one alphabetic character in the channel name; purely numeric channel tokens (e.g. "#123") will not be matched.

---

## ClickMentionRegex
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
[GeneratedRegex(@"(?<!\w)@([\w-]+)")]
    private static partial Regex ClickMentionRegex()
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `@"(?<!\w)@([\w-]+)"` | — | — |


Detects and returns a precompiled Regex for recognizing user mentions in text that start with '@'. The GeneratedRegex attribute ensures the implementation is produced at compile time and the resulting Regex is cached for fast reuse, so consuming code can simply invoke ClickMentionRegex() without paying allocation or compilation costs on every use. The pattern is (?<!\w)@([\w-]+), which requires that the '@' is not immediately preceded by a word character (to avoid matching emails) and captures the username portion consisting of word characters or hyphens.

## Remarks
By centralizing the detector in a source-generated member, the codebase gains a single source of truth for mention parsing and avoids duplicating Regex construction across callers. The private static partial nature means the actual regex is generated and consumed only within the class, providing performance benefits while keeping the API surface small. This pattern is particularly valuable in UI parsing where mentions trigger linkification or notification actions.

## Notes
- The pattern uses a negative lookbehind to avoid matching the '@' inside email addresses.
- The username portion uses [\w-]+, allowing Unicode word characters, digits, underscore, and hyphens; adjust if your allowed username set differs.
- The method is private; to reuse externally, expose a public wrapper or move the logic to a shared helper.

---

## AppVersion
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
internal static readonly string AppVersion =
        typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "?"
```


AppVersion is a read-only string that captures the application's version by reading the MainWindow assembly version and formatting it as major.minor.build. If the version isn’t available, it falls back to a single question mark. Because it is internal, static, and readonly, the value is computed once and can be consumed by UI code or logging without repeating assembly lookups. A developer would reach for it when displaying the application version (for example in an About dialog) or when including the version in diagnostic output.

## Remarks
It serves as a centralized, read-only source of the app’s version for the UI layer, ensuring a single, consistent string is used across dialogs and logs rather than duplicating assembly-version lookups.

---

## CtrlKKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode CtrlKKey = KeyCode.K | KeyCode.CtrlMask
```


Represents the keyboard shortcut Ctrl+K as a KeyCode value by combining the K key with the Ctrl modifier. This private constant centralizes the hotkey used within MainWindow, enabling the code to detect or respond to Ctrl+K without scattering the modifier logic across methods.

## Remarks

By centralizing the shortcut into CtrlKKey, the codebase avoids duplicating the same key-combination logic and makes future changes straightforward (e.g., changing the shortcut would only require updating this single declaration). The naming makes intent clear: it is a Control-K hotkey, distinct from plain K or other modifiers.

## Notes

- Private scope means external code cannot rely on this constant; if external access is needed, expose a public API or event.
- The value depends on the KeyCode and CtrlMask semantics of the project's input system; confirm that CtrlMask is the intended modifier representation to avoid misdetections on other platforms.

---

## DefaultInputTitle
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const string DefaultInputTitle = "Message │ Enter=send │ Tab=complete │ Ctrl+K=search │ F6=pick message"
```


This private constant defines the default text shown for the message input title in the main window. It provides an on-screen cue about how to interact with the input, listing shortcuts such as Enter to send, Tab to complete, Ctrl+K to search, and F6 to pick a message, which helps users discover available actions without opening a help screen.

## Remarks
Centralizing this label ensures consistent user guidance and avoids duplicating the hint in multiple places. Because the field is private and declared as a const, its value is baked into the assembly and cannot be changed at runtime or localized without refactoring to resources. If localization or runtime configurability is required, this should be moved to a resource string or a configuration mechanism and wired into the UI initialization.

## Notes
- Hard-coded strings hinder localization; consider turning this into a resource string if multi-language support is needed.
- As a private const, the value is fixed at compile time; changing the default requires recompilation and re-deployment.

---

## F6Key
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode F6Key = KeyCode.F6
```


F6Key is a private compile-time constant that represents the F6 keyboard key. Use F6Key in input-handling logic within MainWindow to detect the F6 press instead of sprinkling the literal KeyCode.F6 throughout the code.

## Remarks
Centralizes the key mapping to avoid duplicating KeyCode.F6 and to express intent clearly within the class. Since it is private and const, its value is inlined at compile time and not exposed publicly, keeping the wiring internal to MainWindow. If the F6 binding ever needs to be shared or changed, you would introduce a more general configuration mechanism or expose a public abstraction rather than duplicating the literal in multiple places.

## Example
```csharp
// Example: demonstrate using the F6Key constant in a simple comparison
KeyCode current = KeyCode.F6;
if (current == F6Key)
{
    // handle F6 action
}
```

## Notes
- As a const, the value is baked into the assembly; changing it requires recompilation.
- Because it's private, external code cannot rely on this constant; testing and usage should interact with the class's public surface that uses F6Key.


---

## NewlineKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode NewlineKey = KeyCode.N | KeyCode.CtrlMask
```


NewlineKey is a private compile-time constant that represents the keyboard shortcut used to insert a newline in the UI. It encodes the N key combined with the Ctrl modifier by performing a bitwise OR between KeyCode.N and KeyCode.CtrlMask, allowing input handling to recognize the Ctrl+N shortcut as a single KeyCode value rather than separate checks for a key and a modifier.

## Remarks
Centralizes the shortcut in a single symbol, reducing duplication and avoiding magic numbers in input logic. Being private, it remains an implementation detail of the MainWindow UI, so external code should not rely on it. The input-handling code likely compares the current KeyCode to NewlineKey to trigger newline insertion; using a named constant makes the intent explicit and easier to modify if the shortcut changes.

## Notes
- The value is a compile-time constant; changing the shortcut requires modifying the code and recompiling.
- The combo uses KeyCode.CtrlMask; ensure consistency with other Ctrl-modified shortcuts in the same area.
- On platforms where modifier handling differs, verify that Ctrl+N is recognized as intended.

---