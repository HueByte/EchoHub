# Real-time connection and messaging

> How the client establishes and maintains a real-time connection to the server and handles channel messaging.

A compact overview of the client-side real-time layer: how the connection is created, the runtime surface it exposes to higher layers, and the DTOs used to carry chat and channel data. Read this to understand which types you call to open a SignalR-backed, end-to-end-capable chat connection, what events and exceptions to expect, and which transport records carry message and attachment metadata.

## EchoHubConnection.cs
Implements the SignalR connection lifecycle and messaging.

This file defines a small domain exception and a single, high-level connection wrapper. The `ChannelPasswordRequiredException` is a dedicated exception type that carries a `ChannelName` and signals that a channel join failed due to missing or invalid credentials; the doc recommends UIs catch this specific type to prompt for a password and retry. The `EchoHubConnection` class is an event-driven wrapper around a SignalR `HubConnection`: it registers the server callback handlers defined by the server contract, decrypts incoming content when necessary, exposes simple events for messages, presence and channel updates, surfaces connection-state changes, and centralizes token provision and reconnection wiring. Within this topic `EchoHubConnection` consumes the transport records from [ChatDtos.cs](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) and is constructed/managed by the [ConnectionManager](../Code/src/EchoHub.Client/Services/ConnectionManager.cs.md).

## ConnectionManager.cs
Orchestrates connection state and event wiring for the real-time layer.

`ConnectionManager` is the composition root for a full client connection: it performs authentication (via the project's HTTP API client), attempts to fetch and apply end-to-end encryption keys, constructs and wires an [EchoHubConnection](../Code/src/EchoHub.Client/Services/EchoHubConnection.cs.md), and maintains channel membership state. It forwards the hub's runtime events (for example message and presence events) as higher-level events such as `MessageReceived`, `UserJoined`, and `ConnectionStatusChanged`, so UI orchestrators can subscribe without touching SignalR internals; these forwarded events may be raised from SignalR threads and callers must marshal to the UI thread if required. `ConnectAsync` reports progress through an `onStatus` callback and returns a `ConnectResult` (also declared in this file) to indicate outcome; the class and the underlying `EchoHubConnection` both implement `IAsyncDisposable`, and the doc emphasizes awaiting disposal so resources (connection, tokens, keys) are cleaned up.

## ChatDtos.cs
`AttachmentDto` collaborates directly with `ConnectResult` and other members of this topic (9 dependency links).

This file declares the transport records used across the real-time boundary: types such as `AttachmentDto`, `ChannelCryptoDto`, `ChannelDto`, `ChannelMetaDto`, `CreateChannelRequest`, `EmbedDto`, `JoinChannelResult`, `MessageDto`, `RekeyChannelRequest`, `ReplyRefDto`, `SendMessageRequest`, `SendUrlRequest`, `UpdateTopicRequest`, and `UserDto` model messages, channels, users and channel crypto metadata. Concretely, `AttachmentDto` is a value record holding `Kind`, `Url`, `FileName`, `FileSize`, and an optional `AsciiPreview`; the doc highlights that `Url` and `AsciiPreview` may be ciphertext for end-to-end encrypted channels, and that attachments are carried as metadata so clients can fetch or preview content on demand. `ChannelCryptoDto` is a small record with `IsEncrypted` and an optional `EncryptionSalt` and is used to indicate whether channel payloads are protected. These DTOs are the typed payloads that [EchoHubConnection](../Code/src/EchoHub.Client/Services/EchoHubConnection.cs.md) emits and that [ConnectionManager](../Code/src/EchoHub.Client/Services/ConnectionManager.cs.md) tracks when reporting join results and message events.

How the pieces fit

ConnectionManager is the orchestration layer: it authenticates, applies E2E keys, constructs an [EchoHubConnection](../Code/src/EchoHub.Client/Services/EchoHubConnection.cs.md), and subscribes to its events so UI-level orchestrators can observe high-level events and results without dealing with SignalR. EchoHubConnection implements the low-level SignalR wiring, dispatches strongly-typed events and domain exceptions (for example `ChannelPasswordRequiredException`), and uses the DTOs from [ChatDtos.cs](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) as its message and channel payloads. Together they present a clear separation: DTOs define the wire shape, EchoHubConnection maps wire messages to runtime events and errors, and ConnectionManager composes those primitives into a single lifecycle and event surface for the UI.

---
*Covers 3 of 3 source files identified for this topic.*

*Synthesised by AurionDocs on 2026-07-23 09:31:01 UTC*
