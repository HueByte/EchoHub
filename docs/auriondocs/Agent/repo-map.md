# Repo Map — HueByte/EchoHub

> Deterministic structural map generated for AI agents. Read this file for
> orientation, query [`symbol-graph.json`](symbol-graph.json) for exact
> dependencies, and open the linked per-file docs for behaviour — instead of
> scanning the source tree.

Commit `3538ec8005fee153b5741b2eab6823365dae8fa9` · 152 symbols · 99 files · 468 dependency edges

## Subsystems

*Structural clusters detected from the dependency graph — groups of symbols more densely wired to each other than to the rest of the codebase.*

### src/EchoHub.Client/UI · AppOrchestrator

21 symbols across 13 files. Key symbols (by connectivity):

- [`AppOrchestrator`](../Code/src/EchoHub.Client/AppOrchestrator.cs.md) (class) — `src/EchoHub.Client/AppOrchestrator.cs`
- [`ConnectionManager`](../Code/src/EchoHub.Client/Services/ConnectionManager.cs.md) (class) — `src/EchoHub.Client/Services/ConnectionManager.cs`
- [`RefreshToken`](../Code/src/EchoHub.Core/Models/RefreshToken.cs.md) (class) — `src/EchoHub.Core/Models/RefreshToken.cs`
- [`ConnectResult`](../Code/src/EchoHub.Client/Services/ConnectionManager.cs.md) (record) — `src/EchoHub.Client/Services/ConnectionManager.cs`
- [`ClientConfig`](../Code/src/EchoHub.Client/Config/ClientConfig.cs.md) (class) — `src/EchoHub.Client/Config/ClientConfig.cs`
- [`SavedServer`](../Code/src/EchoHub.Client/Config/ClientConfig.cs.md) (class) — `src/EchoHub.Client/Config/ClientConfig.cs`
- [`ConfigManager`](../Code/src/EchoHub.Client/Config/ConfigManager.cs.md) (class) — `src/EchoHub.Client/Config/ConfigManager.cs`
- [`LoginResponse`](../Code/src/EchoHub.Core/DTOs/AuthDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/AuthDtos.cs`
- *…and 13 more (see symbol-graph.json)*

### src/EchoHub.Core/DTOs · ChatService

16 symbols across 13 files. Key symbols (by connectivity):

- [`ChatService`](../Code/src/EchoHub.Server/Services/ChatService.cs.md) (class) — `src/EchoHub.Server/Services/ChatService.cs`
- [`MessageDto`](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ChatDtos.cs`
- [`ChannelDto`](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ChatDtos.cs`
- [`UserPresenceDto`](../Code/src/EchoHub.Core/DTOs/ProfileDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ProfileDtos.cs`
- [`IChatService`](../Code/src/EchoHub.Core/Contracts/IChatService.cs.md) (interface) — `src/EchoHub.Core/Contracts/IChatService.cs`
- [`ChatHub`](../Code/src/EchoHub.Server/Hubs/ChatHub.cs.md) (class) — `src/EchoHub.Server/Hubs/ChatHub.cs`
- [`EchoHubConnection`](../Code/src/EchoHub.Client/Services/EchoHubConnection.cs.md) (class) — `src/EchoHub.Client/Services/EchoHubConnection.cs`
- [`IChatBroadcaster`](../Code/src/EchoHub.Core/Contracts/IChatBroadcaster.cs.md) (interface) — `src/EchoHub.Core/Contracts/IChatBroadcaster.cs`
- *…and 8 more (see symbol-graph.json)*

### src/EchoHub.Client/UI · MainWindow

14 symbols across 13 files. Key symbols (by connectivity):

- [`MainWindow`](../Code/src/EchoHub.Client/UI/MainWindow.cs.md) (class) — `src/EchoHub.Client/UI/MainWindow.cs`
- [`ChatMessageManager`](../Code/src/EchoHub.Client/UI/Chat/ChatMessageManager.cs.md) (class) — `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`
- [`ProfileViewDialog`](../Code/src/EchoHub.Client/UI/Dialogs/ProfileViewDialog.cs.md) (class) — `src/EchoHub.Client/UI/Dialogs/ProfileViewDialog.cs`
- [`MessageType`](../Code/src/EchoHub.Core/Models/MessageType.cs.md) (enum) — `src/EchoHub.Core/Models/MessageType.cs`
- [`ChatLine`](../Code/src/EchoHub.Client/UI/Chat/ChatLine.cs.md) (class) — `src/EchoHub.Client/UI/Chat/ChatLine.cs`
- [`ChatListSource`](../Code/src/EchoHub.Client/UI/Chat/ChatListSource.cs.md) (class) — `src/EchoHub.Client/UI/Chat/ChatListSource.cs`
- [`HexColorHelper`](../Code/src/EchoHub.Client/UI/Helpers/HexColorHelper.cs.md) (class) — `src/EchoHub.Client/UI/Helpers/HexColorHelper.cs`
- [`ChatColors`](../Code/src/EchoHub.Client/UI/Chat/ChatColors.cs.md) (class) — `src/EchoHub.Client/UI/Chat/ChatColors.cs`
- *…and 6 more (see symbol-graph.json)*

### src/EchoHub.Server · User

13 symbols across 11 files. Key symbols (by connectivity):

- [`User`](../Code/src/EchoHub.Core/Models/User.cs.md) (class) — `src/EchoHub.Core/Models/User.cs`
- [`ServerRole`](../Code/src/EchoHub.Core/Models/ServerRole.cs.md) (enum) — `src/EchoHub.Core/Models/ServerRole.cs`
- [`AuthController`](../Code/src/EchoHub.Server/Controllers/AuthController.cs.md) (class) — `src/EchoHub.Server/Controllers/AuthController.cs`
- [`UserProfileDto`](../Code/src/EchoHub.Core/DTOs/ProfileDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ProfileDtos.cs`
- [`UsersController`](../Code/src/EchoHub.Server/Controllers/UsersController.cs.md) (class) — `src/EchoHub.Server/Controllers/UsersController.cs`
- [`IUserService`](../Code/src/EchoHub.Core/Contracts/IUserService.cs.md) (interface) — `src/EchoHub.Core/Contracts/IUserService.cs`
- [`UserService`](../Code/src/EchoHub.Server/Services/UserService.cs.md) (class) — `src/EchoHub.Server/Services/UserService.cs`
- [`UserOperationResult`](../Code/src/EchoHub.Core/DTOs/CommonDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/CommonDtos.cs`
- *…and 5 more (see symbol-graph.json)*

### src/EchoHub.Server · Program

12 symbols across 11 files. Key symbols (by connectivity):

- [`Program`](../Code/src/EchoHub.Server/Program.cs.md) (file) — `src/EchoHub.Server/Program.cs`
- [`EchoHubDbContext`](../Code/src/EchoHub.Server/Data/EchoHubDbContext.cs.md) (class) — `src/EchoHub.Server/Data/EchoHubDbContext.cs`
- [`HubConstants`](../Code/src/EchoHub.Core/Constants/HubConstants.cs.md) (class) — `src/EchoHub.Core/Constants/HubConstants.cs`
- [`DataMigrationService`](../Code/src/EchoHub.Server/Setup/DataMigrationService.cs.md) (class) — `src/EchoHub.Server/Setup/DataMigrationService.cs`
- [`DatabaseSetup`](../Code/src/EchoHub.Server/Setup/DatabaseSetup.cs.md) (class) — `src/EchoHub.Server/Setup/DatabaseSetup.cs`
- [`DirectoryClaimStore`](../Code/src/EchoHub.Server/Services/DirectoryClaimStore.cs.md) (class) — `src/EchoHub.Server/Services/DirectoryClaimStore.cs`
- [`ImageToAsciiService`](../Code/src/EchoHub.Server/Services/ImageToAsciiService.cs.md) (class) — `src/EchoHub.Server/Services/ImageToAsciiService.cs`
- [`LinkEmbedService`](../Code/src/EchoHub.Server/Services/LinkEmbedService.cs.md) (class) — `src/EchoHub.Server/Services/LinkEmbedService.cs`
- *…and 4 more (see symbol-graph.json)*

### src/EchoHub.Core/DTOs · ApiClient

9 symbols across 5 files. Key symbols (by connectivity):

- [`ApiClient`](../Code/src/EchoHub.Client/Services/ApiClient.cs.md) (class) — `src/EchoHub.Client/Services/ApiClient.cs`
- [`RefreshRequest`](../Code/src/EchoHub.Core/DTOs/AuthDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/AuthDtos.cs`
- [`UpdateProfileRequest`](../Code/src/EchoHub.Core/DTOs/ProfileDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ProfileDtos.cs`
- [`AvatarHelper`](../Code/src/EchoHub.Client/Services/AvatarHelper.cs.md) (class) — `src/EchoHub.Client/Services/AvatarHelper.cs`
- [`LoginRequest`](../Code/src/EchoHub.Core/DTOs/AuthDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/AuthDtos.cs`
- [`RegisterRequest`](../Code/src/EchoHub.Core/DTOs/AuthDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/AuthDtos.cs`
- [`AvatarUploadResponse`](../Code/src/EchoHub.Core/DTOs/ProfileDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ProfileDtos.cs`
- [`EncryptionKeyResponse`](../Code/src/EchoHub.Core/DTOs/ServerDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ServerDtos.cs`
- *…and 1 more (see symbol-graph.json)*

### src/EchoHub.Core/DTOs · ChannelService

8 symbols across 5 files. Key symbols (by connectivity):

- [`ChannelService`](../Code/src/EchoHub.Server/Services/ChannelService.cs.md) (class) — `src/EchoHub.Server/Services/ChannelService.cs`
- [`IChannelService`](../Code/src/EchoHub.Core/Contracts/IChannelService.cs.md) (interface) — `src/EchoHub.Core/Contracts/IChannelService.cs`
- [`ChannelOperationResult`](../Code/src/EchoHub.Core/DTOs/CommonDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/CommonDtos.cs`
- [`FakeChannelService`](../Code/src/EchoHub.Tests/Irc/TestHelpers.cs.md) (class) — `src/EchoHub.Tests/Irc/TestHelpers.cs`
- [`ChannelError`](../Code/src/EchoHub.Core/DTOs/CommonDtos.cs.md) (enum) — `src/EchoHub.Core/DTOs/CommonDtos.cs`
- [`PaginatedResponse`](../Code/src/EchoHub.Core/DTOs/CommonDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/CommonDtos.cs`
- [`ChannelListItem`](../Code/src/EchoHub.Core/Contracts/IChannelService.cs.md) (record) — `src/EchoHub.Core/Contracts/IChannelService.cs`
- [`ChannelMembership`](../Code/src/EchoHub.Core/Models/ChannelMembership.cs.md) (class) — `src/EchoHub.Core/Models/ChannelMembership.cs`

### src/EchoHub.Server · ChannelsController

7 symbols across 5 files. Key symbols (by connectivity):

- [`ChannelsController`](../Code/src/EchoHub.Server/Controllers/ChannelsController.cs.md) (class) — `src/EchoHub.Server/Controllers/ChannelsController.cs`
- [`FileStorageService`](../Code/src/EchoHub.Server/Services/FileStorageService.cs.md) (class) — `src/EchoHub.Server/Services/FileStorageService.cs`
- [`CreateChannelRequest`](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ChatDtos.cs`
- [`SendUrlRequest`](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ChatDtos.cs`
- [`UpdateTopicRequest`](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ChatDtos.cs`
- [`FilesController`](../Code/src/EchoHub.Server/Controllers/FilesController.cs.md) (class) — `src/EchoHub.Server/Controllers/FilesController.cs`
- [`FileValidationHelper`](../Code/src/EchoHub.Server/Services/FileValidationHelper.cs.md) (class) — `src/EchoHub.Server/Services/FileValidationHelper.cs`

### src/EchoHub.Server · ServerDirectoryService

7 symbols across 1 files. Key symbols (by connectivity):

- [`ServerDirectoryService`](../Code/src/EchoHub.Server/Services/ServerDirectoryService.cs.md) (class) — `src/EchoHub.Server/Services/ServerDirectoryService.cs`
- [`ErrorDetail`](../Code/src/EchoHub.Server/Services/ServerDirectoryService.cs.md) (record) — `src/EchoHub.Server/Services/ServerDirectoryService.cs`
- [`Response`](../Code/src/EchoHub.Server/Services/ServerDirectoryService.cs.md) (record) — `src/EchoHub.Server/Services/ServerDirectoryService.cs`
- [`DirectoryProtocol`](../Code/src/EchoHub.Server/Services/ServerDirectoryService.cs.md) (class) — `src/EchoHub.Server/Services/ServerDirectoryService.cs`
- [`DirectoryRegistrationErrors`](../Code/src/EchoHub.Server/Services/ServerDirectoryService.cs.md) (class) — `src/EchoHub.Server/Services/ServerDirectoryService.cs`
- [`RegisterServerDto`](../Code/src/EchoHub.Server/Services/ServerDirectoryService.cs.md) (record) — `src/EchoHub.Server/Services/ServerDirectoryService.cs`
- [`RegisterServerResult`](../Code/src/EchoHub.Server/Services/ServerDirectoryService.cs.md) (record) — `src/EchoHub.Server/Services/ServerDirectoryService.cs`

### src/EchoHub.Client/Services · UpdateBackupService

6 symbols across 4 files. Key symbols (by connectivity):

- [`UpdateBackupService`](../Code/src/EchoHub.Client/Services/UpdateBackupService.cs.md) (class) — `src/EchoHub.Client/Services/UpdateBackupService.cs`
- [`UpdateChecker`](../Code/src/EchoHub.Client/Services/UpdateChecker.cs.md) (class) — `src/EchoHub.Client/Services/UpdateChecker.cs`
- [`BackupInfo`](../Code/src/EchoHub.Client/Services/UpdateBackupService.cs.md) (record) — `src/EchoHub.Client/Services/UpdateBackupService.cs`
- [`BackupJsonContext`](../Code/src/EchoHub.Client/Services/UpdateBackupService.cs.md) (class) — `src/EchoHub.Client/Services/UpdateBackupService.cs`
- [`UpdateConfirmDialog`](../Code/src/EchoHub.Client/UI/Dialogs/UpdateConfirmDialog.cs.md) (class) — `src/EchoHub.Client/UI/Dialogs/UpdateConfirmDialog.cs`
- [`UpdateProgressDialog`](../Code/src/EchoHub.Client/UI/Dialogs/UpdateProgressDialog.cs.md) (class) — `src/EchoHub.Client/UI/Dialogs/UpdateProgressDialog.cs`

### src/EchoHub.Client/UI · UserStatus

6 symbols across 5 files. Key symbols (by connectivity):

- [`UserStatus`](../Code/src/EchoHub.Core/Models/UserStatus.cs.md) (enum) — `src/EchoHub.Core/Models/UserStatus.cs`
- [`StatusDialog`](../Code/src/EchoHub.Client/UI/Dialogs/StatusDialog.cs.md) (class) — `src/EchoHub.Client/UI/Dialogs/StatusDialog.cs`
- [`UserSession`](../Code/src/EchoHub.Client/Services/UserSession.cs.md) (class) — `src/EchoHub.Client/Services/UserSession.cs`
- [`StatusDialogResult`](../Code/src/EchoHub.Client/UI/Dialogs/StatusDialog.cs.md) (record) — `src/EchoHub.Client/UI/Dialogs/StatusDialog.cs`
- [`UserDto`](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ChatDtos.cs`
- [`UpdateStatusRequest`](../Code/src/EchoHub.Core/DTOs/ProfileDtos.cs.md) (record) — `src/EchoHub.Core/DTOs/ProfileDtos.cs`

### src/EchoHub.Client/Themes

5 symbols across 4 files. Key symbols (by connectivity):

- [`Program`](../Code/src/EchoHub.Client/Program.cs.md) (file) — `src/EchoHub.Client/Program.cs`
- [`Theme`](../Code/src/EchoHub.Client/Themes/Theme.cs.md) (class) — `src/EchoHub.Client/Themes/Theme.cs`
- [`ThemeManager`](../Code/src/EchoHub.Client/Themes/ThemeManager.cs.md) (class) — `src/EchoHub.Client/Themes/ThemeManager.cs`
- [`PathSetup`](../Code/src/EchoHub.Client/Services/PathSetup.cs.md) (class) — `src/EchoHub.Client/Services/PathSetup.cs`
- [`ThemeColors`](../Code/src/EchoHub.Client/Themes/Theme.cs.md) (class) — `src/EchoHub.Client/Themes/Theme.cs`

*…and 8 smaller subsystems (see symbol-graph.json).*

## Most connected symbols

The load-bearing symbols — changes here have the widest blast radius.

| Symbol | Kind | Used by | Uses | File |
|---|---|---|---|---|
| [`AppOrchestrator`](../Code/src/EchoHub.Client/AppOrchestrator.cs.md) | class | 3 | 38 | `src/EchoHub.Client/AppOrchestrator.cs` |
| [`ApiClient`](../Code/src/EchoHub.Client/Services/ApiClient.cs.md) | class | 3 | 22 | `src/EchoHub.Client/Services/ApiClient.cs` |
| [`Channel`](../Code/src/EchoHub.Core/Models/Channel.cs.md) | class | 24 | 1 | `src/EchoHub.Core/Models/Channel.cs` |
| [`Program`](../Code/src/EchoHub.Server/Program.cs.md) | file | 0 | 24 | `src/EchoHub.Server/Program.cs` |
| [`Message`](../Code/src/EchoHub.Core/Models/Message.cs.md) | class | 19 | 2 | `src/EchoHub.Core/Models/Message.cs` |
| [`UserStatus`](../Code/src/EchoHub.Core/Models/UserStatus.cs.md) | enum | 21 | 0 | `src/EchoHub.Core/Models/UserStatus.cs` |
| [`User`](../Code/src/EchoHub.Core/Models/User.cs.md) | class | 18 | 2 | `src/EchoHub.Core/Models/User.cs` |
| [`ChannelsController`](../Code/src/EchoHub.Server/Controllers/ChannelsController.cs.md) | class | 0 | 20 | `src/EchoHub.Server/Controllers/ChannelsController.cs` |
| [`ChatService`](../Code/src/EchoHub.Server/Services/ChatService.cs.md) | class | 1 | 19 | `src/EchoHub.Server/Services/ChatService.cs` |
| [`MainWindow`](../Code/src/EchoHub.Client/UI/MainWindow.cs.md) | class | 2 | 16 | `src/EchoHub.Client/UI/MainWindow.cs` |

*Regenerated on every full documentation run; see [Agent/README.md](README.md) for how to use this pack.*