# Onboarding — HueByte/EchoHub

> *A curated reading path through this codebase for new contributors. Work through the stops in order.*

This onboarding reading path gets a new engineer from zero to ready-to-contribute in EchoHub by steering you through a short, focused set of documents and source files. Read these stops in order: start with the architecture overview to understand the system shape, inspect the two Program entry points to see how the app is composed at startup, trace an ingress path through a client command and the server SignalR hub, then study the client-side services and where persistent shapes live. Each stop links to the exact files you should open and the concrete types you will meet.

## Stop 1: What this project is
Skim the generated system overview to pick up the collaboration pattern, the main components (client, server, realtime hubs, and services), and where state is declared and stored. Read the [Synthesis/Architecture.md](Architecture.md) document to get the high-level map the rest of this path assumes.

## Stop 2: Where execution starts
Open the two entry points to see how each app boots and what core infrastructure gets wired at startup: the client bootstrapper coordinates early-defense tasks, configuration, logging, and the transition into the main UI in [Code/src/EchoHub.Client/Program.cs.md](../Code/src/EchoHub.Client/Program.cs.md), while the server entry point in [Code/src/EchoHub.Server/Program.cs.md](../Code/src/EchoHub.Server/Program.cs.md) shows the resilient host loop that configures logging, application settings, and critical startup pieces such as EF Core and hosted services. These files are where DI registrations, hosted services, and the middleware/host lifecycle are established for the rest of the codebase.

## Stop 3: Where requests come in
Trace an ingress path by reading the client-side command entry point and the server realtime hub: the client [CommandHandler](../Code/src/EchoHub.Client/Commands/CommandHandler.cs.md) is the object that receives and processes user commands in the client, and the server-side [ChatHub](../Code/src/EchoHub.Server/Hubs/ChatHub.cs.md) is the authorized SignalR hub that exposes realtime messaging endpoints. Together they show the surface area for end-to-end interactions you’ll be working with when implementing features that span client UI and server realtime behavior.

## Stop 4: Where the business logic lives
Inspect the client services that implement the concrete work behind the UI and network surface: the [ApiClient](../Code/src/EchoHub.Client/Services/ApiClient.cs.md) (an IDisposable networking client), [AudioPlaybackService](../Code/src/EchoHub.Client/Services/AudioPlaybackService.cs.md) for playing audio, [ClientEncryptionService](../Code/src/EchoHub.Client/Services/ClientEncryptionService.cs.md) which implements IMessageEncryptionService for client-side message encryption, [NotificationSoundService](../Code/src/EchoHub.Client/Services/NotificationSoundService.cs.md) for notification sounds, and [UpdateBackupService](../Code/src/EchoHub.Client/Services/UpdateBackupService.cs.md) which participates in backup serialization (it declares a JsonSerializable for BackupInfo). These are the workhorse types you’ll modify when adding features like networking, media playback, encryption, and backups.

## Stop 5: Where state lives
See the concrete places that own state and persistence shapes in the client: the [CommandHandler](../Code/src/EchoHub.Client/Commands/CommandHandler.cs.md) participates in command processing, the [ConnectionManager](../Code/src/EchoHub.Client/Services/ConnectionManager.cs.md) is the internal IAsyncDisposable that manages connection lifecycle, [UpdateBackupService](../Code/src/EchoHub.Client/Services/UpdateBackupService.cs.md) defines the serializable BackupInfo shape, and the [ConnectDialog](../Code/src/EchoHub.Client/UI/Dialogs/ConnectDialog.cs.md) is the UI component that captures connection information. These files show what is stored or serialized and which client-side components own that state.

## Stop 6: Where to put new code
Follow the repository’s conventions for contributions by looking at the canonical homes for server-side APIs, domain services, and realtime endpoints: server HTTP endpoints live under controllers such as [AuthController](../Code/src/EchoHub.Server/Controllers/AuthController.cs.md) (an ApiController), domain operations belong in services like [ChannelService](../Code/src/EchoHub.Server/Services/ChannelService.cs.md) which implements IChannelService, and realtime features belong in hubs such as [ChatHub](../Code/src/EchoHub.Server/Hubs/ChatHub.cs.md) (an authorized SignalR hub). Use these locations as templates when you add new endpoints, service implementations, or hub methods.

## Next steps
Start by reading the architecture overview ([Synthesis/Architecture.md](Architecture.md)), then run and step through the two entry points ([Code/src/EchoHub.Client/Program.cs.md](../Code/src/EchoHub.Client/Program.cs.md) and [Code/src/EchoHub.Server/Program.cs.md](../Code/src/EchoHub.Server/Program.cs.md)) locally so you see the client and server bootstrapping in action; that will give you the context needed to pick a small first contribution such as adding a log statement in startup or a simple API/hub method wired through the locations above.

---
*Synthesised by Aurion on 2026-07-08 17:09:24 UTC*
