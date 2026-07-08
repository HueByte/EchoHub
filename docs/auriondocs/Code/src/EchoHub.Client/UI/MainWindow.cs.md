# MainWindow.cs

> **Source:** `src/EchoHub.Client/UI/MainWindow.cs`

## Contents

- [MainWindow](#mainwindow)
- [MainWindow (constructor)](#mainwindow-constructor)
- [CurrentChannel](#currentchannel)
- [ApplyColorSchemes](#applycolorschemes)
- [BuildMenuBar](#buildmenubar)
- [ClearAll](#clearall)
- [ClickChannelRegex](#clickchannelregex)
- [ClickMentionRegex](#clickmentionregex)
- [EnsureChannelInList](#ensurechannelinlist)
- [FocusInput](#focusinput)
- [GetChannelNames](#getchannelnames)
- [OnChannelListSelectionChanged](#onchannellistselectionchanged)
- [OnChatViewportChanged](#onchatviewportchanged)
- [OnHistoryPrepended](#onhistoryprepended)
- [OnInputContentsChanged](#oninputcontentschanged)
- [OnInputKeyDown](#oninputkeydown)
- [OnMessageListAccepting](#onmessagelistaccepting)
- [OnMessageListVerticalScrollBarScrolled](#onmessagelistverticalscrollbarscrolled)
- [OnMessagesChanged](#onmessageschanged)
- [OnStatusBarDrawContent](#onstatusbardrawcontent)
- [OnUsersListAccepting](#onuserslistaccepting)
- [OnWindowKeyDown](#onwindowkeydown)
- [RefreshChannelList](#refreshchannellist)
- [RefreshMenuBar](#refreshmenubar)
- [RefreshMessages](#refreshmessages)
- [RemoveChannel](#removechannel)
- [SetChannelTopic](#setchanneltopic)
- [SetChannels](#setchannels)
- [SetCurrentUser](#setcurrentuser)
- [ShowError](#showerror)
- [ShowSearchDialog](#showsearchdialog)
- [SwitchToChannel](#switchtochannel)
- [ToggleUsersPanel](#toggleuserspanel)
- [TryAutocompleteCommand](#tryautocompletecommand)
- [UpdateLayout](#updatelayout)
- [UpdateOnlineUsers](#updateonlineusers)
- [UpdateStatusBar](#updatestatusbar)
- [UpdateTopicBar](#updatetopicbar)
- [AltQKey](#altqkey)
- [AppVersion](#appversion)
- [CtrlKKey](#ctrlkkey)
- [EnterKey](#enterkey)
- [F2Key](#f2key)
- [NewlineKey](#newlinekey)
- [SlashCommands](#slashcommands)
- [StatusBrandAttr](#statusbrandattr)
- [StatusConnectedAttr](#statusconnectedattr)
- [StatusDisconnectedAttr](#statusdisconnectedattr)
- [StatusTransitionalAttr](#statustransitionalattr)
- [TabKey](#tabkey)
- [UsersPanelWidth](#userspanelwidth)

---

## MainWindow
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** class

```csharp
public sealed partial class MainWindow : Runnable
```


MainTerminal.Gui window that hosts the EchoHub chat UI — channel list, message list, input box, status/topic labels, menus and an optional online-users panel. Use this class when embedding or orchestrating the console UI: it exposes the visible controls' behaviour via events (channel selection, message submission, connect/disconnect, profile, theme, load-more, etc.) so higher-level code (an orchestrator) can drive application logic without touching Terminal.Gui internals.

## Remarks
This class is the UI boundary between Terminal.Gui controls and the application orchestration layer. It maintains the views and internal state (channel names/topics/public flags, message manager, users list visibility) and exposes user intent through a set of events rather than directly performing network or persistence actions. The window also contains UI convenience features used by the orchestrator: a list of slash commands for Tab autocomplete, cached Key constants (compare via .KeyCode per the implementation comment), and an F2-controlled users panel size toggle.

## Example
```csharp
// Typical orchestrator hookup: obtain the MainWindow from your app/orchestrator and subscribe
// to the events you implement in application logic. AppOrchestrator exposes a MainWindow property.
var mw = orchestrator.MainWindow;

mw.OnChannelSelected += channel => Console.WriteLine($"Switching to channel: {channel}");
mw.OnMessageSubmitted += (channel, message) => Console.WriteLine($"Send to {channel}: {message}");
mw.OnConnectRequested += () => Console.WriteLine("User requested connect");

// The UI will raise OnLoadMore when the user scrolls to the top of the message list.
mw.OnLoadMoreRequested += () => Console.WriteLine("Load older messages for current channel");
```

## Notes
- UI/Threading: Terminal.Gui controls generally require updates on the UI thread. Ensure event handlers that update the UI or Terminal.Gui controls run on the appropriate main loop thread.
- Event lifecycle: subscribe/unsubscribe explicitly. The window exposes many events; long-lived handlers that are not removed can keep objects alive and cause memory leaks.
- Load-more behaviour: the UI raises OnLoadMoreRequested when the message list reaches the top — handlers should load older messages and append them in a way that preserves scroll position.
- Key handling: the class caches Key constants and compares via .KeyCode to avoid Key.Equals semantics that consider the Handled flag; treat key comparisons accordingly when integrating custom key logic.

---

## MainWindow (constructor)
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** constructor

```csharp
public MainWindow(IApplication app, ChatMessageManager messageManager)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `app` | `IApplication` | — |
| `messageManager` | [`ChatMessageManager`](Chat/ChatMessageManager.cs.md) | — |


Initializes and wires up the main application window for the EchoHub client. This constructor takes an IApplication and a ChatMessageManager, stores them, subscribes to key message events, and constructs the primary user interface layout (menu bar, channels list, topic display, chat area with messages, input region, and online users panel). It also wires up data sources (ChannelListSource, ChatListSource, UserListSource) and binds event handlers for channel selection, message sending, scrolling, and dynamic updates to messages, channels, and user presence. The resulting window is ready to render and interact as soon as the application starts.

## Remarks
By centralizing UI bootstrap and dependency wiring, this constructor isolates view assembly from business logic. It defines the top-level composition of panels and their data sources, ensuring the chat experience stays responsive to live changes in channels, messages, and user presence. Injecting IApplication and ChatMessageManager makes the MainWindow easier to test and mock in isolation.

## Notes
- The constructor subscribes to _messageManager.MessagesChanged and _messageManager.HistoryPrepended; without a corresponding disposal, these subscriptions may keep the window alive beyond its intended lifetime.
- The layout relies on fixed coordinates and Dim helpers; adapt if targeting different screen sizes or terminal dimensions.

---

## CurrentChannel
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** property

```csharp
public string CurrentChannel => _messageManager.CurrentChannel
```


CurrentChannel exposes the name of the currently active channel by delegating to the internal message manager's CurrentChannel property. This read-only accessor provides a convenient way for UI code or business logic to display or react to the current channel without binding directly to _messageManager.

## Remarks
This property is a simple pass-through that mirrors _messageManager.CurrentChannel. It does not cache or modify the value; it always reflects the latest state of the message manager. Exposing it here helps keep the UI layer decoupled from the internal implementation.

## Notes
- Accessing CurrentChannel may throw a NullReferenceException if _messageManager is null or not yet initialized, since the getter accesses its CurrentChannel directly.

---

## ApplyColorSchemes
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void ApplyColorSchemes()
```

**Returns:** `void`


Applies the currently registered color schemes to all views. Call after theme changes to refresh colors. The method retrieves the 'Base' and 'Menu' schemes from SchemeManager; if a base scheme is available, it assigns it to the current view and propagates it to all child views that should use the base scheme, excluding the menu bar and the status and topic labels. If a menu scheme is available, it applies it to the menu bar, status label, and topic label.

## Remarks

Centralizes theming logic, making theme changes simpler to implement. Rather than updating each view individually, you call ApplyColorSchemes and the function coordinates propagation to the appropriate elements. The separation between base and menu schemes allows content views to share a consistent base appearance while chrome elements can adopt a separate menu-focused style.

## Example

```csharp
// Theme changed, refresh UI colors
ApplyColorSchemes();
```

## Notes

- Assumes that SubViews, _menuBar, _statusLabel, and _topicLabel are initialized before calling; otherwise a NullReferenceException may occur.
- If a requested scheme (Base or Menu) is not registered, the corresponding block is skipped gracefully.

---

## BuildMenuBar
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private MenuBar BuildMenuBar()
```

**Returns:** `MenuBar`


BuildMenuBar constructs the top-level MenuBar used by the main window. It creates three menus — File, Server, and User — and populates them with a mix of static commands and dynamic, runtime-driven items. Theme entries are built by enumerating Themes.ThemeManager.GetAvailableThemes(); each theme name becomes a MenuItem that triggers OnThemeSelected with the theme name, and these theme items are prepended with a separator header by placing them under the User menu after a line. The User menu, therefore, contains profile-related actions, a separator, and the dynamically generated theme list.

File items are conditionally augmented when a backup exists, using UpdateBackupService.BackupExists(). If a backup is present, a Rollback item is added (labeling as either Rollback to v{version} or Rollback Update...), followed by a separator line. The File menu always includes Check for Updates and Quit. The Server menu provides typical server lifecycle actions (Connect, Disconnect, Logout), channel management actions (New Channel, Delete Channel), saved servers, and a Toggle Users Panel item. The menu bar is then assembled with File, Server, and User top-level items and positioned at the top-left with full width.

A small UI workaround is applied: each MenuBarItem’s CommandView is marked TransparentMouse to allow clicks to reach the MenuBarItem itself instead of propagating to its sub-view. This stabilizes input handling when selecting menu items.

The method returns a fully configured MenuBar suitable for integration into the main window’s UI lifecycle, with the command wiring left to the surrounding event handlers (OnConnectRequested, OnThemeSelected, OnProfileRequested, etc.).

---

## ClearAll
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void ClearAll()
```

**Returns:** `void`


ClearAll resets the chat UI to a pristine state by wiping all in-memory channel and message data and reinitializing the visual components used to display channels, users, and messages. It is intended to be invoked when disconnecting from the server to ensure no stale state remains before a new session begins.

## Remarks
By centralizing the reset logic in a single method, ClearAll ensures the UI is returned to a consistent baseline after a disconnect. It coordinates updates across multiple bound sources and controls (channel list, channel topics, user list, and the chat frame) so callers don't have to perform these steps individually.

## Notes
- Must be called on the UI thread to avoid cross-thread UI updates.
- This method only clears UI state and internal collections; it does not terminate connections or close the window.
- Calling ClearAll multiple times is safe; the method is effectively idempotent and will leave the UI in an empty state.

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


Parses channel mentions from text by providing a compile-time generated Regex that matches tokens beginning with '#', not immediately preceded by a word character, and intended to include at least one letter. This dedicated, precompiled pattern is exposed via a private static partial method, ClickChannelRegex, so internal parsing paths can cheaply scan input (e.g., chat messages) for channel references without repeatedly constructing Regex objects.

## Remarks
This abstraction centralizes the channel-mention parsing rule within the UI layer. By using a GeneratedRegex, the Regex instance is produced at compile time, delivering fast, repeated matches at runtime and reducing allocation costs compared to building a new Regex on every parse. The match is anchored to a boundary that prevents matching inside words and is designed to skip numeric-only tokens (such as hex colors or simple issue numbers), reinforcing consistent channel extraction across the code that consumes it.

## Notes
- The body of the method is generated by the source generator from the GeneratedRegex attribute; you won’t see an implementation in source code.
- The method is private to the containing class; if reuse outside is needed, wrap it with a public/internal accessor.
- This feature requires .NET 7+ (GeneratedRegex) and the appropriate language/runtime support for source-generated regexes.


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


This private static partial method exposes a pre-compiled Regex used to identify clickable user mentions in text. It matches an at-sign followed by a username consisting of word characters or hyphens, but only when the preceding character is not a word character to avoid matching email addresses. The source generator via GeneratedRegex produces and caches the Regex, so callers can reuse ClickMentionRegex() for efficient mention detection without rebuilding the pattern.

## Remarks
Centralizes mention-detection logic in a single internal helper, ensuring consistent matching across the UI. The pattern is generated at compile time, which reduces allocations and startup overhead compared to constructing a new Regex at every use, and keeps the actual regular expression definition in one place. Being private signals it's an internal concern of the containing class (likely the UI layer) rather than a public API.

## Notes
- The GeneratedRegex attribute implies a source-generated Regex; changes require a rebuild to regenerate the cached instance.
- Because the method is private, usage is restricted to the containing class; consider exposing a controlled API if external consumers need to reuse the same pattern.

---

## EnsureChannelInList
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void EnsureChannelInList(string channelName, bool? isPublic = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `isPublic` | `bool?` | `null` |

**Returns:** `void`


Ensures that a channel name is represented in the left-panel channel list, which is used for private channels joined via /join. If the optional isPublic flag is provided, it updates the internal privacy map for that channel. If the channel is not already tracked, it adds the channel and refreshes the UI by calling RefreshChannelList to reflect the new entry.

## Remarks
This method centralizes the logic for presenting channels in the left panel. By mutating the internal structures _channelNames and _channelPublic, it keeps the UI state in a single place and delegates the actual UI update to RefreshChannelList. It helps ensure consistency between channel membership and what the user sees, particularly for channels joined through /join.

## Example
```csharp
// Example: ensure a newly joined private channel appears in the left panel
var window = new MainWindow();
window.EnsureChannelInList("private-channel-42", isPublic: false);
```

## Notes
- If the channel already exists in _channelNames, the method updates _channelPublic (if provided) but returns immediately, so the UI is not refreshed by this call. To reflect visibility changes for an existing channel, trigger a RefreshChannelList() or another operation that causes a refresh.

---

## FocusInput
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void FocusInput()
```

**Returns:** `void`


FocusInput provides a concise, intent-driven way to move keyboard focus to the main typing field in the window. Call it when you want the user to begin typing immediately (for example after opening the window, after resetting a form, or in response to a typing shortcut) without requiring a manual click. The method delegates the actual focus action to the underlying input control via _inputField.SetFocus(), centralizing focus behavior behind a clear API so future changes to focus handling can be made in one place.

## Remarks
Wrapping the focus call isolates UI focus behavior from business logic and layout code. It offers a single extension point for enhancements (such as accessibility hooks or focus-tracking) and simplifies testing by allowing callers to verify FocusInput was requested without manipulating UI state directly.

## Notes
- Ensure _inputField is initialized before FocusInput is invoked; otherwise you may encounter a NullReferenceException.
- FocusInput only changes focus; it does not alter the field's text or selection. If you need to select or modify the content, perform that separately.

---

## GetChannelNames
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public IReadOnlyList<string> GetChannelNames() => _channelNames.AsReadOnly()
```

**Returns:** `IReadOnlyList<string>`


GetChannelNames exposes the set of channel names that have message buffers, which are used when broadcasting status changes. It returns a read-only view of the internal _channelNames collection, allowing callers to inspect which channels will receive broadcasts without being able to modify the underlying list.

## Remarks
Exposing the collection via `IReadOnlyList<string>` preserves encapsulation: callers can read or enumerate the channel names but cannot mutate the internal list. The view reflects the current state of the internal _channelNames field, so it can change as channels are added or removed. If a stable snapshot is required, copy the contents (e.g., into a new `List<string>`) rather than keeping the live view.

## Notes
- The return value is a live view backed by the internal list; it is not a copied snapshot.
- Enumerating the returned collection while the underlying _channelNames is being modified may throw InvalidOperationException due to concurrent modification.

---

## OnChannelListSelectionChanged
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


Responds to changes in the channel list selection by validating the new index, resolving the corresponding channel name, switching to that channel when it is different from the current one, and notifying listeners via OnChannelSelected.

## Remarks
Acts as the UI-to-state bridge for channel selection. It guards against invalid indices and redundant switches, ensuring a clean state transition only when a new channel is chosen. By invoking SwitchToChannel and then OnChannelSelected, it coordinates the internal channel state with the UI and any interested observers.

## Notes
- Relies on _channelNames being in sync with the UI's channel list; desynchronization may cause selections to be ignored.
- OnChannelSelected is invoked via ?.Invoke to safely handle the absence of subscribers.
- Assumes the method runs on the UI thread; dispatch accordingly if invoked from a background context.

---

## OnChatViewportChanged
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void OnChatViewportChanged()
```

**Returns:** `void`


Responds to changes in the chat viewport width. It reads the new width from _messageList.Viewport.Width and, if the value is positive and different from the last known width, updates _lastChatWidth, informs the message manager of the new width via _messageManager.SetChatWidth(newWidth), and refreshes the chat messages to reflow content for the new width.

## Remarks
This method centralizes width-change handling for the chat UI. By guarding against redundant updates with _lastChatWidth, it avoids unnecessary work when the viewport reports unchanged or non-positive widths. It coordinates three responsibilities—state tracking, layout adjustment, and content refresh—through a single, well-scoped trigger.

## Notes
- Rapid resizes can trigger multiple RefreshMessages calls; consider debouncing or coalescing updates if this becomes a performance concern.

---

## OnHistoryPrepended
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


Keeps the user’s reading position stable when older messages are prepended to the current channel. It runs only for the active channel, fetches the channel’s messages, records how many items were present before the update, refreshes the message list, and if new items were added at the top, selects the item at the offset equal to the number of prepended messages so as to avoid jumping to the top.

## Remarks
Centralizes the user experience detail of preserving scroll position during history growth. It determines how many messages were prepended by comparing the new ChatListSource.Count to the prior value captured before RefreshMessages(), and it only adjusts the selection when that delta is positive, minimizing unnecessary UI motion.

## Notes
- If the Source is not a ChatListSource, oldCount defaults to 0, which can affect the computed prependedCount.
- The early return guards ensure the method does nothing when invoked for a non-current channel or when there are no messages.

---

## OnInputContentsChanged
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


This private input event handler runs in response to changes to the input field's contents. It delegates emoji substitution to EmojiHelper.ReplaceEmoji and then updates the input field's text and caret position so typing continues smoothly after replacement; it uses a guard (_suppressEmojiReplace) to prevent recursive edits caused by programmatic text changes.

## Remarks
By centralizing the emoji replacement logic here, the UI remains consistent when users type or edit text. The handler does not rely on ContentsChangedEventArgs for its behavior; it instead reads the current state of the input field and applies the replacement, making it resilient to different editing patterns. The length delta calculation and cursor repositioning ensure the caret lands immediately after the replaced content, preserving typing flow.

## Notes
- Ensure this runs on the UI thread; mutating _inputField from a non-UI thread can cause exceptions.
- Emoji replacement relies on EmojiHelper.ReplaceEmoji; if that method changes behavior (e.g., new shortcode rules), this handler will inherit those changes.


---

## OnInputKeyDown
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


Handles keyboard interactions in the chat input area. OnInputKeyDown filters specific keys and translates them into UI actions: pressing Tab triggers command autocompletion; pressing Newline inserts a newline into the input field; pressing Enter submits the current message to the active channel (when there is non-empty text and a channel is selected); pressing Alt+Q stops the application; pressing Ctrl+K opens the search dialog. For all matched keys the method marks the event as handled to prevent default handling and to guarantee consistent in-app behavior. The method coordinates with _inputField, _messageManager, _app, and the OnMessageSubmitted event to perform these actions.

---

## OnMessageListAccepting
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


Responds to the user action of accepting a selected message in the chat list. It validates the message list source and the selected line, then applies a prioritized set of actions: if the line has an audio or file attachment, it raises the corresponding event to play or download the attachment; otherwise it analyzes the line text for interactive tokens (a user mention or a channel reference) and navigates accordingly; if none of these apply, it defaults to opening the sender's profile.

## Remarks
Centralizes the interaction model for a message-line acceptance, decoupling UI from business logic by emitting events for playback, download, or navigation. It encodes a clear priority: attachments first, then text-based interactions (mentions, channels), then the sender's profile. This makes it straightforward to adjust behavior by subscribing to the events rather than changing the handler's internal logic.

The handler sets e.Handled = true on every successful path to prevent further processing of the click/accept action, ensuring a single, predictable outcome per accepted line.

## Notes
- Attachment handling requires both AttachmentUrl and AttachmentFileName to be non-null for playback/download to trigger; otherwise the method falls back to text-based interactions.
- The mention/channel detection relies on regular expressions (ClickMentionRegex and ClickChannelRegex). Changes to those helpers could alter which tokens are recognized and how navigation occurs.


---

## OnMessageListVerticalScrollBarScrolled
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


This private scroll event handler translates the user action of scrolling the message list into a request to load older messages. When the vertical scrollbar is at the top (VerticalScrollBar.Value == 0), it raises OnLoadMoreRequested to fetch earlier content; otherwise it remains inert.

## Remarks
By isolating this behavior, the UI logic stays decoupled from the actual loading implementation, making it easy to swap in different loading strategies or to test the scrolling-to-load workflow. It relies on the _messageList's VerticalScrollBar and the OnLoadMoreRequested event to coordinate history loading, which is a common pattern for reverse infinite scrolling (loading earlier items as the user scrolls upward).

## Notes
- There is no internal debounce or reentrancy guard; repeated top-edge scrolls can trigger OnLoadMoreRequested multiple times if the subscriber does not guard against concurrent loads.
- The logic depends on the VerticalScrollBar.Value semantics (top equals 0). If the control's value semantics change, the top-edge condition may need adjustment.

---

## OnMessagesChanged
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


An event handler invoked when the message collection for a channel changes. It decides which part of the UI to refresh by comparing the affected channel to the currently viewed channel: if they match, it refreshes the messages for the active channel; otherwise, it refreshes the channel list to reflect changes elsewhere. This keeps the user’s current view responsive while signaling updates in other channels.

## Remarks
This is a minimal dispatcher that avoids unnecessary work by refreshing only the part of the UI affected by the change. It encapsulates the decision logic behind a private method, preserving a cohesive flow in the main window’s message-handling path. The approach relies on _messageManager.CurrentChannel to determine the active context and delegates to RefreshMessages or RefreshChannelList to update the appropriate UI region.


---

## OnStatusBarDrawContent
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


Renders the status bar content by composing branding, version information, connection state, the current user, and the active channel into the status label, applying the current theme attributes and grapheme-aware width calculations. It is invoked as part of the UI drawing cycle to produce a single, cohesive line that fits within the available viewport width and to prevent default rendering by cancelling the event.

## Remarks
This method centralizes status bar rendering, ensuring consistent theming and presentation across app states. It relies on GraphemeHelper to respect complex character widths and on the theme attributes gathered from SchemeManager or the status label itself. By performing width-aware writes and padding the remainder with spaces, it maintains a stable visual layout even as dynamic state (user, channel, connection) changes.

## Notes
- If the viewport width is zero or negative, the method returns early and draws nothing, avoiding useless work or layout glitches.
- The function sets e.Cancel = true to suppress the default drawing, so callers should not expect any additional rendering after this handler runs.
- It mutates per-segment attributes via Resolve, which substitutes a neutral background when the theme provides Color.None; this behavior depends on the availability of the normal attributes or a scheme.


---

## OnUsersListAccepting
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


Responds to the user accepting an item in the users list. It obtains the selected index, ensures it is non-null and within bounds, resolves the associated username via _usersListSource.GetUsername, and, if a non-null username is produced, raises the OnUserProfileRequested event with that username and marks the command as handled to prevent further processing.

## Remarks
By encapsulating the selection-to-profile flow, this method decouples the UI selection logic from navigation. It centralizes defensive checks for invalid selections (null or out-of-range index) and uses the OnUserProfileRequested event to notify subscribers to display the user's profile, making the interaction easy to test and reuse without embedding navigation logic in the UI control.

## Notes
- If no item is selected or the index is out of range, the method returns without raising the event.
- If a valid index yields a null username, no profile request is issued.
- The event is invoked via OnUserProfileRequested?.Invoke(username), so if there are no subscribers, nothing happens.

---

## OnWindowKeyDown
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


Handles KeyDown events for the main window and routes three global shortcuts: Alt+Q to request application shutdown, F2 to toggle the Users panel, and Ctrl+K to display the search dialog. It checks the pressed key against AltQKey.KeyCode, F2Key.KeyCode, and CtrlKKey.KeyCode, performing the associated action and setting e.Handled = true to prevent further processing when a shortcut matches.

## Remarks
Centralizes keyboard shortcuts in one place for predictable global behavior on the main window. By depending on AltQKey, F2Key, and CtrlKKey for the actual key codes, it decouples the shortcut definitions from the actions invoked, making it easier to adjust bindings without touching the handler logic.

## Notes
- If you add more shortcuts, consider a mapping structure to keep the method maintainable.
- Be mindful that e.Handled is only set when a shortcut matches; non-matching keys propagate to other handlers.

---

## RefreshChannelList
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void RefreshChannelList()
```

**Returns:** `void`


RefreshChannelList updates the channel list UI to show updated channel names along with their unread counts and to reflect the currently active channel. It updates the underlying list source with the channel names, unread counts from the message manager, and the current channel, then binds that source to the UI control and attempts to restore the selection to the active channel by locating its index in the names list.

## Remarks
By centralizing this refresh logic, the UI stays in sync with the application state and preserves user context after channel or message changes. It encapsulates the pattern of computing new data, applying it to the view, and restoring user focus, reducing duplication across callers.

## Notes
- This method updates UI controls; call it on the UI thread to avoid cross-thread exceptions.
- The selection restoration is conditional: if CurrentChannel is not present in _channelNames, the selection is not changed.

---

## RefreshMenuBar
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void RefreshMenuBar()
```

**Returns:** `void`


Rebuilds and replaces the menu bar to reflect changes in the UI, such as updated theme lists. It removes the current _menuBar, rebuilds a fresh instance via BuildMenuBar(), inserts it back into the UI, applies the current color schemes, and requests a redraw so the new visuals are painted.

## Remarks
By centralizing the refresh logic, this method ensures the menu bar always reflects the latest theme decisions and styling. It coordinates a tight sequence: remove the old bar, construct a new one, add it to the window, apply color schemes, and invalidate the display for drawing.

## Example
```csharp
// After the theme list changes at runtime
RefreshMenuBar();
```

## Notes
- UI mutations should occur on the main/UI thread to avoid cross-thread exceptions.

---

## RefreshMessages
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void RefreshMessages()
```

**Returns:** `void`


RefreshMessages repopulates the chat list for the current channel. It fetches the messages, wraps each line to the viewport width when available, and pushes the result into a ChatListSource bound to the UI. If there are messages, it selects the last item to show the latest chat.

## Remarks
To support dynamic resizing, the method caches the last known chat width and uses it when the viewport hasn't been laid out yet. Wrapping is performed via line.Wrap(width, line.ContinuationIndent) to maintain indentation across wrapped lines, improving readability of long messages. When there are no messages, the source is reset to an empty ChatListSource.

## Dependencies
- ChatListSource
- Viewport

## Dependency APIs
- class ChatListSource (src/EchoHub.Client/UI/Chat/ChatListSource.cs)
  - int Count
  - int MaxItemLength
  - bool SuspendCollectionChangedEvent
  - void Add(ChatLine line)
  - void AddRange(`IEnumerable<ChatLine>` lines)
  - void InsertRange(int index, `IEnumerable<ChatLine>` lines)
  - void Clear()
  - ChatLine? GetLine(int index)
  - bool IsMarked(int item)
  - void SetMark(int item, bool value)
  - IList ToList()
  - void Render(ListView listView, bool selected, int item, int col, int row, int width, int viewportX)
  - …and 3 more member(s) not shown

## Notes
- The selection of the last item only happens when source.Count > 0; otherwise, the list remains empty.
- If the viewport width has not been established yet, wrapping falls back to the last known width, which may delay proper wrapping on first layout.

---

## RemoveChannel
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


Removes a channel from the left panel by deleting its entries from internal state collections and then refreshing the panel. Use this when the user chooses to remove a channel; it encapsulates the multi-collection cleanup and UI refresh in a single operation to keep the left panel in sync with the data model.

## Remarks

This method centralizes removal logic so all left-panel representations stay in sync (names, topics, and visibility/public flags). It reduces the risk of partial state by avoiding callers mutating multiple collections directly, and it ensures that a UI refresh is performed consistently after the mutation.

## Example

```csharp
// Example: remove a channel named "General" from the left panel
RemoveChannel("General");
```

## Notes

- Must be invoked on the UI thread; cross-thread calls may throw or cause inconsistent UI state.
- If channelName is not present in the internal collections, no error is raised; the method simply leaves state unchanged.

---

## SetChannelTopic
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


Sets the topic for a specific channel by updating the internal per-channel topic store and, if the target channel is currently active, refreshes the UI to display the new topic.

The method assigns the provided topic to the channelName entry in the _channelTopics dictionary. If the channel being updated is the one currently shown in the UI (as identified by _messageManager.CurrentChannel), it triggers a UI refresh via UpdateTopicBar to ensure the topic bar reflects the latest value. Passing null as the topic clears the topic for that channel.

## Remarks

Topics are stored in a per-channel dictionary, which decouples topic data from rendering logic and allows quick lookups when rendering different channels. The conditional UpdateTopicBar call minimizes UI work by refreshing only when the active channel's topic changes.

## Example

```csharp
SetChannelTopic("general", "Welcome to the general channel!");
```

## Notes

- A null topic clears the topic for the specified channel.
- If this method may be invoked from non-UI threads, consider synchronization to avoid race conditions when updating _channelTopics and refreshing the UI.

---

## SetChannels
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


Sets the current list of available channels in the UI by consuming a collection of ChannelDto objects and rebuilding the internal caches, followed by a refresh of the display. It clears the in-memory channel state (_channelNames, _channelTopics, and _channelPublic), repopulates these structures from the provided channels, and then calls RefreshChannelList to reflect the changes in the UI.

## Remarks

This method decouples the data payload from the UI representation and provides a single, coherent update path for the channel list. By maintaining separate caches for names, topics, and visibility, the code can render or filter the list efficiently and keep the visual state in sync with the data. It serves as a single entry point for channel-list updates, which helps ensure consistency whenever the channel collection changes.

## Notes

- Null input isn't validated; passing null will throw a NullReferenceException.
- Should be invoked on the UI thread since it updates UI state and triggers a UI refresh.
- If multiple ChannelDto entries share the same Name, the last one wins due to dictionary assignment; upstream data should avoid duplicates.

---

## SetCurrentUser
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


Sets the active user name and forwards it to the message manager for at-mention detection. Call this when the user signs in or switches identity so future messages are attributed to the correct user and mentions resolve against the current user.

## Remarks
By delegating to the message manager, the UI remains focused on presentation while the mention-parsing logic stays centralized. This thin wrapper ensures a single source of truth for the active user across features that rely on @mention detection. Updating the current user through this method affects all areas that rely on the message manager’s notion of the active user.

## Example
```csharp
// Switch the active user context to Alice
SetCurrentUser("Alice");
```

## Notes
- The wrapper does not perform input validation; it passes the provided username straight to the underlying message manager. If validation or normalization is required, handle it prior to calling this method or implement it within the message manager.

---

## ShowError
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


ShowError shows an error message to the user by opening a modal error dialog using the shared MessageBox helper. It standardizes error presentation by always using the title 'Error' and an 'OK' button, and by routing through the application's context (_app) to ensure the dialog is shown within the current UI.

## Remarks
By centralizing error display, this method enforces a consistent look and behavior for error messages across the UI layer. It hides the details of the underlying MessageBox API from callers and ties the dialog to the application's UI context, making it easier to adapt the look-and-feel in one place. In addition, it signals to developers that user-facing errors should be surfaced to the user, not logged silently.

## Notes
- Must be invoked on the UI thread; calling from a background thread may raise threading issues or cause the dialog not to render.
- Fixed title and button label; for localization or customization, extend or overload.
- Relies on _app being initialized; ensure the UI is running before showing.

---

## ShowSearchDialog
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void ShowSearchDialog()
```

**Returns:** `void`


ShowsSearchDialog raises the OnSearchRequested event to signal that the search UI should be shown. It does not open the dialog directly; instead, it notifies any subscribers that a search has been requested, allowing the actual UI presentation to be supplied by the hosting layer or a consumer of the event.

## Remarks

Encapsulates the trigger of the search UI behind a small, private method. This decouples the window from the dialog implementation, letting a consumer decide how to present the search UI and enabling easier unit testing by substituting a test handler for OnSearchRequested.

## Notes

- If no OnSearchRequested handler is attached, the method is a no-op due to the null-conditional invocation.
- Being private, this method is not part of the public API surface; external components should interact with the event or other public entry points to request a search.


---

## SwitchToChannel
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


## Source Code
SwitchToChannel switches the chat view to the specified channel, updating the current channel state, and setting the window title to reflect the channel name. It also clears the unread count for that channel and refreshes the channel list, messages, and topic bar so the UI stays consistently synchronized when the user switches channels.

## Remarks
Centralizing all channel-switch side effects in one method prevents inconsistent UI state that could arise from scattered updates. It coordinates multiple components (the message manager, chat frame title, channel list, topic bar, and status label) to ensure the active channel is reflected consistently across the UI. The early assignment of the current channel followed by the refresh steps ensures that subsequent updates observe the new context.

## Notes
- If channelName isn't present in _channelNames, the selection won't be updated (the code checks idx >= 0 before setting SelectedItem). Ensure channel exists or handle invalid input elsewhere.

## Dependencies
- _messageManager
- _chatFrame
- _channelNames
- _channelList
- RefreshChannelList
- RefreshMessages
- UpdateTopicBar
- _statusLabel

## Symbol To Document
- Name: SwitchToChannel
- Kind: method
- File: src/EchoHub.Client/UI/MainWindow.cs
- Language: csharp
- ID: a0176655-e010-4695-b78e-bc922f79b378

---

## ToggleUsersPanel
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void ToggleUsersPanel()
```

**Returns:** `void`


ToggleUsersPanel inverts the internal visibility flag for the online users panel and triggers a layout refresh. It provides a single, reusable way to show or hide the panel in response to user input, abstracting away direct state changes and layout calls from call sites. Typically invoked by user actions (for example, pressing F2) to quickly toggle the panel visibility.

## Remarks
By centralizing the show/hide behavior, this method preserves a consistent UI state and ensures UpdateLayout is always invoked after a change. It serves as a small, focused piece of UI orchestration within the MainWindow, making it easier to extend with cross-cutting concerns (logging, analytics, or optional animations) without duplicating state management across event handlers.

## Notes
- Callers should be executed on the UI thread; updating UI state from a non-UI thread can cause cross-thread exceptions.
- This method toggles visibility and delegates layout refresh to UpdateLayout; if you need richer behavior (e.g., animations), consider extending this with additional UI updates or a separate helper.

---

## TryAutocompleteCommand
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void TryAutocompleteCommand()
```

**Returns:** `void`


Tab-complete slash commands typed into the input field. It activates when the current text starts with '/' and contains no spaces, completing to the single matching command followed by a space, or to the longest common prefix of all matches when multiple commands share a prefix; the caret is moved to the end afterward.

## Remarks
Encapsulates the slash-command completion logic in the UI layer to provide a consistent user experience across all commands. When multiple matches exist, it uses the longest common prefix to minimize keystrokes rather than completing to a single command prematurely. The method is private and relies on the input field state and the SlashCommands collection, so callers should trigger it from input events (for example, an autocomplete key) rather than invoking it arbitrarily.

## Notes
- Runs only when the input begins with '/' and contains no spaces; otherwise it returns early.
- If there are multiple matches and the computed longest common prefix extends beyond the current text, the input is updated to that prefix; if not, no visible change occurs.
- After processing, the input caret is moved to the end of the text by setting the InsertionPoint.

---

## UpdateLayout
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void UpdateLayout()
```

**Returns:** `void`


Adjusts widths of the chat, topic, and input frames based on the visibility of the users panel. It computes a right margin depending on whether the panel is shown and applies that margin via Dim.Fill to the three main controls, ensuring the UI reflows to accommodate or release space as needed. It also toggles the users panel frame visibility to reflect the current state and requests a redraw to apply the changes.

## Remarks
This method centralizes the horizontal layout logic for the chat UI, guaranteeing consistent alignment when the users panel is shown or hidden. By encapsulating the width calculation and visibility toggle in one place, it reduces duplication and drift across the UI when the panel state changes. Callers should mutate _usersPanelVisible (and, if needed, UsersPanelWidth) and then invoke UpdateLayout to reflow the interface.

## Notes
- Assumes UsersPanelWidth is non-negative; negative values could yield unexpected widths.
- Should be invoked on the UI thread after changing _usersPanelVisible to avoid races or partial updates.

---

## UpdateOnlineUsers
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


Updates the online users list display by transforming each UserPresenceDto into a display item that combines a status glyph, an optional role marker, and the colored display name, then applies the resulting collection to the UI list and updates the frame title to reflect the current user count. It selects DisplayName when available, or falls back to Username, and uses a color parsed from NicknameColor to tint the displayed name. This method is typically invoked when a fresh snapshot of user presence arrives from the server to refresh the on-screen roster.

## Remarks
This method centralizes the presentation logic that maps domain data (presence, roles, and color preferences) to the UI representation of each user. By consolidating the status icons, role tags, and color-coding in one place, it becomes easier to adjust the visual language without touching rendering code elsewhere. It also ensures the user list title accurately mirrors the number of connected users.

## Notes
- Status icons rely on Unicode glyphs; ensure the UI font supports them, otherwise fallback characters may appear.
- DisplayName is optional; Username is used as a fallback label when no display name is provided.
- NicknameColor is parsed to tint the name; if parsing fails, HexColorHelper's behavior determines the final color (see ParseHexColor/ParseHexToColor usage).

---

## UpdateStatusBar
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


Updates the connection status displayed in the status bar by storing the provided status value in internal state and requesting the status label to redraw. This keeps the UI in sync with the underlying connection state without requiring callers to manage the redraw themselves.

## Remarks

Serves as a small, focused UI helper that centralizes how the status bar is refreshed when the connection state changes. By updating the internal _connectionStatus field and delegating the actual rendering to the status label's redraw mechanism (_statusLabel.SetNeedsDraw()), this method keeps presentation concerns separated from higher-level connection logic. Note that this method should be invoked on the UI thread to avoid potential race conditions when touching UI components.

## Notes

- Calling from a non-UI thread may cause race conditions since it mutates UI state and requests a redraw.
- Ensure _statusLabel is non-null before calling SetNeedsDraw to avoid NullReferenceException.
- If the status string requires formatting for display, consider centralizing formatting elsewhere rather than duplicating logic here.

---

## UpdateTopicBar
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void UpdateTopicBar()
```

**Returns:** `void`


UpdateTopicBar synchronizes the visibility and layout of the channel topic area with the current channel's topic. It looks up the topic by the current channel via _messageManager.CurrentChannel in the _channelTopics dictionary, and if a non-empty topic is found, it sets the topic label to ' Topic: {topic}', makes the label visible, and moves the chat frame down by setting Y to 2. If there is no topic, it hides the label and restores the chat frame to Y = 1. Callers rely on this method to refresh the topic display after channel changes or topic updates, instead of manipulating UI controls directly.

## Remarks
This method acts as a small presentation adapter between the data (channel-to-topic mapping) and the concrete UI. It encapsulates the conditional layout changes needed to show or hide the topic bar, keeping channel/topic state changes from leaking into scattered UI updates. By centralizing the logic, it reduces duplication and ensures consistent behavior whenever the topic display needs to reflect the current channel.

## Notes
- Ensure this is invoked on the UI thread when modifying UI controls to avoid cross-thread access errors.
- If _channelTopics may be updated concurrently, consider synchronization to prevent race conditions.
- The topic label text uses a leading space in the generated string; if localization or formatting changes are needed, adjust accordingly.

---

## AltQKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Key AltQKey = Key.Q.WithAlt
```


Encodes the Alt+Q keyboard shortcut as an immutable Key value by combining Q with the Alt modifier. This private static readonly field is used within the UI to recognize and react to the Alt+Q input without recomputing the modifier at runtime.

## Remarks
Because it is private and static readonly, AltQKey is initialized once for the type and then shared across all instances; it cannot be reassigned. The field relies on the Key type and the WithAlt extension to express the exact shortcut, centralizing the shortcut definition so input handling can reference a single source of truth. If you need a different shortcut, add another field or extract the behavior to a dedicated keyboard-shortcut manager rather than duplicating literals.

## Notes
- Be mindful of OS- or framework-level shortcuts that might conflict with Alt+Q; choose alternatives if necessary.
- Because it's private, this constant is not accessible outside its declaring type; expose a public alias only if you need external consumers to reuse the same combination.
- Ensure the WithAlt extension is available in the target framework version; otherwise the symbol won't compile.

---

## AppVersion
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
internal static readonly string AppVersion =
        typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "?"
```


AppVersion is a static, internal string field that exposes a compact representation of the application's version by reading the version of the assembly that defines MainWindow. It computes the version with three components (major.minor.build) and falls back to '?' if no version is available; use it when you need a lightweight, display-friendly version string in the UI or logs instead of querying the assembly repeatedly.

## Remarks
This abstraction centralizes how the UI's version is presented and ties it to the MainWindow assembly, ensuring consistency wherever the version string is shown. Because it is initialized once and stored as readonly, the value remains stable for the lifetime of the process, and ToString(3) intentionally truncates any additional version components beyond three.

## Notes
- Computed once during type initialization; it will not reflect a runtime version change within a single app run.
- If the assembly version is not set, the code gracefully falls back to a single '?' instead of throwing.
- Being internal, this field is only accessible within the containing assembly; to expose a public version string, a dedicated public API would be required.

---

## CtrlKKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Key CtrlKKey = Key.K.WithCtrl
```


Represents the Ctrl+K keyboard shortcut as a Key value. It provides a reusable, centralized representation of this combination for input handling in the UI, so code can react to Ctrl+K without duplicating modifier checks in multiple places.

## Remarks
Centralizing the shortcut as a single field ensures consistency across all code paths that respond to Ctrl+K and makes future changes to the shortcut trivial (update in one place). Being private keeps this detail encapsulated within the class, exposing no surface area to external components. It fits alongside other keyboard bindings and command wiring in the MainWindow's input handling strategy.

## Notes
- It is private, so not accessible from outside the defining type. If reuse outside the class is required, provide a public alias or binding elsewhere.
- It is static and readonly, ensuring the value is initialized once and remains constant for the lifetime of the process.
- The value relies on Key.WithCtrl, which ties the ctrl modifier to Key.K; if the Key type or modifier semantics change, the field should be updated accordingly.

---

## EnterKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Key EnterKey = Key.Enter
```


EnterKey caches the Key.Enter value in a private, static readonly field to avoid repeating Key equality checks during Enter-key handling in the UI. By comparing against EnterKey.KeyCode instead of Key.Equals (which also considers the Handled state), the code achieves faster and more predictable detection of the Enter key.

## Remarks
EnterKey acts as a small central anchor for Enter-key detection within MainWindow. The static readonly field ensures a single, shared value is used across the class, reducing boilerplate and potential inconsistencies in key comparisons. Favor using KeyCode-based checks over Key.Equals to avoid the Handled state influencing the result.

## Notes
- Private scope limits reuse to the containing class; for cross-class sharing, expose a public helper or property.
- Static initialization is thread-safe and occurs once before first use, guaranteeing a single cached value.
- If the Key type changes its internal comparison semantics (e.g., KeyCode API changes), update or remove EnterKey accordingly to preserve behavior.

---

## F2Key
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Key F2Key = Key.F2
```


F2Key is a private static readonly field that stores the F2 value from the Key enum. It serves as a named, immutable alias for the Key.F2 used in the UI keyboard handling within MainWindow, enabling comparisons against key input without scattering the literal Key.F2 across the codebase. The combination of private visibility, static initialization, and readonly immutability ensures a single, stable reference that improves readability and maintainability.

## Remarks
This abstraction communicates the intent that the F2 key is a canonical shortcut used by this class, and it centralizes the key binding so changes to the shortcut can be made in one place rather than updating multiple comparisons to Key.F2. Because the field is private, external code cannot rely on or reuse it; if cross-class reuse is required, consider widening visibility (e.g., internal or public) or extracting the key binding into a shared constants module.

## Notes
- Private visibility limits reuse to the declaring type; exposing the key would require changing its accessibility.
- If you anticipate reusing this binding across multiple classes, extract it into a shared location or make it internal/public with clear naming.
- The readonly modifier guarantees the value cannot be reassigned after initialization, preserving a stable binding to Key.F2.


---

## NewlineKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Key NewlineKey = Key.N.WithCtrl
```


Defines a keyboard shortcut for Ctrl+N as a Key value and caches it in a private static readonly field named NewlineKey. This centralizes the recognition of Ctrl+N in the UI code so handlers can test e.Key against NewlineKey instead of reconstructing the gesture each time.

## Remarks
Because the field is static readonly, its value is initialized once and cannot be changed at runtime, which guarantees consistent behavior across the UI and reduces the risk of subtle drift in shortcuts. Making it private ensures it is encapsulated within its containing type; if you need to expose the shortcut externally, provide a public accessor rather than duplicating the value. This symbol typically interacts with input handling logic that listens for KeyDown/PreviewKeyDown events and triggers the associated action when the pressed key matches NewlineKey. It relies on the WithCtrl extension applied to Key.N to compose the Ctrl+N gesture, signaling a consistent, framework-agnostic representation of that shortcut.

## Notes
- The field is private; outside code cannot reference NewlineKey directly. If sharing the shortcut is required, add a controlled public/internal accessor.
- The value is tied to the specific Key and modifier composition (Ctrl+N). If the project targets a different input framework, the exact representation may differ even though the intent remains the same.

---

## SlashCommands
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly string[] SlashCommands =
    [
        "/status", "/nick", "/color", "/theme", "/send",
        "/avatar", "/profile", "/servers", "/join", "/leave",
        "/topic", "/users", "/kick", "/ban", "/unban",
        "/mute", "/unmute", "/role", "/nuke", "/test-sound", "/quit", "/help"
    ]
```


It defines the fixed set of slash commands used to power the chat tab-autocomplete in EchoHub.Client's main window. A developer would reference this list when implementing or auditing the autocomplete behavior, rather than hard-coding command strings at multiple call sites.

## Remarks

This array represents the canonical set of slash commands consumed by the UI's autocomplete layer. Because the field is private, static, and readonly, the list is a single source of truth that shared across all instances and cannot be mutated at runtime. Centralizing the commands here helps ensure consistent autocomplete behavior and simplifies updates when supported commands change. If more flexibility is needed in the future (for example, per-user command variations or dynamic loading), consider exposing a controlled API rather than duplicating literals across the codebase.

## Notes

- The field is private and readonly, so external code cannot modify it at runtime.
- The ordering defines the display sequence; if you require a different presentation, you may sort at the point of use.
- Updating this list requires recompilation; there is no runtime plugin mechanism to inject new commands.

---

## StatusBrandAttr
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Attribute StatusBrandAttr = new(new Color(218, 165, 32), Color.None)
```


This private static readonly field represents the status branding attribute used by the UI. It constructs an Attribute with a gold color (RGB 218,165,32) and Color.None as the secondary color, establishing a consistent visual cue for status indicators within MainWindow without duplicating color values.

## Remarks
This field centralizes the branding decision for status visuals in MainWindow. Its private, static, and readonly nature ensures a single, immutable instance that is shared within the class, preventing external consumers from depending on internal branding details. If branding changes are needed, updating this single initializer will propagate to all usages within the class. If you need to reuse the concept elsewhere, expose a public accessor or move branding into a shared resource instead of duplicating values.

## Notes
- Hard-coded color values mean this attribute won't automatically adapt to themes or accessibility adjustments; consider theming or resources if you plan to support dark/light modes.
- The field is private, so external code cannot reference it directly; provide a public API if broader reuse is required.


---

## StatusConnectedAttr
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Attribute StatusConnectedAttr = new(new Color(0, 200, 0), Color.None)
```


This private static readonly field defines a reusable styling token used to indicate a connected status in the UI. It creates a green color attribute (RGB 0,200,0) with no secondary color, enabling the MainWindow class to apply a consistent, prebuilt visual cue for 'connected' elements without creating new Attribute instances at each use.

## Remarks
Centralizes the visual language for the 'connected' state, ensuring consistent coloring across the window's controls. The field is private to the MainWindow class, so external code cannot reuse it directly, which favors encapsulation of styling concerns. Because the field is static, the Attribute instance is created once at type initialization and shared by all usages within the class.

## Notes
- The readonly modifier prevents reassignment of the field reference, but the underlying Attribute instance may still be mutable depending on the Attribute API. Treat this as a shared token; changes to the object's state could affect all consumers.

---

## StatusDisconnectedAttr
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Attribute StatusDisconnectedAttr = new(new Color(220, 50, 50), Color.None)
```


StatusDisconnectedAttr is a private static readonly field of type Attribute used by the MainWindow UI to represent the visual styling for a disconnected state. It is initialized with a primary color of (220, 50, 50) and Color.None for the secondary color, providing a clear, red-themed indicator. This shared instance is intended to be reused wherever the disconnected state needs to be signaled, avoiding repeated construction and ensuring a consistent look.

## Remarks

Centralization and reuse: A single static instance ensures consistent appearance and simplifies future theming changes; hiding it as private ensures encapsulation within the window's logic.

Thread-safety and immutability: The static initialization is thread-safe, but if the underlying Attribute object is mutable, any downstream mutation would affect all consumers of this field. Prefer treating it as an immutable styling reference, or create copies when dynamic variation is required.

## Notes

- Color.None indicates the absence of a secondary color; if you need an accent, replace or clone to create a new Attribute.
- Because the field is private, external code cannot reference StatusDisconnectedAttr directly; to reuse this style, expose it via a controlled API or create similar static fields.

---

## StatusTransitionalAttr
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Attribute StatusTransitionalAttr = new(new Color(220, 180, 0), Color.None)
```


This private static readonly field defines a reusable Attribute instance used to convey a 'transitional' status in the UI. It is constructed with a Color(220, 180, 0) and Color.None as the secondary color, and is kept private and static so the same instance is reused across the MainWindow class. Developers reach for this field when they need a consistent transitional cue without creating new Attribute objects for every element.

## Remarks
Centralizing the transitional appearance helps maintain a consistent visual language across the main window and prevents ad-hoc color choices. If you need additional variants, prefer expanding this palette rather than duplicating Attribute creation at call sites.

## Notes
- Readonly prevents reassignment of the field reference, but the underlying Attribute object may still be mutated if its properties are publicly writable, so treat it as a configuration object rather than a pure value.

---

## TabKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Key TabKey = Key.Tab
```


TabKey is a private static readonly field that captures the Tab key value from the Key enum by aliasing Key.Tab. It serves as a centralized, read-only reference for keyboard navigation logic inside the MainWindow’s UI code, so the codebase can refer to a single symbol rather than repeatedly writing Key.Tab. The private scope keeps it encapsulated within the class, signalling that this alias is an implementation detail of its keyboard handling behavior.

## Remarks
TabKey abstracts the specific Key.Tab value behind a named symbol, clarifying intent wherever keyboard handling compares against the tab key. It also provides a single place to adjust the semantics of tab-related behavior in the future, even though the field itself is readonly. Because it’s private, the alias cannot be used outside the class, encouraging tests and consumers to interact with the class through its public behavior rather than internal details.

## Example
```csharp
if (e.Key == TabKey)
{
    // Custom tab handling
}
```

## Notes
- This member is private; external code cannot reference it. If you need cross-class usage, consider exposing a public constant or property.
- It is static and readonly; the value is initialized once and cannot be changed at runtime.

---

## UsersPanelWidth
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const int UsersPanelWidth = 22
```


The private constant defines the fixed width of the Users panel in the main window. By using a dedicated constant instead of a magic number, the UI sizing remains explicit, making it easy to adjust the panel width in a single location without scattering literals throughout the layout code. This improves readability and helps maintain a consistent appearance for the Users panel across the window.

## Remarks
This constant centralizes a layout decision and communicates intent in the UI construction code. Keeping it private preserves encapsulation—the sizing of the Users panel is an internal detail of the main window logic, not part of the public API. If future requirements necessitate wider sharing of this dimension, consider exposing a controlled API (while preserving testability) rather than sprinkling literals across the UI setup.

## Notes
- As a compile-time const, the value is inlined at call sites; changing it requires recompiling the referencing code.
- Being private means external code cannot rely on or override this value.

---