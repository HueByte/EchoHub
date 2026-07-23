# Onboarding — HueByte/EchoHub

> *A curated reading path through this codebase for new contributors. Work through the stops in order.*

This reading path gets a new team member from zero to a place where they can run the app and make a small contribution. Read the short architecture overview first to understand the system's collaboration pattern, then inspect the entry points to see how the pieces are wired; from there follow a single request through the ingress layer into the services and state so you can start making safe, focused changes.

## Stop 1: What this project is
At this stop skim the auto-generated system description to learn the overall collaboration pattern and where state is owned; the document also highlights the main components and their responsibilities. Start by opening [Architecture](Architecture.md) to pick up the big-picture boundaries and the primary data stores so later code-level reads map to that conceptual model.

## Stop 2: Where execution starts
Read the two Program entry points to see how the client and server are bootstrapped, which early runtime concerns are wired, and what cross-cutting services are registered. Inspect the client [Program.cs](../Code/src/EchoHub.Client/Program.cs.md) to see startup tasks like rollback handling, permission checks, configuration provisioning, logging setup, and PATH preparation; then open the server [Program.cs](../Code/src/EchoHub.Server/Program.cs.md) to see how configuration, logging, data access, authentication, service registrations and the ASP.NET Core pipeline are arranged.

## Stop 3: Where requests come in
Trace a single end-to-end interaction by following the client command entry, the server HTTP controller, and the real-time hub used for chat. Read the client [CommandHandler.cs](../Code/src/EchoHub.Client/Commands/CommandHandler.cs.md) to learn how user commands are emitted, then the server [UsersController.cs](../Code/src/EchoHub.Server/Controllers/UsersController.cs.md) to see the API surface that handles user-related requests, and finally the SignalR [ChatHub.cs](../Code/src/EchoHub.Server/Hubs/ChatHub.cs.md) to understand real-time message routing and authorization checks.

## Stop 4: Where the business logic lives
Drill into the substantive services that perform work for the client: network calls, audio, encryption, and backup orchestration. Read the client [ApiClient.cs](../Code/src/EchoHub.Client/Services/ApiClient.cs.md) that manages HTTP requests and disposal, the [AudioPlaybackService.cs](../Code/src/EchoHub.Client/Services/AudioPlaybackService.cs.md) that handles playback concerns, the [ClientEncryptionService.cs](../Code/src/EchoHub.Client/Services/ClientEncryptionService.cs.md) which implements IMessageEncryptionService for message protection, the [NotificationSoundService.cs](../Code/src/EchoHub.Client/Services/NotificationSoundService.cs.md) for user-facing alerts, and the [UpdateBackupService.cs](../Code/src/EchoHub.Client/Services/UpdateBackupService.cs.md) which is involved in BackupInfo serialization and backup flows.

## Stop 5: Where state lives
Look at the code that owns connection state, persisted backups, and the commands that drive application state changes. Revisit [CommandHandler.cs](../Code/src/EchoHub.Client/Commands/CommandHandler.cs.md) to understand the commands that mutate client state, inspect [ConnectionManager.cs](../Code/src/EchoHub.Client/Services/ConnectionManager.cs.md) for the lifecycle and disposal of live connections, open [UpdateBackupService.cs](../Code/src/EchoHub.Client/Services/UpdateBackupService.cs.md) to see how BackupInfo is serialized for persistence, and check the UI [ConnectDialog.cs](../Code/src/EchoHub.Client/UI/Dialogs/ConnectDialog.cs.md) to learn where connection information is captured and handed off to the connection manager.

## Stop 6: Where to put new code
Use the conventional places represented by controllers, server services, and hubs when deciding where to add features or fixes. For HTTP and auth-related endpoints add or update controllers like [AuthController.cs](../Code/src/EchoHub.Server/Controllers/AuthController.cs.md); server-side domain operations belong in services such as [ChannelService.cs](../Code/src/EchoHub.Server/Services/ChannelService.cs.md) (which implements IChannelService); and real-time or cross-connection behavior belongs in the SignalR hub [ChatHub.cs](../Code/src/EchoHub.Server/Hubs/ChatHub.cs.md).

## Next steps
Run the app locally: read the two [Program.cs](../Code/src/EchoHub.Server/Program.cs.md) and [Program.cs](../Code/src/EchoHub.Client/Program.cs.md) files to learn how to start the server and client, then launch both projects and use the Connect dialog to exercise the [ChatHub](../Code/src/EchoHub.Server/Hubs/ChatHub.cs.md) path.

---
*Synthesised by Aurion on 2026-07-23 05:54:49 UTC*
