# Attachments transfer

> Attachment handling for staged files and outgoing attachments in chat messages.

This guide describes how the client UI stages user-provided files and how the application constructs the immutable attachment objects that travel with outgoing chat messages. It explains the UI surface that users interact with, the small data carrier used to represent a prepared attachment, and the orchestrator that connects the two when a send or clear action occurs. Read this to quickly locate the methods you'll call to stage files, build upload payloads, and clean up temporary paste artifacts.

## MainWindow
Stage and manage file attachments in the chat input.

The [MainWindow](../Code/src/EchoHub.Client/UI/MainWindow.cs.md) type is the UI surface for composing messages and managing staged attachments. Its documented members include explicit input- and attachment-focused operations such as SetStagedAttachments and StageFiles (for adding files from disk or paste), ClearAll and HandleCmdClearAttachments-related flows, plus many UI helpers (FocusInput, UpdateInputTitle, UpdateInputReadOnly) that keep the compose area in sync. Per the file relationships, MainWindow is used by [AppOrchestrator](../Code/src/EchoHub.Client/AppOrchestrator.cs.md); the orchestrator drives MainWindow to display or clear staged attachments and reacts to user commands emitted from the window.

## OutgoingAttachment
Represents attachments prepared for sending with messages.

The [OutgoingAttachment](../Code/src/EchoHub.Client/Services/OutgoingAttachment.cs.md) record is a compact, immutable data carrier containing a Stream and the original FileName plus two optional fields: DeclaredKind and EncryptedPreview. As a record it provides value-based equality so attachments can be compared or deduplicated as they move through the pipeline. The DeclaredKind/EncryptedPreview pair is used to carry presentation/encryption metadata for end-to-end encrypted channels, while normal (non-encrypted) sends typically populate only Stream and FileName. The file is consumed by the orchestrator when preparing payloads for transmit.

## AppOrchestrator
`AppOrchestrator` collaborates directly with `OutgoingAttachment` and other members of this topic (2 dependency links).

The [AppOrchestrator](../Code/src/EchoHub.Client/AppOrchestrator.cs.md) mediates between UI actions and the attachment/send logic. Notable documented members include BuildOutgoingAttachmentAsync (the builder that produces an [OutgoingAttachment](../Code/src/EchoHub.Client/Services/OutgoingAttachment.cs.md) from a file/clipboard source), CleanupPastedTempFiles (removes temporary files created when pasting), and explicit command handlers such as HandleCmdSendFile and HandleCmdClearAttachments. AppOrchestrator depends on [MainWindow](../Code/src/EchoHub.Client/UI/MainWindow.cs.md) to reflect staged attachments in the UI and to respond to user-driven events; it constructs the immutable OutgoingAttachment values and manages lifecycle concerns (downloads, ensuring room unlocked for send, and cleanup).

How the pieces fit

- MainWindow is the UI owner of staged files: it exposes StageFiles, SetStagedAttachments, ClearAll and other composition helpers so users can add, view, and remove attachments before sending.
- AppOrchestrator listens for UI commands, calls BuildOutgoingAttachmentAsync to turn staged input into an [OutgoingAttachment](../Code/src/EchoHub.Client/Services/OutgoingAttachment.cs.md), and invokes the send/download/cleanup flows (including CleanupPastedTempFiles) as needed.
- OutgoingAttachment is the immutable transport object passed from the orchestrator into the send pipeline; optional DeclaredKind and EncryptedPreview carry E2EE-specific metadata when applicable.

---
*Covers 3 of 3 source files identified for this topic.*

*Synthesised by AurionDocs on 2026-07-23 09:32:36 UTC*
