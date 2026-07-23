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
  - [ClickChannelRegex](#clickchannelregex)
  - [ClickMentionRegex](#clickmentionregex)
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
  - [AppVersion](#appversion)
  - [CtrlCKey](#ctrlckey)
  - [CtrlKKey](#ctrlkkey)
  - [CtrlVKey](#ctrlvkey)
  - [CtrlXKey](#ctrlxkey)
  - [CtrlYKey](#ctrlykey)
  - [DefaultInputTitle](#defaultinputtitle)
  - [EnterKey](#enterkey)
  - [F2Key](#f2key)
  - [F6Key](#f6key)
  - [NewlineKey](#newlinekey)
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

---

## MainWindow
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** class

```csharp
public sealed partial class MainWindow : Runnable
```


Main Terminal.Gui-backed chat window that composes and coordinates the channel list, message view, input field, users panel, and menus for the EchoHub client. Reach for `MainWindow` when you want a complete, ready-to-run terminal UI for chat (keyboard handling, paste/drag staging, autocomplete and connection UI) rather than composing low-level `Terminal.Gui` controls yourself.

## Remarks
`MainWindow` is the single UI surface that translates user interactions into domain-level events and coordinates the internal UI state. It owns and wires together UI components such as `ListView` (for `*_channelList` and `*_messageList`), `TextView` (`_inputField`), `FrameView` (`_chatFrame`, `_inputFrame`, `_usersFrame`), `Label` (`_statusLabel`, `_topicLabel`), and a `MenuBar` (`_menuBar`). The class exposes application-facing events — `OnChannelSelected`, `OnMessageSubmitted`, `OnFilesStaged`, `OnImagePasted`, and `OnConnectRequested` — so the surrounding orchestration (for example the [`AppOrchestrator`](../AppOrchestrator.cs.md)/`Make`) can react without needing to know UI internals. `MainWindow` also holds UI-centric state such as `AppVersion`, the `SlashCommands` used for Tab completion, per-channel metadata (`_channelNames`, `_channelTopics`, `_channelPublic`, `_channelProtected`, `_systemChannels`), and message handling via `ChatMessageManager`.

Keyboard handling is intentionally implemented using raw `KeyCode` constants (for example `EnterKey`, `NewlineKey`, `TabKey`, `CtrlKKey`, `F6Key`) so comparisons avoid `Key.Equals` semantics that include the `Handled` flag; this makes the key-switching logic deterministic and suitable for switch statements. The users panel visibility is controlled by `_usersPanelVisible` and sized using the `UsersPanelWidth` constant; toggling it affects layout and available chat width (`_lastChatWidth`).

## Notes
- UI thread: `MainWindow` is a `Terminal.Gui`-based UI — subscribers to `OnMessageSubmitted`, `OnFilesStaged`, `OnImagePasted`, and other events are invoked from the UI context. Handlers must avoid long/blocking work and should dispatch to background threads or task queues for I/O, network, or CPU-heavy operations.
- Key comparison detail: key bindings are implemented as `KeyCode` constants and compared by raw value (e.g. `CtrlKKey = KeyCode.K | KeyCode.CtrlMask`). If you add custom key handling, compare against the same raw `KeyCode` values rather than relying on `Key.Equals` or higher-level key abstractions.
- Staged attachments and file paths: `OnFilesStaged` provides absolute filesystem paths for files already accepted by the UI (the code expects existing files). Consumers should still validate and handle missing/removed files; the `_hasStagedAttachments` flag indicates staged state within the `MainWindow` and must be cleared by whatever logic performs the actual upload/send.

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


`MainWindow` wires the core UI dependencies (`IApplication` and `ChatMessageManager`), subscribes to message events (`MessagesChanged` and `HistoryPrepended`), and builds the primary terminal UI for the EchoHub client. It lays out a top menu bar, a left channels panel, a center chat area with a messages list and input field, and a right online users panel, wiring up the corresponding list sources and event handlers to reflect data changes and user interactions.

## Remarks
`MainWindow` serves as the composition root for the client UI, coordinating three specialized list sources ([`ChannelListSource`](ListSources/ChannelListSource.cs.md), [`ChatListSource`](Chat/ChatListSource.cs.md), [`UserListSource`](ListSources/UserListSource.cs.md)) with their corresponding `ListView`s and centralizing interaction wiring (selection, input, rendering). It also incorporates a targeted keyboard-binding workaround to ensure a stable editing experience in the terminal UI by remapping `Key.W.WithCtrl` to `Command.KillWordLeft` to avoid clipboard-related crashes.

## Notes
- The input field rebinds Ctrl+W to delete-word-left to prevent clipboard exceptions when the OS clipboard is involved; if you customize input controls or port to a different UI framework, review keyboard bindings to avoid unintended clipboard interactions.

---

### CurrentChannel
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** property

```csharp
public string CurrentChannel => _messageManager.CurrentChannel
```


The `CurrentChannel` property exposes the name of the actively used message channel by delegating to the internal `_messageManager`. It serves as a lightweight, UI-friendly accessor that decouples UI code from the underlying manager while providing a stable source of truth for the current channel.

## Remarks
The property acts as a forwarder to `_messageManager.CurrentChannel`, encapsulating the channel retrieval so callers don't need to reference the manager directly. It defines a read-only snapshot of the channel that the UI can display or react to, without incurring additional logic in this wrapper. This separation helps preserve a clean boundary between the UI layer and the message subsystem.

---

### HasPendingReplyIndicator
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** property

```csharp
public bool HasPendingReplyIndicator => _replyTitleFragment is not null
```


`HasPendingReplyIndicator` reports whether a pending-reply indicator should be shown in the `MainWindow` UI. It returns true when `_replyTitleFragment` is not null, indicating there is a pending reply ready to be presented.

## Remarks
By wrapping the internal `_replyTitleFragment` state, this property exposes a stable UI contract for the `MainWindow` without leaking implementation details. It turns on the indicator whenever `_replyTitleFragment` is non-null, and off otherwise, making the visibility rule easy to reason about from the UI layer. This centralizes the visibility logic in one place, so changes to how a pending reply is represented don't ripple through callers.

---

### IsCurrentChannelReadOnly
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** property

```csharp
private bool IsCurrentChannelReadOnly => _systemChannels.Contains(_messageManager.CurrentChannel)
```


IsCurrentChannelReadOnly indicates whether the currently active channel is a system (read-only) channel. It returns true when the current channel is contained in the `_systemChannels` collection; otherwise false. This centralized property allows UI logic to decide, for example, whether message input should be enabled when the user is in a read-only channel, without scattering the `_systemChannels.Contains(_messageManager.CurrentChannel)` check throughout the code.

## Remarks
By encapsulating the read-only rule in a single property, the codebase gains a single source of truth for what constitutes a system channel. If `_systemChannels` is updated or the current channel reference changes, this property automatically reflects the new state, keeping UI behavior consistent. It also improves readability and testability by naming the intent instead of embedding the containment check in multiple places.

## Notes
- The property is private; external consumers cannot rely on it directly. Tests should exercise the observable behavior (e.g., enabling/disabling input) rather than this accessor.

---

### IsTransitionalStatus
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** property

```csharp
private bool IsTransitionalStatus => _connectionStatus is not ("Connected" or "Disconnected")
```


Determines whether the current `_connectionStatus` represents a transitional, non-final state. Use this property when you need to react to an ongoing change in the connection (for example, showing a loading indicator or deferring user actions) rather than simply checking for `Connected` or `Disconnected`.

## Remarks
This property encodes the idea that a connection is in flux rather than settled into a terminal state. By centralizing the check, it prevents scattered string comparisons across the class and makes it easier to adapt if the exact terminal labels change. It works in concert with UI state management to drive indicators or input gating during transitions.

## Notes
- It hinges on the exact string literals `Connected` and `Disconnected`; changes to these labels or localization would require updating this property.
- Being private, external code cannot rely on this property; expose a dedicated API if external components must react to transitional states.

---

### ApplyColorSchemes
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void ApplyColorSchemes()
```

**Returns:** `void`


Applies the currently registered color schemes to all views and should be invoked after a theme change to refresh UI colors. It queries `SchemeManager` for `Base`, `Menu`, and `Border` schemes; the `Base` scheme is applied to the root view, then propagated to most subviews. If a `Border` scheme is available (falling back to `Base` when absent), it is applied to frame borders via `FrameView.Border` to allow borders to be tinted independently of text. After processing the base scheme, any available `Menu` scheme is applied to `_menuBar`, `_statusLabel`, and `_topicLabel` to ensure menu-related visuals reflect the active theme.

## Remarks
By centralizing theming logic here, the UI consistently reflects theme changes without each subview duplicating scheme-updating code. It also accommodates borders that should be tinted differently by applying the `Border` scheme to `FrameView.Border` when present, allowing border tones to diverge from text colors. The propagation intentionally excludes `_menuBar`, `_statusLabel`, and `_topicLabel` from the base propagation so they can be driven by the `Menu` scheme for coherent menu visuals across the interface.

## Notes
- If `Base` is null, no base propagation occurs; base-based updates are guarded by a null check.
- The `Border` scheme is optional; `borderScheme` is applied to borders only when it is not null.
- During propagation, frames are updated via `FrameView.Border?.SetScheme(borderScheme)` only when a suitable `borderScheme` exists.

---

### BuildMenuBar
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private MenuBar BuildMenuBar()
```

**Returns:** `MenuBar`


Builds the application's top-level `MenuBar` by composing three root menus — `File`, `Server`, and [`User`](../../EchoHub.Core/Models/User.cs.md) — and appending a dynamic theme submenu sourced from `Themes.ThemeManager.GetAvailableThemes()`. It also conditionally inserts a rollback entry when a backup exists via `UpdateBackupService.BackupExists()` and wires up all actions to their corresponding callbacks (e.g. `OnProfileRequested`, `OnThemeSelected`, `OnConnectRequested`, `OnDisconnectRequested`, `OnLogoutRequested`, `OnCreateChannelRequested`, `OnDeleteChannelRequested`, `OnSavedServersRequested`, `ToggleUsersPanel`, `OnCheckForUpdatesRequested`). Finally, it lays out the menu bar at `(0,0)`, stretches it to fill width, and applies a mouse-through workaround by setting `ViewportSettings` to `TransparentMouse` on each `MenuBarItem`'s `CommandView` to ensure clicks hit the intended item.

---

### ClearAll
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void ClearAll()
```

**Returns:** `void`


Resets the chat state and UI to a clean slate when disconnecting. It clears all channel-related data (`_channelNames`, `_channelTopics`, `_channelPublic`, `_channelProtected`, `_systemChannels`) and the messages via `_messageManager.ClearAll()`, resets the channel and user lists (`_channelListSource`, `_channelList`, `_usersListSource`, `_usersList`), and restores the UI to its initial layout (title set to `Chat`, topic label hidden, frame Y reset, and user frame titled `Users`). Finally, it calls `RefreshMessages()` to purge any displayed content.

## Remarks
Centralizes cleanup logic for the chat UI, reducing the risk of inconsistent state when disconnecting. By coordinating both in-memory data and their visual bindings, it guarantees the UI starts from a known baseline on the next connection.

## Notes
- This method should only be invoked during disconnect; calling it during an active session will wipe the current UI state.
- Assumes UI thread context since it manipulates UI elements like `_chatFrame`, `_topicLabel`, and `_usersFrame`.

---

### ClickChannelRegex
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


ClickChannelRegex is a compile-time-generated factory that returns a `Regex` instance capable of matching channel mentions that begin with a '#' and are not part of a larger word. The `GeneratedRegex` attribute on this method enables the compiler's source generator to emit a precompiled regex for the given pattern, so callers get a ready-to-use `Regex` with minimal allocation and startup cost. Use this symbol when your text parsing layer needs to identify channel-like tokens, such as `#general`, while avoiding hex color codes like `#FFFFFF` or other purely numeric identifiers.

## Remarks
By encapsulating the logic in a private static partial method annotated with `GeneratedRegex`, the class centralizes the channel-detection pattern and benefits from compile-time optimization. The negative lookbehind `(?<!\w)` prevents matches that are part of a larger word, and the lookahead `(?=.*[a-zA-Z])` ensures at least one alphabetic character is present, avoiding numeric-only tokens. The generated `Regex` is intended for local use within the UI's text parsing flow to recognize and potentially hyperlink or trigger interactions for channel tokens.

## Example
```csharp
var regex = ClickChannelRegex();
var m = regex.Match("#general");
if (m.Success)
{
    Console.WriteLine($"Found channel: {m.Value}");
}
```

## Notes
- The `GeneratedRegex` attribute requires source-generation support in the project (e.g., .NET 7+); ensure the build enables the generator for this symbol.
- The `ClickChannelRegex` method is private; expose a public wrapper if external call sites must reuse it.
- The pattern intentionally requires at least one letter after `#` to avoid matching hex colors (e.g., `#FFFFFF`) or purely numeric tokens.

---

### ClickMentionRegex
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


ClickMentionRegex returns a pre-generated `Regex` configured to detect user mentions in text by matching an '@' followed by a username, but only when the '@' isn't preceded by a word character (to avoid matching emails such as `name@example.com`). The underlying pattern is `(?<!\w)@([\w-]+)`.

## Remarks
Because the method is annotated with `GeneratedRegex` and declared as `static partial`, the compiler generates a cached, strongly-typed `Regex` instance at build time, enabling fast matches at runtime without repeated allocations. This approach avoids constructing a new `Regex` on every call and keeps the parsing logic encapsulated within the containing class. The negative lookbehind `(?<!\w)` ensures emails like `name@example.com` aren't treated as mentions, preserving the intended behavior. If you need to reuse this regex outside the class, expose a public wrapper or move the logic to a shared helper.

## Notes
- The method is private; external code cannot call it directly. If reuse is needed outside the containing type, provide a public wrapper around the regex or relocate the logic to a shared utility.
- The pattern is fixed at compile time via the `GeneratedRegex` attribute; changing it requires recompilation.

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


Prompts the user to confirm deletion of a message and, if confirmed, raises the `OnDeleteMessageRequested` event with the provided `messageId`. It uses `MessageBox.Query` to display a modal dialog titled `Delete Message` with the content `Delete this message?` and two actions, `Delete` and `Cancel`. If the user presses the first button (the `Delete` action, which yields a return value of `0`), it invokes `OnDeleteMessageRequested` with `messageId`.

## Remarks
This method centralizes the deletion-confirm UX for messages within the MainWindow UI, decoupling the prompt from the actual delete logic by publishing the `OnDeleteMessageRequested` event. The `OnDeleteMessageRequested` invocation is guarded with the null-conditional operator, so the UI remains safe even if there are no subscribers. The behavior assumes the first button corresponds to the delete action; if the button order or labels change, the conditional should be updated accordingly to preserve the intended UX.

## Notes
- The conditional relies on the first button (value `0`) representing the `Delete` action; changes to the dialog wiring require updating this check. 
- This method should run on the UI thread since it shows a modal dialog via `MessageBox.Query`.
- Being `private`, this helper is intended strictly for internal UI orchestration; if reuse is needed elsewhere, consider extracting a shared confirmation helper or exposing a higher-level API.

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


Copies the provided `string` text to the system clipboard through the application's clipboard service by calling `_app.Clipboard?.TrySetClipboardData(text)`. If the clipboard is unavailable or an exception occurs, the method swallows the error and logs a warning with the message `Copy to clipboard failed` using `Log.Warning`.

## Remarks
This small helper centralizes clipboard access behind `_app.Clipboard` to keep clipboard interactions consistent and resilient to clipboard provider unavailability. It decouples clipboard operations from UI logic and ensures the application remains responsive by not surfacing clipboard errors to the user—only a warning is recorded for diagnostics.

## Notes
- Clipboard operations may fail silently if the clipboard provider is unavailable or restricted; users won't see an immediate notification, only a logged warning.

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


EnsureChannelInList updates the left-panel channel list by creating or mutating a channel entry and applying optional metadata flags. When called, it updates internal collections such as `_channelPublic`, `_channelProtected`, and `_systemChannels`, and adds the channel name to `_channelNames` if it is new; it then refreshes the UI via `RefreshChannelList` to reflect the current state.

## Remarks
By encapsulating the mutation logic, `EnsureChannelInList` reduces duplication and keeps the left-panel state consistent across different update paths. It coordinates between multiple internal collections and the UI refresh, ensuring that changes to a channel's visibility or role are reflected in the UI with a single call.

## Example
```csharp
// Ensure a public channel exists
this.EnsureChannelInList("general", isPublic: true);

// Mark a channel as protected
this.EnsureChannelInList("ops", isProtected: true);

// Ensure a system channel flag
this.EnsureChannelInList("system-notice", isSystem: true);
```

## Notes
- Not thread-safe; should be invoked on the UI thread.
- Nullable parameters mean "leave unchanged" when null; pass the appropriate booleans to modify behavior.
- Adding a new channel triggers a UI refresh; calling it repeatedly for many channels may cause multiple refreshes.

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


Regenerates a separator rule (date change / unread marker) to span the current viewport width by constructing a new [`ChatLine`](Chat/ChatLine.cs.md) with a single [`ChatSegment`](Chat/ChatSegment.cs.md) that renders the left border, the label, and a trailing run of `─` characters to fill the space. It uses the attribute from `line.RuleAttr` if present, otherwise `ChatColors.DateRuleAttr`; it takes the label from `line.RuleLabel` and computes the tail length as `Math.Max(width - 4 - label.GetColumns() - 1, 2)`, finally propagating the `IsUnreadMarker` flag from the original line.

## Remarks
Expanding the rule centrally ensures a consistent visual separator as the viewport changes size and binds its appearance to the established color attributes, rather than scattering sizing logic across call sites. It abstracts the drawing of date-change and unread markers behind a single path, so collaborators render a correctly sized rule without reproducing the dash calculations.

## Notes
- The code uses the null-forgiving operator on `line.RuleLabel`, so a null value will throw a `NullReferenceException` at runtime.

---

### FocusInput
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void FocusInput()
```

**Returns:** `void`


FocusInput focuses the input field for typing by delegating to the underlying `_inputField` control's focus mechanism. Call this when you want to place the caret in the input area so the user can start typing immediately, without manipulating the private UI element directly.

## Remarks
Because this method simply forwards to `_inputField.SetFocus()`, it provides a small abstraction boundary that isolates callers from the control's implementation details. This helps keep focus-related logic in one place and makes it easier to adjust the focus policy (e.g., when to trigger focus after navigation or modal dialogs) without changing call sites. It also aids testability by providing a deterministic point to assert that focus is requested.

## Notes
- Requires UI-thread access; ensure invocation occurs on the UI thread or marshal accordingly.

---

### FocusMessageList
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void FocusMessageList()
```

**Returns:** `void`


Moves focus into the `messageList` to enable keyboard navigation (arrows) and deletion (Delete). If there is no valid selection, it selects the most recent message; if the channel has no messages, the operation is a no-op. Internally, the method guards against non-chat sources or empty lists and then focuses the list and requests a redraw after ensuring a valid `SelectedItem`.

## Remarks
This helper centralizes the focus and selection logic for the message list to provide a consistent keyboard-driven UX. By selecting the last item when no valid selection exists, it aligns with the common expectation that the newest message is the natural target for keyboard actions. The explicit calls to `SetFocus()` and `SetNeedsDraw()` ensure the UI stays responsive and reflects changes promptly.

## Notes
- If the current selection is already valid, the method preserves it and only shifts focus to the list; it will still trigger a redraw.
- The behavior is contingent on `_messageList.Source` being a [`ChatListSource`](Chat/ChatListSource.cs.md) with items; if not, the method exits early as a no-op.


---

### GetChannelNames
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public IReadOnlyList<string> GetChannelNames() => _channelNames.AsReadOnly()
```

**Returns:** `IReadOnlyList<string>`


This method returns the set of channel names that have message buffers for broadcasting status changes. It provides a read-only view of the internal `_channelNames` collection, so callers can enumerate available channels without mutating the underlying data.

## Remarks
This method serves as a safe, read-only projection of the internal channel registry used for status broadcasts. By returning `IReadOnlyList<string>` via `_channelNames.AsReadOnly()`, it preserves encapsulation while letting callers enumerate available channels. Because it is a live view of the underlying collection, any subsequent changes to `_channelNames` will be reflected in the returned sequence; if a stable snapshot is required, callers should materialize a copy at the point of use.

## Notes
- The returned `IReadOnlyList<string>` is a live view into `_channelNames`; it does not copy elements, so mutations to the underlying list will be visible to callers.


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


The `GuardedClipboardAction` method executes the provided `Action` to drive a clipboard-backed edit while swallowing transient clipboard failures that would otherwise bubble up from the input loop and crash the app. If the action throws an exception, it is caught and a warning is logged via `Log.Warning` with the operation name, after which execution continues.

## Remarks

This function acts as a resilience boundary around clipboard edits, ensuring that transient OS clipboard contention does not destabilize the UI input loop. By centralizing this behavior, all clipboard-backed actions share consistent error handling and telemetry through the `Log` dependency.

## Notes

- Broadly catching `Exception` may hide non-transient failures; if you rely on exceptions for debugging, consider narrowing the catch or rethrowing critical exceptions.

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


In the chat UI, `MentionUser` inserts a properly formatted user mention into the input field and then returns focus to the input control. It accomplishes this by inserting the text starting with an at-sign followed by the provided `username` and a trailing space via `_inputField.InsertText`, and then calling `_inputField.SetFocus` to restore typing context.

## Remarks
This method encapsulates a small piece of UI behavior: it formats a username into a chat mention and ensures the input remains focused after insertion. Centralizing this logic avoids duplication at call sites and makes it easy to adjust how mentions are presented or how focus is managed in the future.

## Example
```csharp
MentionUser("Alice");
```

## Notes
- Assumes `_inputField` is non-null and that these calls occur on the UI thread; otherwise, this method may throw or fail to update focus.

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


Handles changes to the channel list selection in the UI. When the user selects a different channel, this method validates the new index, resolves the corresponding channel name from `_channelNames`, and, if it differs from the current channel exposed by `_messageManager.CurrentChannel`, switches to that channel by calling `SwitchToChannel` and notifies subscribers via `OnChannelSelected`.

## Remarks
By centralizing the UI-to-channel-switch logic, this symbol serves as the single decision point for user-driven channel changes, keeping UI concerns separate from channel management. It avoids unnecessary work by only performing a switch when the new channel is different from the current one and by emitting `OnChannelSelected` to any interested listeners. This method relies on the integrity of `_channelNames` and `_messageManager.CurrentChannel` and assumes the UI selection reflects the latest channel list.

## Notes
- This method assumes `_channelNames` is non-null and synchronized with the UI list. If `_channelNames` can be null or updated concurrently, this handler may throw or behave inconsistently; ensure proper initialization and synchronization.

---

### OnChatViewportChanged
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void OnChatViewportChanged()
```

**Returns:** `void`


When the chat viewport width changes, `OnChatViewportChanged` reads the current width from `_messageList.Viewport.Width`, and if the width is positive and differs from `_lastChatWidth`, it updates `_lastChatWidth`, applies the new width to `_messageManager` via [`SetChatWidth`](Chat/ChatMessageManager.cs.md), and refreshes the messages with `RefreshMessages()`.

## Remarks
This method encapsulates the UI's width-responsive behavior for the chat area. By guarding on positive, changed widths, it avoids unnecessary reflows and redraws, delegating the actual rendering adjustments to `_messageManager` and `RefreshMessages()`.

## Notes
- Guard against zero widths to prevent wasted work during startup or transient layout passes.
- Ensure `_lastChatWidth` is initialized appropriately so the first meaningful width change triggers an update.

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


OnHistoryPrepended is a UI helper invoked when older messages are prepended to the chat history for a channel. It only runs for the currently active channel, fetches that channel's messages, and if there are messages to process, it refreshes the message list and then reselects the item that now sits at the top due to the prepend. This preserves the user’s reading position, so they don’t scroll to the newest messages just because history was loaded.

## Remarks
This method acts as a focused UX shim around the history-prepend workflow. By capturing the count of items before and after `RefreshMessages()` and then assigning `_messageList.SelectedItem` to the delta, the view stays anchored to the same top-most message despite the updated list. The logic is deliberately scoped to the current channel and the [`ChatListSource`](Chat/ChatListSource.cs.md) used by the UI list; if those assumptions fail, the method exits or does not adjust the selection, preventing misalignment.

## Notes
- It only executes when `channelName` matches `_messageManager.CurrentChannel`; otherwise it returns immediately.
- It relies on `_messageList.Source` being a [`ChatListSource`](Chat/ChatListSource.cs.md) to compute counts; if not, the old count defaults to 0, which can affect the delta calculation.
- The selection repositioning uses the delta (`prependedCount`) as the new `SelectedItem`; changes to how the list interprets `SelectedItem` could alter the visual scroll behavior if the UI contract changes.


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


OnInputContentsChanged is a private event handler that runs when the main input field's contents change. It normalizes user input by (1) detecting a dropped file path that resolves to files and staging them via `StageFiles` instead of sending the raw path, and (2) performing emoji substitutions with `EmojiHelper.ReplaceEmoji`, updating the input and cursor position when needed; all updates are guarded by `_suppressEmojiReplace` to avoid recursive edits.

## Remarks
This method centralizes input normalization at the UI boundary: it both interprets dropped files and performs emoji normalization, so callers need not handle these concerns separately. It coordinates [`DroppedFileParser`](Helpers/DroppedFileParser.cs.md) and [`EmojiHelper`](Helpers/EmojiHelper.cs.md) to convert user input into the appropriate sent form while preserving the user's cursor position, and uses `_suppressEmojiReplace` to avoid re-entrant updates caused by programmatic text changes.

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


OnInputKeyDown processes key presses in the message-entry area and dispatches the appropriate actions for common chat shortcuts, such as autocompletion (`TabKey`), canceling a pending reply (`Esc` when `HasPendingReplyIndicator` is true), inserting a newline (`NewlineKey`), and submitting a message (`EnterKey`) when the channel is writable and there is text or staged attachments. It also handles app-level commands (`AltQKey`), opening the search dialog (`CtrlKKey`), and focus management (`F6Key`) to move focus to the message list. It additionally implements clipboard-based behavior for paste (`CtrlVKey`, `CtrlYKey`), cut (`CtrlXKey`), and copy (`CtrlCKey`), including attachment staging and image pasting via [`ClipboardFiles`](../Services/ClipboardFiles.cs.md) and [`ClipboardImage`](../Services/ClipboardImage.cs.md). Clear coordination with `_inputField`, `_messageManager`, and events like `OnMessageSubmitted` and `OnImagePasted` is essential to keep the chat input responsive and consistent across channels.


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


OnMessageListAccepting is a private event handler that runs when the user activates a selected item in the chat message list. It first validates that the list's `Source` is a [`ChatListSource`](Chat/ChatListSource.cs.md), then obtains the currently selected line via `GetLine` and guards against nulls or invalid indices. Depending on the line's state, it dispatches the appropriate action: if the line exposes a `JumpToMessageId`, it calls `ScrollToMessage(jumpTarget)` and marks the event as handled (`e.Handled = true`); if the line has an attachment (both `AttachmentUrl` and `AttachmentFileName` non-null), it prioritizes the attachment type by invoking `OnAudioPlayRequested` for audio, `OnFileDownloadRequested` for files, or `OnImageOpenRequested` for images, each time setting `e.Handled` to true. If no attachment applies, the method converts the line to text and checks for an `@mention` using `ClickMentionRegex()`; on success it requests the mentioned user’s profile via `OnUserProfileRequested`. It then checks for a channel reference with `ClickChannelRegex()` and, on success, requests joining that channel via `OnChannelJoinRequested`. If none of the above apply, it falls back to opening the sender’s profile if a `SenderUsername` is present, again setting `e.Handled` to true for the activation.

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


OnMessageListKeyDown is a private key-down handler for the message list. It focuses the input when the user presses `F6Key` and, for `Key.Delete.KeyCode` or `Key.Backspace.KeyCode`, initiates a delete for the currently selected message by obtaining its `MessageId` from the line at the current index and calling `ConfirmDeleteMessage`, with the server enforcing permission and the client only signaling intent.

## Remarks
This symbol centralizes keyboard interactions for the message list so common shortcuts translate into UI and server actions rather than scattered ad-hoc logic. It guards against invalid states by ensuring the source is a [`ChatListSource`](Chat/ChatListSource.cs.md), that there is a valid selection within bounds, and that the retrieved line contains a `MessageId` before invoking `ConfirmDeleteMessage`. The actual permission check occurs on the server; the client simply signals intent when the user presses delete-related keys.

## Notes
- If the currently selected line cannot provide a `MessageId`, the delete flow is bypassed and nothing is sent to the server.
- The handler marks the event as handled for both focus and deletion-related keys to prevent default behavior and potential duplicate processing.
- This is a UI-level interception; changes to the underlying [`ChatListSource`](Chat/ChatListSource.cs.md) shape or the line retrieval API may require corresponding updates to maintain the delete flow.

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


This private event handler processes mouse input from the chat message list (`_messageList`). It distinguishes left-clicks from right-clicks and ignores other inputs. For a left-click performed on a line that contains an attachment action span and provides an `AttachmentUrl` and an `AttachmentFileName`, it dispatches the corresponding operation by invoking `OnImageOpenRequested` or `OnImageSaveRequested` with the `url` and `name`, and marks the event as handled. For a right-click, it selects the clicked row, focuses the list, and shows the message context menu via `ShowMessageContextMenu`.

## Remarks
Separation of concerns: UI input handling is isolated here, mapping raw mouse events to high-level actions (attachment operations or context-menu invocation). It coordinates with the chat line data (via `AttachmentUrl`, `AttachmentFileName`, and `ActionSpans`) and with the event callbacks (`OnImageOpenRequested`, `OnImageSaveRequested`) to perform the appropriate operation.

## Notes
- Left-click path only triggers an action when the click lies within an `ActionSpan` for the line; otherwise, the code returns and preserves normal selection behavior.
- Right-click path selects the row and shows the context menu, enabling per-row actions.
- All action branches set `e.Handled = true` to prevent further processing and ensure predictable UI behavior.

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


This private method handles the vertical scrollbar's scroll event for the message list. When the scrollbar reaches the top (i.e., `_messageList.VerticalScrollBar.Value` is 0), it invokes `OnLoadMoreRequested` to request loading older messages.

## Remarks
It acts as a lightweight bridge between the UI event and the data-loading workflow. By exposing the `OnLoadMoreRequested` signal, the function keeps scroll-triggered loading decoupled from the actual loading implementation, enabling subscribers to decide how to fetch and prepend older messages. The top-only condition aligns with common chat UX, ensuring a deliberate user action triggers a load.

## Notes
- Repeatedly reaching the top while scrolling could trigger multiple load requests if the subscriber doesn't guard against reentrancy or throttling.
- The invocation uses a null-conditional call (`OnLoadMoreRequested?.Invoke()`); if there are no subscribers, it is a no-op.

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


OnMessagesChanged handles a channel-name change by updating the UI context: if the provided `channelName` matches the current channel from `_messageManager.CurrentChannel`, it refreshes the message list via `RefreshMessages()`; otherwise it refreshes the channel list via `RefreshChannelList()`. It then triggers the status area to redraw by calling `_statusLabel.SetNeedsDraw()` to reflect background activity.

## Remarks
Serves as a compact coordinator that keeps the messages view, channel list, and status indicator in sync with the active channel. It centralizes the decision between refreshing messages or the channel roster and ensures the status bar reflects updates arising from channel or message changes, all without exposing this logic beyond the UI layer.

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


OnStatusBarDrawContent is a private event handler that renders the EchoHub status bar during its draw cycle. It builds a single, grapheme-aware line by consulting the current color scheme via `SchemeManager.GetScheme("Menu")`, applying a fallback background where needed, and writing segments through a local `Write` helper until the `Viewport.Width` is exhausted. The method echoes branding (`" EchoHub"`), the app version (`AppVersion`), connection status (with an animated spinner for transitional states), the current user, the active channel with visibility modifiers, and an activity summary of channels with unread messages, finally padding the remaining space. It cancels the default drawing by setting `e.Cancel = true` to ensure full control over the rendered line.


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


Responds to the user acceptance action on the users list. It reads the currently selected item index from `_usersList`, validates that a value exists and is within the bounds of `_usersListSource`, and then gets the corresponding `username` via `_usersListSource.GetUsername(index.Value)`. If a non-null `username` is found, it raises the `OnUserProfileRequested` event with that `username` and marks the action as handled by setting `e.Handled = true` to prevent further command processing.

## Remarks
Encapsulates the UI behavior of opening a user profile in a small, focused handler. The handler decouples the act of selecting a user from the navigation logic by emitting `OnUserProfileRequested`, allowing the application to decide how to present the profile. The invocation uses a null-conditional operator, so subscribers may opt into profile navigation without forcing a handler here.

## Notes
- The `OnUserProfileRequested` invocation uses the null-conditional operator, so the absence of subscribers results in a no-op rather than an exception.
- The method guards against invalid selections (no selection, negative index, or index out of range) and against a missing username, ensuring it only attempts to navigate when a valid username is available.


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


OnWindowKeyDown is a private window event handler that maps a small set of keyboard shortcuts to actions on the main window. When the user presses `AltQKey`, `F2Key`, or `CtrlKKey`, it invokes `_app.RequestStop()` (quit the app), `ToggleUsersPanel()` (toggle the users panel), or `ShowSearchDialog()` (open the search UI) respectively; for any other key, it returns without handling. If a known shortcut is processed, it sets `e.Handled = true` to prevent further propagation of the event.

## Remarks
This private handler centralizes keyboard accessibility for the main window by funneling a few shortcuts through a single place. It decouples the raw key events from the UI actions (`_app.RequestStop()`, `ToggleUsersPanel()`, `ShowSearchDialog()`), ensuring consistent and predictable behavior for the defined shortcuts.

## Notes
- Only the configured shortcuts consume the event; other keys are ignored and may bubble to other handlers.
- Changing bindings or the actions requires updating the corresponding `AltQKey`, `F2Key`, `CtrlKKey` definitions or the called methods.
- The handler is synchronous and marks the event as handled (`e.Handled = true`) after processing a known shortcut.

---

### RefreshChannelList
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void RefreshChannelList()
```

**Returns:** `void`


RefreshChannelList updates the channel list UI to display unread counts next to each channel, while pinning system channels to the top and preserving the relative order of the other channels. It computes private channels from the channel names and visibility, then updates the channel list source with the latest data (unread counts, current channel, protected and mention channels, private channels, and system channels) and applies the result to the UI by setting the channel list source and restoring the current selection via the current channel.

## Remarks
By centralizing this logic in `RefreshChannelList`, the UI presentation stays consistent with the underlying channel state and unread counts. It leverages `_channelNames` as the source of truth for selection, uses `_systemChannels` to identify system channels, and updates `_channelListSource` in one place to reflect unread counts, privacy, and channel lifecycle, reducing duplication and improving stability when channel data changes. This design keeps user context intact by re-selecting `_messageManager.CurrentChannel` after refresh.

## Notes
- Must be invoked on the UI thread; touching `_channelList` or `_channelListSource` from a background thread can cause cross-thread exceptions.

---

### RefreshMenuBar
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void RefreshMenuBar()
```

**Returns:** `void`


RefreshMenuBar rebuilds and replaces the menu bar after theme list changes. It removes the current `_menuBar`, rebuilds it via `BuildMenuBar()`, re-attaches it with `Add(_menuBar)`, applies color schemes with `ApplyColorSchemes()`, and finally requests a redraw via `SetNeedsDraw()`.

## Remarks
Centralizes the refresh of the main navigation UI after theme updates, ensuring the active theme is rendered consistently in the menu bar. It coordinates the menu lifecycle by removing the old `_menuBar`, rebuilding a fresh instance with `BuildMenuBar()`, re-attaching it with `Add(_menuBar)`, and then applying color schemes via `ApplyColorSchemes()` before signaling a redraw with `SetNeedsDraw()`.

---

### RefreshMessages
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void RefreshMessages()
```

**Returns:** `void`


Refreshes the chat list for the current channel by rebuilding a [`ChatListSource`](Chat/ChatListSource.cs.md) from the messages returned by `_messageManager.GetMessages(_messageManager.CurrentChannel)` and wiring it to `_messageList`. When a valid viewport width is available, it wraps lines (or expands rule labels with `ExpandRule`) to that width and selects the most recent item; if the width is not yet known, it uses the last cached width in `_lastChatWidth` to preserve layout across resizes. If no channel is selected (the messages call returns null), it renders a MOTD-style welcome banner via `WelcomeBanner.Build(width, AppVersion)` and assigns that as the source.

## Remarks
This method centralizes UI refresh logic for the chat area, ensuring consistent behavior when changing channels or resizing the window. It delegates rendering details to [`ChatListSource`](Chat/ChatListSource.cs.md) (and the associated `Render` pathway) while relying on the viewport width to determine wrapping and layout, and it uses `_lastChatWidth` to maintain a stable presentation during initial layout passes.


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


Removes a channel from the left panel by clearing its references from several internal state collections and then refreshing the display. Given a `channelName`, it deletes that name from `_channelNames`, `_channelTopics`, `_channelPublic`, `_channelProtected`, and `_systemChannels`, before calling `RefreshChannelList()` to update the UI.

## Remarks
Consolidating the removal into a single method ensures the UI and data-model stay in sync. By performing all removals before triggering `RefreshChannelList()`, it avoids partially updated state that could occur if the collections were modified independently. If the channel name is not present, the removals are no-ops and the method still triggers a refresh, making the operation idempotent at the call site.

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


ScrollToMessage scrolls the message list to the first line of a given message, a helper invoked by reply quote navigation to reveal the original message in the loaded buffer. It selects the matching line (where `line.MessageId == messageId` and `line.JumpToMessageId` is null), scrolls the list so the item is visible with up to three preceding lines, then focuses and redraws the list. If the message is not present in the current buffer, the method returns without modifying the UI.

## Remarks
ScrollToMessage encapsulates a small piece of navigation logic for the chat UI: it isolates the actions needed to reveal a concrete message while maintaining user context. By ignoring lines that are quotes (`line.JumpToMessageId` not null), it ensures the navigation lands on the original message rather than a quoted reference. The method coordinates with `_messageList` and the underlying [`ChatListSource`](Chat/ChatListSource.cs.md) to set the selection, adjust the scroll position via `TopItem`, and request a redraw, producing a predictable experience when replying to messages.

## Notes
- No-op behavior: if the target message is not loaded in the current buffer, the method returns without changing the UI.
- Target correctness: the navigation only lands on lines where `JumpToMessageId` is null, avoiding jump-to-quote lines that may reference the same message.


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


Sets or updates the topic for a given channel. It assigns the provided `topic` to the `_channelTopics` store for the key `channelName`. If the updated channel is the one currently displayed in the UI (as indicated by `_messageManager.CurrentChannel`), it refreshes the topic display by calling `UpdateTopicBar()`.

## Remarks
This method centralizes per-channel topic management by mutating the internal topic store and, only when relevant, triggering a UI refresh. It keeps data and presentation in sync for the active channel while avoiding unnecessary work for other channels, thus maintaining UI responsiveness and consistency between the data model and the topic bar.

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


Rebuilds the internal channel state from the provided `List<ChannelDto>` and refreshes the channel list UI. It clears the internal collections `_channelNames`, `_channelTopics`, `_channelPublic`, `_channelProtected`, and `_systemChannels`, then populates them from each [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) in `channels` (adding `ch.Name` to `_channelNames`, setting `_channelTopics[ch.Name]` to `ch.Topic`, and `_channelPublic[ch.Name]` to `ch.IsPublic`). If a channel is protected or system, it additionally tracks those names in `_channelProtected` and `_systemChannels`, respectively, before finally calling `RefreshChannelList()`.

## Remarks
Bulk-replaces rather than incrementally updating; this is intended when the server-provided channel list changes in full. By consolidating all channel attributes (topic, visibility, and category flags) under the channel name key, it keeps the UI and internal views consistent with a single refresh.

## Notes
- Assumes `channels` is non-null and that all channel names are unique; nulls or duplicates can cause runtime errors or inconsistent state because the method does not validate input.

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


Updates the active user name by forwarding the provided `username` to the message manager's `SetCurrentUser` method, enabling proper @mention detection. This wrapper lets the UI layer update the current user without needing to know how the message manager stores or uses the username.

## Remarks
Because this symbol is a forwarding wrapper, it preserves a clean separation of concerns: the UI layer remains decoupled from the internal mention-processing logic, with the `_messageManager` handling the actual behavior. It centralizes the current-user context in a single component, making future changes to mention handling easier to apply without touching UI code.

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


SetReplyingTo updates the input frame's title to reflect the message you're replying to. When a non-null `label` is supplied, it builds the title fragment ``↩ Replying to {label} │ Esc=cancel`` and stores it in the internal field ``_replyTitleFragment``; passing `null` clears the indicator. It then refreshes the UI by calling ``UpdateInputTitle()``.

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
This method centralizes the UI logic for reflecting the current set of staged attachments in the input title, ensuring callers don't assemble the title text themselves. It constructs a compact label beginning with the 📎 emoji, followed by the count and a comma-separated list of filenames (truncated when too long), then the ASCII-size hint and quick actions. After updating `_stagedTitleFragment`, it calls `UpdateInputTitle()` to refresh the on-screen title.

## Example
```csharp
// Example: attach two files and display their names with a 128x64 ASCII size hint
SetStagedAttachments(new[] { "image1.png", "image2.png" }, "128x64");
```

## Notes
- If the joined filenames exceed 45 characters, they're truncated to preserve layout, appending '...'.
- Passing an empty list resets the hint by clearing `_stagedTitleFragment` and updating the title.
- This method updates UI state and should be invoked on the UI thread where the input frame lives to avoid cross-thread issues.

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


ShowError is a small UI helper that presents an error dialog to the user by calling `MessageBox.ErrorQuery(_app, "Error", message, "OK")`. Use this method when you want to surface a user-facing error with consistent styling and boilerplate centralized in one place.

## Remarks
This wrapper centralizes error presentation, enforcing a consistent user experience by always using the same dialog title and button label via `MessageBox.ErrorQuery(_app, "Error", message, "OK")`. It also isolates UI-dialog boilerplate so changes to the underlying dialog surface can be made in one place without changing call sites. It assumes `_app` is a valid UI context; if `_app` is null or the call is made on a non-UI thread, it can fail or cause exceptions.

## Notes
- Requires a valid UI thread context; ensure `_app` is initialized before calling.

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


Shows a right-click context menu for a chat line, consolidating attachment actions, reply handling, mention/profile commands, text copying, and deletion with server-side permission checks. The method builds the menu by inspecting the [`ChatLine`](Chat/ChatLine.cs.md) for an attachment (its [`AttachmentKind`](../../EchoHub.Core/Models/AttachmentKind.cs.md), `AttachmentUrl`, and `AttachmentFileName`), the presence of a sender, and whether the line is a reply target (`MessageId`). It then populates a `PopoverMenu` with `MenuItem`s such as `Open image`, `Save original image`, `Play audio`, `Download file`, `Reply`, `Mention @sender`, `View {sender}'s profile`, `Copy text`, `Copy message ID`, and `Delete message` (the latter gated by the existence of `MessageId`). Actions are wired to events like `OnImageOpenRequested`, `OnImageSaveRequested`, `OnAudioPlayRequested`, `OnFileDownloadRequested`, `OnReplyRequested`, `OnUserProfileRequested`, and helper calls such as `CopyToClipboard` and `ConfirmDeleteMessage`. The UI is shown via `_app.Popovers` by registering the menu and displaying it at the given `screenPosition`.



---

### ShowSearchDialog
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void ShowSearchDialog()
```

**Returns:** `void`


ShowSearchDialog is a private helper that triggers the search UI by raising the `OnSearchRequested` event. It encapsulates the mechanism of opening a search dialog so UI controls can simply invoke it without depending on a concrete dialog implementation.

## Remarks

By funneling the open-search action through this private method, the class remains decoupled from how the search dialog is presented. The event-driven approach lets any subscriber decide how to respond to a search request, enabling easier testing and customization. The null-conditional invocation (`OnSearchRequested?.Invoke()`) ensures a safe no-op when no components are listening, avoiding the need for explicit subscriber checks in call sites.

## Notes

- If no subscribers exist for `OnSearchRequested`, this method does nothing, which is a deliberate no-op. Calling code should subscribe to the event if it needs a visible search UI.

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


Staging files (from a drop or a file-clipboard paste) as attachments in one batch; the next Enter sends them with any typed caption. The method retrieves the current channel from `_messageManager.CurrentChannel` and, if a channel is present, notifies listeners by invoking `OnFilesStaged` with the channel and the provided file paths; if there is no active channel, it returns without action.

## Remarks
`StageFiles` serves as a small adapter between the UI action of dropping or pasting files and the sending workflow. By emitting `OnFilesStaged`, it decouples the staging concern from the actual send operation, allowing different parts of the UI or logic to respond to staged files without the method needing to know what happens next.

## Notes
- No action occurs if there is no active channel (`_messageManager.CurrentChannel` is null or empty).
- The invocation uses `OnFilesStaged?.Invoke(channel, files)`; if there are no subscribers, nothing happens without throwing.

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


SwitchToChannel switches the chat view to the specified channel by name, resets its unread count, and refreshes related UI so the active channel is clearly reflected to the user. It updates the underlying current channel in the `_messageManager`, updates the chat window title to the channel header prefixed with a hash (`#` + channelName), clears unread markers for that channel via `_messageManager.ClearUnread(channelName)`, refreshes the channel list and messages, updates the topic bar and input state, redraws the status label, and aligns the channel list selection when the channel exists in `_channelNames` by setting `_channelList.SelectedItem` to the channel's index if found.

---

### ToggleUsersPanel
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
public void ToggleUsersPanel()
```

**Returns:** `void`


`ToggleUsersPanel` flips the private field `_usersPanelVisible` to its opposite value and then calls `UpdateLayout()` to refresh the UI accordingly. It is typically invoked in response to the user pressing the `F2` key to show or hide the online users panel.

---

### TryAutocompleteCommand
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void TryAutocompleteCommand()
```

**Returns:** `void`


It tab-completes slash commands entered into the input field. When the user begins typing a command (text starting with `/` and containing no spaces yet), it matches against the known `SlashCommands` using a case-insensitive comparison (`StringComparison.OrdinalIgnoreCase`). If there is a single match, it replaces the input with that command plus a trailing space; if there are multiple matches, it computes the longest common prefix among the matches and updates the input to that prefix to guide refinement, and finally moves the cursor to the end via `_inputField.InsertionPoint`.

---

### UpdateInputReadOnly
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void UpdateInputReadOnly()
```

**Returns:** `void`


Disables user input when the current channel is read-only by assigning `ReadOnly` on `_inputField` based on `IsCurrentChannelReadOnly`, and then refreshes the input frame title via `UpdateInputTitle` to reflect the new state.

## Remarks
By encapsulating this behavior in a single method, the UI consistently represents interactivity and state across channel changes. It prevents input in system/read-only channels and ensures the input frame title communicates the current mode, avoiding drift between interactivity and labeling. This approach also centralizes the read-only logic around the `IsCurrentChannelReadOnly` state source, simplifying future changes.

---

### UpdateInputTitle
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void UpdateInputTitle()
```

**Returns:** `void`


UpdateInputTitle refreshes the `_inputFrame` title to reflect the current reply and staged hints; if the current channel is read-only (`IsCurrentChannelReadOnly`), it writes the fixed message `Read-only channel — you cannot type here` to `_inputFrame.Title` and immediately requests a redraw via `_inputFrame.SetNeedsDraw()`. Otherwise it computes the title from `_replyTitleFragment` and `_stagedTitleFragment` with a four-case switch: both null → `DefaultInputTitle`, only reply → `reply`, only staged → `staged`, or both present → `"{reply} │ {staged}"`, followed by a redraw. 

## Remarks
This method centralizes the UI title logic, ensuring read-only channels take precedence and that the title cleanly represents combined states when both a reply and a staged title exist. It couples state fragments with the input frame’s rendering, reducing duplication and keeping the UI consistent across edits.

---

### UpdateLayout
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void UpdateLayout()
```

**Returns:** `void`


Recomputes and applies the main window layout whenever the users panel visibility changes. It determines a right margin based on `_usersPanelVisible` (using `UsersPanelWidth` when the panel is visible, or 0 otherwise), updates `_chatFrame.Width`, `_topicLabel.Width`, and `_inputFrame.Width` using `Dim.Fill(rightMargin)`, toggles `_usersFrame.Visible` accordingly, and then calls `SetNeedsDraw()` to refresh the UI.

## Remarks
By centralizing width calculations in a single private helper, this method keeps the layout logic consistent and minimizes layout drift as the panel appears or disappears. It acts as the synchronization point between the panel visibility state and the content frames, ensuring the chat, topic label, and input areas always use the remaining horizontal space.

## Notes
- Ensure this runs on the UI thread; UI elements are updated here, and invoking from a background thread can lead to race conditions or exceptions.

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


Updates the on-screen list of online users by converting each [`UserPresenceDto`](../../EchoHub.Core/DTOs/ProfileDtos.cs.md) into a display tuple that includes a status glyph, an optional role glyph, the user's display name (falling back to the username), and a color derived from either the nickname color or the username. It also marks IRC-connected users with an `[irc]` suffix and then updates `_usersListSource`, binds it to `_usersList`, and refreshes the frame title with the current user count. This method centralizes the formatting logic for the user list and ensures color and status presentation stay consistent with chat messages.

## Remarks
It centralizes the presentation of user entries for the online users panel, ensuring status, role, name, and color are consistently derived from the same sources ([`UserStatus`](../../EchoHub.Core/Models/UserStatus.cs.md), [`ServerRole`](../../EchoHub.Core/Models/ServerRole.cs.md), and nickname/username color palettes). By reusing the deterministic color logic and the inline role and status icons, the UI remains consistent with chat message coloring and user identity.

## Notes
- This method updates UI state and should be executed on the UI thread to avoid threading issues.
- If `NicknameColor` is not parseable, the color falls back to `NickColorHelper.GetAttribute(u.Username)`.

---

### UpdateSpinner
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void UpdateSpinner()
```

**Returns:** `void`


Starts the spinner timer when entering a transitional connection state (e.g., `Connecting`, `Reconnecting`); the timer stops itself once the state settles. It guards against starting multiple timers by returning early when `_spinnerToken` is not null, then schedules a 120ms callback via `_app.AddTimeout` that, if still transitional, rotates the spinner frame by incrementing `_spinnerFrame` modulo `SpinnerFrames.Length` and triggers a redraw of `_statusLabel`; the callback returns true to continue scheduling and false to stop when `IsTransitionalStatus` becomes false.

## Remarks
Encapsulates the spinner animation logic so the UI keeps a single, self-terminating indicator while the connection state is in flux. It relies on `SpinnerFrames` for the frame sequence and uses `_statusLabel.SetNeedsDraw()` to refresh the display. The `_spinnerToken` field ensures only one active timer exists at a time, avoiding overlapping animation loops.

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


Updates the connection status displayed in the status bar. This method assigns the new status to the internal `_connectionStatus`, triggers the spinner update via `UpdateSpinner()`, and requests a redraw of the label by calling `_statusLabel.SetNeedsDraw()`.

Call this method whenever the connection state changes, to keep the status bar in sync without spreading UI update logic elsewhere.

## Remarks
This method centralizes the UI update path for connection state changes in the main window. It ensures consistency between the underlying `_connectionStatus` and the visuals by coordinating state assignment, spinner visibility, and redraw scheduling through `_statusLabel.SetNeedsDraw()`.

---

### UpdateTopicBar
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** method

```csharp
private void UpdateTopicBar()
```

**Returns:** `void`


Updates the topic bar for the current channel by reading the topic from `_channelTopics` for the current channel via `_messageManager.CurrentChannel`. If a non-empty topic is found, `_topicLabel.Text` is set to `Topic: {topic}`, `_topicLabel` is made visible, and `_chatFrame.Y` is set to 2 to make room for the topic bar. If there is no topic, `_topicLabel` is hidden and `_chatFrame.Y` is set to 1.

## Remarks
Keeping this logic in one place decouples topic data from layout decisions, ensuring a consistent UI state whenever channels switch or topics change. It coordinates `_channelTopics` with `_messageManager.CurrentChannel` to update `_topicLabel` and `_chatFrame` in lockstep, so the presence or absence of a topic immediately reflects in the UI.

---

### AltQKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode AltQKey = KeyCode.Q | KeyCode.AltMask
```


Defines the keyboard shortcut Alt+Q as a single `KeyCode` value by bitwise OR-ing `KeyCode.Q` with `KeyCode.AltMask`. This private constant centralizes the Alt+Q pattern so input checks can reference `AltQKey` instead of composing the combination inline, improving readability and reducing the risk of inconsistencies in the UI input handling.

## Remarks
Private to the enclosing class, `AltQKey` serves as a single source of truth for the Alt+Q shortcut within the main window's input handling. This encapsulation keeps the shortcut localized and ensures future changes (e.g., modifying the modifier or the base key) only need to modify this constant. By naming the combination, it communicates intent and reduces cognitive load when reviewing input checks.

---

### AppVersion
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
internal static readonly string AppVersion =
        typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "?"
```


AppVersion provides a concise, ready-to-display version string for the UI by taking the `Version` of the assembly containing `MainWindow`, formatting it with three components via `ToString(3)`, and falling back to `?` when the version cannot be determined. Developers typically reference `AppVersion` when showing the application version in the UI or logs to avoid duplicating assembly-lookup logic.

## Remarks

By deriving from `typeof(MainWindow).Assembly`, the value is tied to the UI assembly's metadata, ensuring the string reflects exactly the UI binary the user interacts with. It is evaluated once during type initialization and remains constant for the lifetime of the process; the null-coalescing ensures a non-null string even if the UI assembly lacks a `Version`.

## Notes

- Computed once per process lifecycle; subsequent reads are a cheap field access.
- This value is internal to the containing assembly, so external components cannot rely on it being visible; use a public API if you need to surface the version externally.


---

### CtrlCKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode CtrlCKey = KeyCode.C | KeyCode.CtrlMask
```


Encodes the Ctrl+C keyboard shortcut as a single `KeyCode` value by combining `KeyCode.C` with `KeyCode.CtrlMask`. This private constant is used by the UI to detect when the user presses the `Ctrl+C` shortcut, consolidating the detection logic in one location to avoid duplicating the combo throughout `MainWindow`.

## Remarks

Centralizes input handling for a common shortcut, so the detection is consistent across the class. If the shortcut changes, update this single constant rather than scattered inline checks. The private scope marks it as an internal wiring detail of the UI, not part of the public API. It encodes a Ctrl-based shortcut; consider platform-specific Cmd+C handling for macOS if cross-platform parity is needed.

## Notes

- This member is private to `src/EchoHub.Client/UI/MainWindow.cs`; it cannot be accessed from outside. If external components need to reference the shortcut, consider exposing it via an internal or public API or by providing a helper method.

---

### CtrlKKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode CtrlKKey = KeyCode.K | KeyCode.CtrlMask
```


This private constant named `CtrlKKey` encodes the Ctrl+K keyboard shortcut as a `KeyCode` value by combining `KeyCode.K` with `KeyCode.CtrlMask`. It is used in the `src/EchoHub.Client/UI/MainWindow.cs` keyboard input handling to detect when the user presses `Ctrl+K`, centralizing the shortcut representation rather than duplicating the bitwise expression across the code.

## Remarks
By keeping the shortcut in a single `CtrlKKey` field, the codebase gains a single source of truth for this shortcut. This reduces duplication and makes future changes to the `CtrlKKey` binding easier to maintain within `MainWindow.cs`.

## Notes
- On some platforms or Unity configurations, modifier bits may vary; if you need to support Cmd on macOS or other modifiers, you may need a broader input check rather than relying solely on `KeyCode.CtrlMask`.

---

### CtrlVKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode CtrlVKey = KeyCode.V | KeyCode.CtrlMask
```


The `CtrlVKey` field encodes the Ctrl+V shortcut as a composite `KeyCode` value by combining `KeyCode.V` with `KeyCode.CtrlMask` (`KeyCode.V | KeyCode.CtrlMask`). As a private constant in `src/EchoHub.Client/UI/MainWindow.cs`, it provides a single canonical value for detecting paste commands in input handling, avoiding repeated bitwise construction scattered through the code. Use this symbol whenever you need to detect paste-like input from the user, rather than comparing against `KeyCode.V` or the control modifier separately.

---

### CtrlXKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode CtrlXKey = KeyCode.X | KeyCode.CtrlMask
```


Represents the keyboard shortcut Ctrl+X as a `KeyCode` value by combining `KeyCode.X` with `KeyCode.CtrlMask` and is intended for use in the UI input handling within `MainWindow` to detect the Ctrl+X shortcut. By centralizing the combination in this `private const`, code that reacts to Ctrl+X can simply compare against `CtrlXKey` instead of duplicating the bitwise OR expression in multiple places.

## Remarks
This constant serves as a single source of truth for the Ctrl+X shortcut within the `MainWindow` input pipeline. It improves readability by exposing the intent of the key combination (X with the Ctrl modifier) and makes future changes to the shortcut straightforward—update the constant in one place rather than hunting through the codebase. Being `private`, its usage is intentionally confined to the class boundary, reinforcing encapsulation around the UI's keyboard handling.

## Notes
- `CtrlXKey` is a `private const`, so the value is inlined by the compiler and not accessible from outside.
- If you need to reuse the same shortcut elsewhere, consider extracting it to a shared location or exposing a public/internal member to avoid duplication.
- Ensure all input checks compare against `CtrlXKey` with the same modifier semantics (i.e., modifiers represented by `KeyCode.CtrlMask`).

---

### CtrlYKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode CtrlYKey = KeyCode.Y | KeyCode.CtrlMask
```


Represents the Ctrl+Y keyboard shortcut as a single `KeyCode` value by combining `KeyCode.Y` with `KeyCode.CtrlMask`. Use this constant in input handling to recognize Ctrl+Y presses without duplicating the bitwise expression, keeping the code readable and maintainable when checking for shortcuts in the UI.

## Remarks
Because it is declared as a private constant inside `src/EchoHub.Client/UI/MainWindow.cs`, the shortcut is kept private to the UI layer and serves as a single source of truth for this particular binding. This encapsulation makes it easy to update the shortcut in one place and ensures all input checks against `CtrlYKey` stay consistent across the related methods.

---

### DefaultInputTitle
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const string DefaultInputTitle = "Message │ Enter=send │ Tab=complete │ Ctrl+K=search │ F6=pick message"
```


`DefaultInputTitle` is a private compile-time constant string that defines the default title shown for the message input area in the main window. It is initialized with the user-facing hint text `Message │ Enter=send │ Tab=complete │ Ctrl+K=search │ F6=pick message`, which communicates the available keyboard shortcuts to users during initial UI presentation. This value is sourced from `src/EchoHub.Client/UI/MainWindow.cs` and is used to initialize the UI to provide consistent guidance at startup.

## Remarks
Because this constant is private to the UI class, it is solely used during initialization to ensure a consistent hint is presented across the session. If you need localization or runtime configurability, this hard-coded value should be moved to a resources file or exposed through a non-const property.

## Notes
- As a `private const string`, the value is compiled into the assembly and cannot be changed at runtime; consider localization if the application targets multiple languages.

---

### EnterKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode EnterKey = KeyCode.Enter
```


EnterKey is a private constant of type `KeyCode` set to `KeyCode.Enter`. It provides a single, reusable value for comparing input against the Enter key using raw `KeyCode` values, avoiding the extra semantics of `Key.Equals` (which also checks `Handled`). This approach keeps Enter-key handling in switch or conditional checks straightforward within the class.

## Remarks
By encapsulating the Enter key mapping in this private field, the class avoids duplicating the `KeyCode.Enter` literal and ensures consistent semantics across all internal input checks. It also decouples the input comparison logic from the `Key` class's equality semantics, making intent clearer and future changes to the binding easier to manage. Because the field is private, reuse across other classes would require a shared abstraction.

## Notes
- Private visibility means this constant isn't accessible outside its containing type; if cross-class usage is needed, expose a public constant or centralize key bindings in a shared utility.

---

### F2Key
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode F2Key = KeyCode.F2
```


Defines the F2 keyboard binding as a private constant `F2Key` set to `KeyCode.F2` for use in the `MainWindow` input handling. This avoids repeating the raw `KeyCode.F2` value scattered through the class and makes future changes to the F2 shortcut straightforward by updating a single symbol.

## Remarks
Replaces magic key literals with a named binding inside the class, improving readability and reducing the risk of inconsistent shortcuts. Being private ensures this mapping is encapsulated within the UI logic and not exposed to external components; if sharing the binding is needed, consider exposing it via a property or moving it to a shared constants location.

## Notes
- Private visibility means it cannot be referenced from outside the class; reuse across components would require a public or internal accessor or moving the binding to a shared constants file.
- Because it is a `const`, its value is baked at compile time; if runtime configurability is required, switch to a non-const static field.

---

### F6Key
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode F6Key = KeyCode.F6
```


Provides a single, immutable reference to the F6 key as a private constant `KeyCode` named `F6Key` in `src/EchoHub.Client/UI/MainWindow.cs`; this removes magic literals from input handling and makes it easy to adjust the binding in one place if needed. The value is fixed at compile time as `KeyCode.F6`.

## Remarks
By centralizing the binding in a private constant, the surrounding input logic can rely on a single source of truth for the F6 key, reducing drift and typos. Keeping it private confines the binding to `MainWindow`, signaling that this is an internal convention rather than a public API. If future needs require rebinding at runtime, replace this constant with a configurable alternative.

## Notes
- Because `F6Key` is a `const`, its value is baked in at compile time and cannot be changed at runtime. If you anticipate needing to rebind the key, switch to a mutable configuration-based approach.

---

### NewlineKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode NewlineKey = KeyCode.N | KeyCode.CtrlMask
```


Represents the private, compile-time constant `NewlineKey` of type `KeyCode` that encodes the `Ctrl+N` keyboard shortcut by combining `KeyCode.N` with `KeyCode.CtrlMask`. It is used in input handling to detect the `Ctrl+N` sequence without duplicating the bitwise expression across the class, enabling a single source of truth for this shortcut and a consistent trigger (such as initiating a new item or inserting a newline) wherever the UI logic responds to that keystroke.

## Remarks
This abstraction localizes the keyboard shortcut within the `MainWindow` class, reducing duplication and clarifying intent when handling input. Because the field is `private const`, its value is fixed at compile time and inaccessible from outside the class; if the shortcut needs to change, a code change and recompilation are required for the update to propagate.

## Notes
- Because `NewlineKey` is a `const`, its value is inlined at call sites by the compiler, so changes require recompiling all dependents that reference it.

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


This private static readonly field `SlashCommands` defines the set of slash commands available for tab-autocomplete in the chat input of the main window. It lists commands like `/status`, `/nick`, `/color`, `/theme`, `/send`, `/me`, `/banner`, `/avatar`, `/profile`, `/servers`, `/join`, `/passwd`, `/leave`, `/clear`, `/size`, `/downloadpath`, `/topic`, `/users`, `/kick`, `/ban`, `/unban`, `/mute`, `/unmute`, `/role`, `/invite`, `/export`, `/deleteaccount`, `/nuke`, `/test-sound`, `/quit`, and `/help`.

## Remarks
This field serves as the single source of truth for the UI's autocomplete behavior in `MainWindow.cs`. By making it `private static readonly`, the list is a shared, effectively constant source of suggestions that the tab-autocomplete logic can rely on at runtime, ensuring consistent user experience. If new slash commands are introduced elsewhere in the application, they must be added here to keep the autocomplete in sync with the available commands.

## Notes
- Because the list is hard-coded in source, changes require recompilation and redeployment for the UI to pick up new commands.
- The field is private; if cross-component reuse is needed, consider exposing a public accessor or moving the list to a shared configuration.

---

### SpinnerFrames
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly string[] SpinnerFrames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"]
```


SpinnerFrames is a private static readonly array of strings containing the braille spinner glyphs used to animate a spinner while the connection is in a transitional state. The UI cycles through these frames to convey progress during connectivity changes. 

## Remarks
By making the field static and readonly, the frame sequence is created once and cannot be mutated at runtime, ensuring a consistent animation across all usages within the class. Centralizing the frame sequence here avoids repeating literal frame values throughout the UI code and makes it straightforward to adjust the spinner's appearance in a single place.

## Notes
- Do not attempt to modify the frames at runtime; `SpinnerFrames` is `readonly`, so reassignment isn’t possible.
- Ensure the source file encoding supports the braille glyphs used in the frames; using an incompatible encoding may lead to garbled or lost characters.

---

### StatusActivityAttr
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Attribute StatusActivityAttr = new(new Color(80, 200, 220), Color.None)
```


StatusActivityAttr is a private, static, readonly field of type `Attribute` used to style status-activity visuals within the `MainWindow` UI. It is initialized with a single `Attribute` instance created as `new(new Color(80, 200, 220), Color.None)`, pairing a cyan primary color with no secondary color. Because the field is `static readonly`, the same configured attribute is reused across the class, ensuring a consistent look for status indicators throughout the UI.

## Remarks
Centralizes the color styling for status-activity visuals within the `MainWindow` UI, providing a single source of truth and reducing color-value duplication. Its `private` scope signals that this is an internal implementation detail, while the `static readonly` nature guarantees a single, immutable instance used consistently across all code paths that render status indicators.

---

### StatusBrandAttr
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Attribute StatusBrandAttr = new(new Color(218, 165, 32), Color.None)
```


StatusBrandAttr is a private static readonly instance of `Attribute` used by the UI to apply a consistent branding style to status indicators. It is initialized with a gold-toned primary color (`new Color(218, 165, 32)`) and a secondary color of `Color.None`. This centralized attribute lets internal logic reuse a single branding specification for status visuals, reducing duplication and drift across the UI.

## Remarks
Centralizing branding ensures that all status indicators share the same visual cue, making the UI feel cohesive. Because the field is private and static, it cannot be modified by external code, and changes to the branding can be made in one place. If future requirements call for exposing branding or sharing it across components, consider adding a public accessor or moving the attribute to a shared constants module.

## Notes
- The field is private; external types cannot reference `StatusBrandAttr`. If you need to reuse the branding in other components, expose a public accessor or move the constant to a shared resource.

---

### StatusConnectedAttr
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Attribute StatusConnectedAttr = new(new Color(0, 200, 0), Color.None)
```


Defines a private static readonly field `StatusConnectedAttr` of type `Attribute` that encodes the UI styling for a 'connected' status. It is initialized with a green primary color `new Color(0, 200, 0)` and a secondary color of `Color.None`, enabling a consistent connected-state cue across the main window UI.

## Remarks
By centralizing the color-based representation of the connected state in a single shared `Attribute`, this member prevents duplication and drift of UI styling. The `static readonly` modifier guarantees the value is created once per app domain and reused wherever the attribute is applied, promoting visual consistency in the `MainWindow` UI.

---

### StatusDisconnectedAttr
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Attribute StatusDisconnectedAttr = new(new Color(220, 50, 50), Color.None)
```


Defines a prebuilt `Attribute` for `StatusDisconnectedAttr` that represents the UI treatment for the 'Disconnected' state. It is initialized with a red color (`new Color(220, 50, 50)`) and no secondary color (`Color.None`), ensuring a consistent visual cue when signaling disconnection.

## Remarks
Centralizes the visual cue for disconnection as a single immutable token, guaranteeing that all disconnected indicators use the same color semantics. It is private to the containing class, so reuse is internal; if cross-class reuse is needed, expose a public or internal accessor or move the token to a shared theming resource.

## Notes
- Private accessibility limits reuse outside the declaring type; to share styling, expose an accessor or place the token in a shared theme.

---

### StatusMentionAttr
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Attribute StatusMentionAttr = new(new Color(230, 140, 60), Color.None)
```


Defines a shared, preconfigured instance of `Attribute` named `StatusMentionAttr` that provides the standard color styling for status mentions in the UI. Created as a private static readonly field, it initializes with a primary color of `new Color(230, 140, 60)` and a secondary color of `Color.None`, ensuring a consistent orange highlight without a background tint. Use this field whenever the UI needs the canonical status-mention appearance instead of constructing a new `Attribute` on each use.

## Remarks
By centralizing this styling in a single static field, the UI maintains a consistent look for status mentions across controls and avoids duplicating color configuration. It also makes the intent explicit: the orange highlight is reserved for status mentions and should be reused.

## Notes
- If the `Attribute` type exposes mutable state, avoid mutating `StatusMentionAttr` after initialization, as doing so would propagate changes across all uses within the class.

---

### StatusTransitionalAttr
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private static readonly Attribute StatusTransitionalAttr = new(new Color(220, 180, 0), Color.None)
```


The `StatusTransitionalAttr` is a private static readonly field of type `Attribute` that provides a preconfigured styling primitive for UI elements in a transitional state. It is initialized with a primary `Color` of `new Color(220, 180, 0)` and a secondary color of `Color.None`, enabling consistent usage across the UI without constructing new attributes repeatedly.

## Remarks
This field centralizes the visual cue for transitional statuses, ensuring a uniform appearance wherever it’s used within this class. Because it is private, reuse is limited to the defining type; if external components need the same look, expose a controlled accessor or move the attribute to a shared styling utility. It effectively acts as a single source of truth for the transitional color, so updates to the tone can be made in one place.

## Notes
- Being private, external code cannot reference `StatusTransitionalAttr`. If broader reuse is required, consider exposing an internal/public accessor or relocating the attribute to a shared styling layer.

---

### TabKey
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const KeyCode TabKey = KeyCode.Tab
```


This private constant, `TabKey`, provides a single, named reference to the `KeyCode.Tab` value used in the UI input handling within `src/EchoHub.Client/UI/MainWindow.cs`. It avoids scattering the literal `KeyCode.Tab` across the codebase, making tab-key checks more readable and easier to update if the navigation key changes.

## Remarks
By centralizing the tab key choice in a private constant, this symbol communicates intent clearly within the class and reduces duplication in tab-navigation checks. If you ever need to switch the navigation key, update `TabKey` in one place rather than modifying multiple conditional branches.

---

### UsersPanelWidth
> **File:** `src/EchoHub.Client/UI/MainWindow.cs`  
> **Kind:** field

```csharp
private const int UsersPanelWidth = 22
```


Defines the fixed width of the users panel in the main window as a private constant `UsersPanelWidth`, centralizing the panel’s sizing decisions. When adjusting the layout, developers should reference this constant rather than sprinkling literal values, ensuring consistent alignment across the UI and a single point for future tweaks.

## Remarks
This constant encapsulates a UI sizing decision that would otherwise be repeated across multiple layout expressions. Keeping it private to the `MainWindow` class communicates that the width is an implementation detail of the window’s layout, while still enabling reuse and straightforward changes if the design evolves.

---