# Onboarding — HueByte/EchoHub

> *A curated reading path through this codebase for new contributors. Work through the stops in order.*

This onboarding reading path gets a new contributor from zero to the point where they can make a small, safe change in the EchoHub codebase. Read these stops in order: start with the generated architecture overview to understand the system shape, open the entry points to see how programs and the server host are configured, follow a request through the ingress layer into the business services, and finish by locating the conventional folders where you should add new controllers, services, or hubs.

## Stop 1: What this project is
At this stop you will skim the auto-generated system overview to learn the high-level collaboration pattern, the main components, and where persistent state is kept. Read the [Architecture](Architecture.md) document to pick up the generated dependency map and the summary descriptions the project uses to show which services, APIs, and storage pieces are primary.

## Stop 2: Where execution starts
This stop shows how the client and server processes are bootstrapped so you can see dependency injection, one-time setup, and host configuration before diving deeper. Open the client [Program.cs](../Code/src/EchoHub.Client/Program.cs.md) to see its CLI handling (the --rollback flag), best-effort Unix execute-permission check, and how it provisions configuration; then inspect the server [Program.cs](../Code/src/EchoHub.Server/Program.cs.md) to see the call to FirstRunSetup.EnsureAppSettings(), the bootstrap logger configuration, and the code path that starts the ASP.NET Core host.

## Stop 3: Where requests come in
Trace an incoming action end-to-end by reading the client-side command processor and the server ingress points that handle requests and real-time messages. Examine the client [CommandHandler.cs](../Code/src/EchoHub.Client/Commands/CommandHandler.cs.md) to learn how client commands are dispatched, the [UsersController](../Code/src/EchoHub.Server/Controllers/UsersController.cs.md) to see the HTTP API surface exposed by an [ApiController], and the real-time path via the authorized [ChatHub](../Code/src/EchoHub.Server/Hubs/ChatHub.cs.md) to understand how authenticated SignalR messages are handled.

## Stop 4: Where the business logic lives
Follow the workhorses invoked by the ingress layer: the HTTP/SignalR handlers call into these services to perform the real operations. Read the client-side [ApiClient](../Code/src/EchoHub.Client/Services/ApiClient.cs.md) (a disposable HTTP client wrapper), [AudioPlaybackService](../Code/src/EchoHub.Client/Services/AudioPlaybackService.cs.md) for playback responsibilities, [ClientEncryptionService](../Code/src/EchoHub.Client/Services/ClientEncryptionService.cs.md) which implements IMessageEncryptionService for message-level encryption, [NotificationSoundService](../Code/src/EchoHub.Client/Services/NotificationSoundService.cs.md) for UI sounds, and [UpdateBackupService](../Code/src/EchoHub.Client/Services/UpdateBackupService.cs.md) which participates in backup serialization (JsonSerializable for BackupInfo).

## Stop 5: Where state lives
Identify the concrete types that own runtime and persisted state so you know what to change when you add data or lifecycle concerns. Revisit [CommandHandler.cs](../Code/src/EchoHub.Client/Commands/CommandHandler.cs.md) for command-driven client state transitions, inspect the connection lifecycle in the internal sealed [ConnectionManager](../Code/src/EchoHub.Client/Services/ConnectionManager.cs.md) (IAsyncDisposable), see how UI-driven folder selection is encapsulated in the static [NativeFolderPicker](../Code/src/EchoHub.Client/Services/NativeFolderPicker.cs.md), and review [UpdateBackupService](../Code/src/EchoHub.Client/Services/UpdateBackupService.cs.md) for how BackupInfo is serialized for persistence.

## Stop 6: Where to put new code
Learn the conventional locations to add controllers, services, and hubs by looking at existing examples in the server surface. The server exposes authentication endpoints in [AuthController](../Code/src/EchoHub.Server/Controllers/AuthController.cs.md) (an [ApiController]), long-running or domain behavior belongs in services such as [ChannelService](../Code/src/EchoHub.Server/Services/ChannelService.cs.md) which implements IChannelService, and real-time endpoints belong in hubs like the authorized [ChatHub](../Code/src/EchoHub.Server/Hubs/ChatHub.cs.md).

## Next steps
Try this as your first contribution: read the [Architecture](Architecture.md) overview, run the dev server from the server [Program.cs](../Code/src/EchoHub.Server/Program.cs.md) entry point, and make a tiny change (for example, add a log line in [ChatHub](../Code/src/EchoHub.Server/Hubs/ChatHub.cs.md)) to verify your local build and run loop.

---
*Synthesised by AurionDocs on 2026-07-23 09:34:36 UTC*
