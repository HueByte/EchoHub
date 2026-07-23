# Real-time connection management

> Managing the SignalR hub connection lifecycle and connection state.

# Real-time connection management

This guide explains how the client-side pieces manage a SignalR-based chat connection, surface server events to the UI, and carry message and attachment DTOs across those boundaries. Read it to understand which types own the HubConnection lifecycle, which types represent messages and attachments, and how connection orchestration hands events and histories back to the UI layer.

## EchoHubConnection.cs

Encapsulates the SignalR connection to the server and join history with encryption info.

The [EchoHubConnection](../Code/src/EchoHub.Client/Services/EchoHubConnection.cs.md) type is a thin, SignalR-backed client wrapper that owns a HubConnection and translates server callbacks into plain .NET events (for example OnMessageReceived, OnUserJoined, OnChannelUpdated). It also integrates client-side encryption and room-key lookup: incoming payloads are decrypted before being raised to subscribers, and join history plus encryption metadata is tracked so callers can present past messages. EchoHubConnection declares focused exception types such as ChannelPasswordRequiredException (thrown when a join fails for password reasons) to enable UI-driven retry flows. According to the file relationships, EchoHubConnection consumes message and channel shapes from [ChatDtos.cs](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) and is instantiated and used by [ConnectionManager](../Code/src/EchoHub.Client/Services/ConnectionManager.cs.md).

## ConnectionManager.cs

Coordinates connection lifecycle and connection events across the client.

The [ConnectionManager](../Code/src/EchoHub.Client/Services/ConnectionManager.cs.md) is the high-level owner of authentication, E2E key retrieval, HubConnection creation, wiring of SignalR callbacks, and tracking of joined channels. It exposes a small event surface that forwards the EchoHubConnection events to the UI (the doc notes AppOrchestrator subscribes), implements IAsyncDisposable to tear down both the hub wrapper and the underlying client, and reports progress from ConnectAsync via an onStatus callback while throwing on authentication failure. ConnectionManager also defines the [ConnectResult](../Code/src/EchoHub.Client/Services/ConnectionManager.cs.md) record that packages the login response, a list of channel DTOs, and a dictionary of channel histories (the histories contain [MessageDto] entries defined in ChatDtos). Per the relationships, ConnectionManager depends on [ChatDtos.cs](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) for payload shapes and on [EchoHubConnection](../Code/src/EchoHub.Client/Services/EchoHubConnection.cs.md) to manage the live SignalR interactions.

## ChatDtos.cs

`AttachmentDto` collaborates directly with `ConnectResult` and other members of this topic (9 dependency links).

[ChatDtos.cs](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) defines the immutable DTOs used across the connection boundary: records such as AttachmentDto, ChannelDto, ChannelMetaDto, MessageDto, JoinChannelResult, and request shapes like SendMessageRequest. The [AttachmentDto](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) record carries metadata for file attachments (kind, URL, filename, filesize, optional ASCII preview) and is explicitly designed to work with end-to-end encrypted channels where the resource behind Url may be ciphertext the server cannot read. These DTOs are the concrete payload shapes that both [EchoHubConnection](../Code/src/EchoHub.Client/Services/EchoHubConnection.cs.md) and [ConnectionManager](../Code/src/EchoHub.Client/Services/ConnectionManager.cs.md) send, receive, and store in histories.

How the pieces fit

ConnectionManager is the orchestration layer: it authenticates, attempts to acquire E2E keys, builds and wires an [EchoHubConnection](../Code/src/EchoHub.Client/Services/EchoHubConnection.cs.md), and exposes forwarded events to the UI while tracking joined channels and histories. EchoHubConnection is the SignalR-focused implementation that manages the HubConnection lifecycle, maps server callbacks to events, performs decryption of incoming payloads, and throws focused exceptions (for example ChannelPasswordRequiredException) so the UI can prompt and retry joins. The DTOs in [ChatDtos.cs](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) are the shared, immutable shapes (messages, channels, attachments) that flow between the manager, the hub wrapper, and the UI; ConnectResult packages those DTOs back to callers after an initial connect sequence.

---
*Covers 3 of 3 source files identified for this topic.*

*Synthesised by Aurion on 2026-07-23 05:51:44 UTC*
