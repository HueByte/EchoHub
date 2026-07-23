# Repo Map — HueByte/EchoHub

> Deterministic structural map generated for AI agents. Read this file for
> orientation, query [`symbol-graph.json`](symbol-graph.json) for exact
> dependencies, and open the linked per-file docs for behaviour — instead of
> scanning the source tree.

Commit `40aea9a04b2b4bd3a2e431cdd1cf4e1bfa11c343` · 201 symbols · 128 files · 655 dependency edges

## Subsystems

*Structural clusters detected from the dependency graph — groups of symbols more densely wired to each other than to the rest of the codebase.*

### src/EchoHub.Client/Services · AppOrchestrator

37 symbols across 23 files. Key symbols (by connectivity):

- [`AppOrchestrator`](../Code/src/EchoHub.Client/AppOrchestrator.cs.md) (class) — `src/EchoHub.Client/AppOrchestrator.cs`
- [`ConnectionManager`](../Code/src/EchoHub.Client/Services/ConnectionManager.cs.md) (class) — `src/EchoHub.Client/Services/ConnectionManager.cs`
- [`MessageDto`](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ChatDtos.cs`
- [`EchoHubConnection`](../Code/src/EchoHub.Client/Services/EchoHubConnection.cs.md) (class) — `src/EchoHub.Client/Services/EchoHubConnection.cs`
- [`RefreshToken`](../Code/src/EchoHub.Core/Models/RefreshToken.cs.md) (class) — `src/EchoHub.Core/Models/RefreshToken.cs`
- [`SavedServer`](../Code/src/EchoHub.Client/Config/ClientConfig.cs.md) (class) — `src/EchoHub.Client/Config/ClientConfig.cs`
- [`RoomKeyStore`](../Code/src/EchoHub.Client/Services/RoomKeyStore.cs.md) (class) — `src/EchoHub.Client/Services/RoomKeyStore.cs`
- [`IrcMessageFormatter`](../Code/src/EchoHub.Server.Irc/IrcMessageFormatter.cs.md) (class) — `src/EchoHub.Server.Irc/IrcMessageFormatter.cs`
- *…and 29 more (see symbol-graph.json)*

### src/EchoHub.Client/UI · MainWindow

22 symbols across 19 files. Key symbols (by connectivity):

- [`MainWindow`](../Code/src/EchoHub.Client/UI/MainWindow.cs.md) (class) — `src/EchoHub.Client/UI/MainWindow.cs`
- [`ChatMessageManager`](../Code/src/EchoHub.Client/UI/Chat/ChatMessageManager.cs.md) (class) — `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`
- [`ChatLine`](../Code/src/EchoHub.Client/UI/Chat/ChatLine.cs.md) (class) — `src/EchoHub.Client/UI/Chat/ChatLine.cs`
- [`ProfileViewDialog`](../Code/src/EchoHub.Client/UI/Dialogs/ProfileViewDialog.cs.md) (class) — `src/EchoHub.Client/UI/Dialogs/ProfileViewDialog.cs`
- [`AttachmentKind`](../Code/src/EchoHub.Core/Models/AttachmentKind.cs.md) (enum) — `src/EchoHub.Core/Models/AttachmentKind.cs`
- [`AttachmentDto`](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ChatDtos.cs`
- [`ChatColors`](../Code/src/EchoHub.Client/UI/Chat/ChatColors.cs.md) (class) — `src/EchoHub.Client/UI/Chat/ChatColors.cs`
- [`ChatSegment`](../Code/src/EchoHub.Client/UI/Chat/ChatSegment.cs.md) (record) — `src/EchoHub.Client/UI/Chat/ChatSegment.cs`
- *…and 14 more (see symbol-graph.json)*

### src/EchoHub.Core/DTOs · ApiClient

19 symbols across 11 files. Key symbols (by connectivity):

- [`ApiClient`](../Code/src/EchoHub.Client/Services/ApiClient.cs.md) (class) — `src/EchoHub.Client/Services/ApiClient.cs`
- [`ModerationController`](../Code/src/EchoHub.Server/Controllers/ModerationController.cs.md) (class) — `src/EchoHub.Server/Controllers/ModerationController.cs`
- [`ServerStatsCollector`](../Code/src/EchoHub.Server/Services/Stats/ServerStatsCollector.cs.md) (class) — `src/EchoHub.Server/Services/Stats/ServerStatsCollector.cs`
- [`RefreshRequest`](../Code/src/EchoHub.Core/DTOs/AuthDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/AuthDtos.cs`
- [`AssignRoleRequest`](../Code/src/EchoHub.Core/DTOs/ModerationDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ModerationDtos.cs`
- [`UpdateProfileRequest`](../Code/src/EchoHub.Core/DTOs/ProfileDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ProfileDtos.cs`
- [`AvatarHelper`](../Code/src/EchoHub.Client/Services/AvatarHelper.cs.md) (class) — `src/EchoHub.Client/Services/AvatarHelper.cs`
- [`OutgoingAttachment`](../Code/src/EchoHub.Client/Services/OutgoingAttachment.cs.md) (record) — `src/EchoHub.Client/Services/OutgoingAttachment.cs`
- *…and 11 more (see symbol-graph.json)*

### src/EchoHub.Server · User

19 symbols across 17 files. Key symbols (by connectivity):

- [`User`](../Code/src/EchoHub.Core/Models/User.cs.md) (class) — `src/EchoHub.Core/Models/User.cs`
- [`UsersController`](../Code/src/EchoHub.Server/Controllers/UsersController.cs.md) (class) — `src/EchoHub.Server/Controllers/UsersController.cs`
- [`EchoHubDbContext`](../Code/src/EchoHub.Server/Data/EchoHubDbContext.cs.md) (class) — `src/EchoHub.Server/Data/EchoHubDbContext.cs`
- [`ServerRole`](../Code/src/EchoHub.Core/Models/ServerRole.cs.md) (enum) — `src/EchoHub.Core/Models/ServerRole.cs`
- [`AuthController`](../Code/src/EchoHub.Server/Controllers/AuthController.cs.md) (class) — `src/EchoHub.Server/Controllers/AuthController.cs`
- [`UserProfileDto`](../Code/src/EchoHub.Core/DTOs/ProfileDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ProfileDtos.cs`
- [`UserService`](../Code/src/EchoHub.Server/Services/UserService.cs.md) (class) — `src/EchoHub.Server/Services/UserService.cs`
- [`IUserService`](../Code/src/EchoHub.Core/Contracts/IUserService.cs.md) (interface) — `src/EchoHub.Core/Contracts/IUserService.cs`
- *…and 11 more (see symbol-graph.json)*

### src/EchoHub.Core/DTOs · ChannelService

13 symbols across 9 files. Key symbols (by connectivity):

- [`ChannelService`](../Code/src/EchoHub.Server/Services/ChannelService.cs.md) (class) — `src/EchoHub.Server/Services/ChannelService.cs`
- [`IrcCommandHandler`](../Code/src/EchoHub.Server.Irc/IrcCommandHandler.cs.md) (class) — `src/EchoHub.Server.Irc/IrcCommandHandler.cs`
- [`IChannelService`](../Code/src/EchoHub.Core/Contracts/IChannelService.cs.md) (interface) — `src/EchoHub.Core/Contracts/IChannelService.cs`
- [`ChannelOperationResult`](../Code/src/EchoHub.Core/DTOs/CommonDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/CommonDtos.cs`
- [`FakeChannelService`](../Code/src/EchoHub.Tests/Irc/TestHelpers.cs.md) (class) — `src/EchoHub.Tests/Irc/TestHelpers.cs`
- [`ValidationConstants`](../Code/src/EchoHub.Core/Constants/ValidationConstants.cs.md) (class) — `src/EchoHub.Core/Constants/ValidationConstants.cs`
- [`ChannelCryptoDto`](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ChatDtos.cs`
- [`ChannelError`](../Code/src/EchoHub.Core/DTOs/CommonDtos.cs.md) (enum) — `src/EchoHub.Core/DTOs/CommonDtos.cs`
- *…and 5 more (see symbol-graph.json)*

### src/EchoHub.Server · Program

13 symbols across 12 files. Key symbols (by connectivity):

- [`Program`](../Code/src/EchoHub.Server/Program.cs.md) (file) — `src/EchoHub.Server/Program.cs`
- [`HubConstants`](../Code/src/EchoHub.Core/Constants/HubConstants.cs.md) (class) — `src/EchoHub.Core/Constants/HubConstants.cs`
- [`PresenceTracker`](../Code/src/EchoHub.Server/Services/PresenceTracker.cs.md) (class) — `src/EchoHub.Server/Services/PresenceTracker.cs`
- [`ImageToAsciiService`](../Code/src/EchoHub.Core/Services/ImageToAsciiService.cs.md) (class) — `src/EchoHub.Core/Services/ImageToAsciiService.cs`
- [`UploadLimits`](../Code/src/EchoHub.Server/Config/UploadLimits.cs.md) (class) — `src/EchoHub.Server/Config/UploadLimits.cs`
- [`DatabaseSetup`](../Code/src/EchoHub.Server/Setup/DatabaseSetup.cs.md) (class) — `src/EchoHub.Server/Setup/DatabaseSetup.cs`
- [`DirectoryClaimStore`](../Code/src/EchoHub.Server/Services/DirectoryClaimStore.cs.md) (class) — `src/EchoHub.Server/Services/DirectoryClaimStore.cs`
- [`LinkEmbedService`](../Code/src/EchoHub.Server/Services/LinkEmbedService.cs.md) (class) — `src/EchoHub.Server/Services/LinkEmbedService.cs`
- *…and 5 more (see symbol-graph.json)*

### src/EchoHub.Core/Contracts · ChannelDto

9 symbols across 9 files. Key symbols (by connectivity):

- [`ChannelDto`](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ChatDtos.cs`
- [`UserPresenceDto`](../Code/src/EchoHub.Core/DTOs/ProfileDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ProfileDtos.cs`
- [`ChatHub`](../Code/src/EchoHub.Server/Hubs/ChatHub.cs.md) (class) — `src/EchoHub.Server/Hubs/ChatHub.cs`
- [`IChatService`](../Code/src/EchoHub.Core/Contracts/IChatService.cs.md) (interface) — `src/EchoHub.Core/Contracts/IChatService.cs`
- [`IChatBroadcaster`](../Code/src/EchoHub.Core/Contracts/IChatBroadcaster.cs.md) (interface) — `src/EchoHub.Core/Contracts/IChatBroadcaster.cs`
- [`IrcBroadcaster`](../Code/src/EchoHub.Server.Irc/IrcBroadcaster.cs.md) (class) — `src/EchoHub.Server.Irc/IrcBroadcaster.cs`
- [`SignalRBroadcaster`](../Code/src/EchoHub.Server/Services/SignalRBroadcaster.cs.md) (class) — `src/EchoHub.Server/Services/SignalRBroadcaster.cs`
- [`IEchoHubClient`](../Code/src/EchoHub.Core/Contracts/IEchoHubClient.cs.md) (interface) — `src/EchoHub.Core/Contracts/IEchoHubClient.cs`
- *…and 1 more (see symbol-graph.json)*

### src/EchoHub.Server · ServerDirectoryService

7 symbols across 1 files. Key symbols (by connectivity):

- [`ServerDirectoryService`](../Code/src/EchoHub.Server/Services/ServerDirectoryService.cs.md) (class) — `src/EchoHub.Server/Services/ServerDirectoryService.cs`
- [`ErrorDetail`](../Code/src/EchoHub.Server/Services/ServerDirectoryService.cs.md) (record) — `src/EchoHub.Server/Services/ServerDirectoryService.cs`
- [`Response`](../Code/src/EchoHub.Server/Services/ServerDirectoryService.cs.md) (record) — `src/EchoHub.Server/Services/ServerDirectoryService.cs`
- [`DirectoryProtocol`](../Code/src/EchoHub.Server/Services/ServerDirectoryService.cs.md) (class) — `src/EchoHub.Server/Services/ServerDirectoryService.cs`
- [`DirectoryRegistrationErrors`](../Code/src/EchoHub.Server/Services/ServerDirectoryService.cs.md) (class) — `src/EchoHub.Server/Services/ServerDirectoryService.cs`
- [`RegisterServerDto`](../Code/src/EchoHub.Server/Services/ServerDirectoryService.cs.md) (record) — `src/EchoHub.Server/Services/ServerDirectoryService.cs`
- [`RegisterServerResult`](../Code/src/EchoHub.Server/Services/ServerDirectoryService.cs.md) (record) — `src/EchoHub.Server/Services/ServerDirectoryService.cs`

### src/EchoHub.Client/UI · Channel

6 symbols across 4 files. Key symbols (by connectivity):

- [`Channel`](../Code/src/EchoHub.Core/Models/Channel.cs.md) (class) — `src/EchoHub.Core/Models/Channel.cs`
- [`SearchDialog`](../Code/src/EchoHub.Client/UI/Dialogs/SearchDialog.cs.md) (class) — `src/EchoHub.Client/UI/Dialogs/SearchDialog.cs`
- [`SearchResultType`](../Code/src/EchoHub.Client/UI/Dialogs/SearchDialog.cs.md) (enum) — `src/EchoHub.Client/UI/Dialogs/SearchDialog.cs`
- [`SearchListSource`](../Code/src/EchoHub.Client/UI/ListSources/SearchListSource.cs.md) (class) — `src/EchoHub.Client/UI/ListSources/SearchListSource.cs`
- [`SearchResult`](../Code/src/EchoHub.Client/UI/Dialogs/SearchDialog.cs.md) (record) — `src/EchoHub.Client/UI/Dialogs/SearchDialog.cs`
- [`IrcNumericReply`](../Code/src/EchoHub.Server.Irc/IrcNumericReply.cs.md) (class) — `src/EchoHub.Server.Irc/IrcNumericReply.cs`

### src/EchoHub.Client/UI · UserStatus

6 symbols across 5 files. Key symbols (by connectivity):

- [`UserStatus`](../Code/src/EchoHub.Core/Models/UserStatus.cs.md) (enum) — `src/EchoHub.Core/Models/UserStatus.cs`
- [`StatusDialog`](../Code/src/EchoHub.Client/UI/Dialogs/StatusDialog.cs.md) (class) — `src/EchoHub.Client/UI/Dialogs/StatusDialog.cs`
- [`UserSession`](../Code/src/EchoHub.Client/Services/UserSession.cs.md) (class) — `src/EchoHub.Client/Services/UserSession.cs`
- [`StatusDialogResult`](../Code/src/EchoHub.Client/UI/Dialogs/StatusDialog.cs.md) (record) — `src/EchoHub.Client/UI/Dialogs/StatusDialog.cs`
- [`UserDto`](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ChatDtos.cs`
- [`UpdateStatusRequest`](../Code/src/EchoHub.Core/DTOs/ProfileDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ProfileDtos.cs`

### src/EchoHub.Core/DTOs · ChannelsController

6 symbols across 3 files. Key symbols (by connectivity):

- [`ChannelsController`](../Code/src/EchoHub.Server/Controllers/ChannelsController.cs.md) (class) — `src/EchoHub.Server/Controllers/ChannelsController.cs`
- [`RekeyChannelRequest`](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ChatDtos.cs`
- [`FileValidationHelper`](../Code/src/EchoHub.Core/Services/FileValidationHelper.cs.md) (class) — `src/EchoHub.Core/Services/FileValidationHelper.cs`
- [`CreateChannelRequest`](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ChatDtos.cs`
- [`SendUrlRequest`](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ChatDtos.cs`
- [`UpdateTopicRequest`](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ChatDtos.cs`

### src/EchoHub.Core/Models

6 symbols across 6 files. Key symbols (by connectivity):

- [`Message`](../Code/src/EchoHub.Core/Models/Message.cs.md) (class) — `src/EchoHub.Core/Models/Message.cs`
- [`DataMigrationService`](../Code/src/EchoHub.Server/Setup/DataMigrationService.cs.md) (class) — `src/EchoHub.Server/Setup/DataMigrationService.cs`
- [`Attachment`](../Code/src/EchoHub.Core/Models/Attachment.cs.md) (class) — `src/EchoHub.Core/Models/Attachment.cs`
- [`AsyncRunner`](../Code/src/EchoHub.Client/Services/AsyncRunner.cs.md) (class) — `src/EchoHub.Client/Services/AsyncRunner.cs`
- [`MessageType`](../Code/src/EchoHub.Core/Models/MessageType.cs.md) (enum) — `src/EchoHub.Core/Models/MessageType.cs`
- [`ApiResponse`](../Code/src/EchoHub.Core/DTOs/CommonDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/CommonDtos.cs`

*…and 11 smaller subsystems (see symbol-graph.json).*

## Most connected symbols

The load-bearing symbols — changes here have the widest blast radius.

| Symbol | Kind | Used by | Uses | File |
|---|---|---|---|---|
| [`AppOrchestrator`](../Code/src/EchoHub.Client/AppOrchestrator.cs.md) | class | 3 | 54 | `src/EchoHub.Client/AppOrchestrator.cs` |
| [`Program`](../Code/src/EchoHub.Server/Program.cs.md) | file | 0 | 34 | `src/EchoHub.Server/Program.cs` |
| [`ApiClient`](../Code/src/EchoHub.Client/Services/ApiClient.cs.md) | class | 3 | 29 | `src/EchoHub.Client/Services/ApiClient.cs` |
| [`ChatService`](../Code/src/EchoHub.Server/Services/ChatService.cs.md) | class | 2 | 29 | `src/EchoHub.Server/Services/ChatService.cs` |
| [`Channel`](../Code/src/EchoHub.Core/Models/Channel.cs.md) | class | 29 | 1 | `src/EchoHub.Core/Models/Channel.cs` |
| [`MainWindow`](../Code/src/EchoHub.Client/UI/MainWindow.cs.md) | class | 2 | 25 | `src/EchoHub.Client/UI/MainWindow.cs` |
| [`Message`](../Code/src/EchoHub.Core/Models/Message.cs.md) | class | 21 | 4 | `src/EchoHub.Core/Models/Message.cs` |
| [`ChannelsController`](../Code/src/EchoHub.Server/Controllers/ChannelsController.cs.md) | class | 0 | 25 | `src/EchoHub.Server/Controllers/ChannelsController.cs` |
| [`ConnectionManager`](../Code/src/EchoHub.Client/Services/ConnectionManager.cs.md) | class | 1 | 22 | `src/EchoHub.Client/Services/ConnectionManager.cs` |
| [`MessageDto`](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) | record | 20 | 3 | `src/EchoHub.Core/DTOs/ChatDtos.cs` |

*Regenerated on every full documentation run; see [README](README.md) for how to use this pack.*