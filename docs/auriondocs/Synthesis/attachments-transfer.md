# Attachments and file transfers

> Staging and sending attachments in chat messages and coordinating outbound attachments.

Outgoing attachments are staged in the UI, packaged as transport objects, and then coordinated through the app orchestrator into the live connection for transmission. This topic shows the small set of types and methods that carry file streams and metadata from the MainWindow staging UI through AppOrchestrator into the connection layer so they can be uploaded (optionally encrypted) and surfaced as attachment DTOs in messages.

## OutgoingAttachment.cs
Represents an attachment queued for sending to a channel or user.

The [OutgoingAttachment](../Code/src/EchoHub.Client/Services/OutgoingAttachment.cs.md) record is the in-process transport object used to carry a single file stream and its filename through the sending pipeline. It declares four properties: the raw Stream and FileName (required), and two optional strings DeclaredKind and EncryptedPreview which are intended for end-to-end encrypted scenarios. As a record it provides value-based equality for tracking/deduplication but notably does not manage the Stream lifetime — callers open and dispose streams around instances of this type. In this topic it is produced/consumed by the orchestrator layer (see [AppOrchestrator](../Code/src/EchoHub.Client/AppOrchestrator.cs.md)).

## MainWindow.cs
Provides UI hooks for staging attachments and displaying progress.

The [MainWindow](../Code/src/EchoHub.Client/UI/MainWindow.cs.md) component exposes the user-facing hooks that allow files to be staged and progress or status to be shown. Among its many members are StageFiles (to accept user-selected files) and SetStagedAttachments (to update the UI with the current list of staged items), plus UI update methods such as UpdateSpinner/UpdateInputTitle to reflect in-progress operations. MainWindow depends on the message/attachment DTO types in [ChatDtos.cs](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) for rendering metadata and is called by [AppOrchestrator](../Code/src/EchoHub.Client/AppOrchestrator.cs.md) when orchestrated work (prepare/send/clear attachments) must update the UI.

## AppOrchestrator.cs
Builds outbound attachments and coordinates sending operations.

The [AppOrchestrator](../Code/src/EchoHub.Client/AppOrchestrator.cs.md) owns the high-level send flow: it implements BuildOutgoingAttachmentAsync to assemble outbound attachment payloads (creating [OutgoingAttachment](../Code/src/EchoHub.Client/Services/OutgoingAttachment.cs.md) instances), provides cleanup helpers such as CleanupPastedTempFiles, and contains command handlers like HandleCmdSendFile and HandleCmdClearAttachments that respond to user actions. It depends on the DTO types in [ChatDtos.cs](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) when preparing message payloads and coordinates with the UI by reading staged files from and writing status back to [MainWindow](../Code/src/EchoHub.Client/UI/MainWindow.cs.md). For transmission the orchestrator delegates connection and delivery responsibilities to the connection layer ([ConnectionManager](../Code/src/EchoHub.Client/Services/ConnectionManager.cs.md)).

## ConnectionManager.cs
`ConnectionManager` collaborates directly with `AppOrchestrator` and other members of this topic (4 dependency links).

The [ConnectionManager](../Code/src/EchoHub.Client/Services/ConnectionManager.cs.md) encapsulates the live chat connection lifecycle: authentication, optional end-to-end key fetching, and instantiation/wiring of the SignalR hub connection. It exposes a thin event surface so UI code (principally [AppOrchestrator](../Code/src/EchoHub.Client/AppOrchestrator.cs.md)) can subscribe to SignalR events without dealing with SignalR details, and it implements IAsyncDisposable so the orchestrator can tear down network resources cleanly. ConnectAsync (documented in the file) reports progress via a provided onStatus callback, treats failure to obtain an E2E key as non-fatal, and returns compound results (the internal [ConnectResult](../Code/src/EchoHub.Client/Services/ConnectionManager.cs.md) record) that include login, channel list, and histories for the orchestrator to use.

## ChatDtos.cs
`AttachmentDto` collaborates directly with `AppOrchestrator` and other members of this topic (4 dependency links).

The [AttachmentDto](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) is the immutable transport representation of an attachment that travels with messages: it records the attachment Kind, a Url where the resource can be retrieved, FileName, FileSize, and an optional AsciiPreview used for character-art rendering. The DTO is the canonical metadata shape used across UI, API, and connection boundaries; the orchestrator uses these DTO types when composing or processing message payloads, and the MainWindow reads them to render attachments in the UI. In end-to-end encrypted channels the DTO’s Url and AsciiPreview may represent ciphertext that the server cannot interpret.

How the pieces fit

- UI staging: users pick files via [MainWindow](../Code/src/EchoHub.Client/UI/MainWindow.cs.md). MainWindow.StageFiles and SetStagedAttachments hold the files and show progress to the user while AppOrchestrator drives the workflow.
- Packaging: [AppOrchestrator](../Code/src/EchoHub.Client/AppOrchestrator.cs.md) constructs [OutgoingAttachment](../Code/src/EchoHub.Client/Services/OutgoingAttachment.cs.md) records (via BuildOutgoingAttachmentAsync), cleans up temp files, and maps to the DTO shapes from [ChatDtos.cs](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) when preparing messages.
- Delivery: the orchestrator delegates network work to [ConnectionManager](../Code/src/EchoHub.Client/Services/ConnectionManager.cs.md), which manages connection/auth/E2E keys and forwards events so the UI and orchestrator can report progress and completion.

---
*Covers 5 of 5 source files identified for this topic.*

*Synthesised by Aurion on 2026-07-23 05:53:19 UTC*
