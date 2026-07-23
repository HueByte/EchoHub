# EchoHubDbContext

> **File:** `src/EchoHub.Server/Data/EchoHubDbContext.cs`  
> **Kind:** class

```csharp
public class EchoHubDbContext : DbContext
```


EchoHubDbContext is the EF Core `DbContext` that exposes the EchoHub data model to the database. It provides `DbSet` properties for core aggregates such as [`User`](../../EchoHub.Core/Models/User.cs.md), [`Channel`](../../EchoHub.Core/Models/Channel.cs.md), [`Message`](../../EchoHub.Core/Models/Message.cs.md), [`Attachment`](../../EchoHub.Core/Models/Attachment.cs.md), `RefreshToken`, [`ChannelMembership`](../../EchoHub.Core/Models/ChannelMembership.cs.md), [`InviteCode`](../../EchoHub.Core/Models/InviteCode.cs.md), and [`ServerStatsReport`](../../EchoHub.Core/Models/ServerStatsReport.cs.md), enabling typed queries and persistence throughout the application. When the context is not configured by the host, `OnConfiguring` automatically wires up a SQLite database file named `echohub.db` in the application's base directory via `AppContext.BaseDirectory` and the `UseSqlite` provider.

## Remarks
`EchoHubDbContext` centralizes data access for the domain, acting as the bridge between in-memory entities and their persisted representations. The `OnModelCreating` configuration defines keys, unique constraints, indices, and relationships that enforce data integrity and shape the underlying schema: [`User`](../../EchoHub.Core/Models/User.cs.md) enforces a unique `Username` with length limits; [`Channel`](../../EchoHub.Core/Models/Channel.cs.md) and [`ChannelMembership`](../../EchoHub.Core/Models/ChannelMembership.cs.md) establish channel scopes and membership rules; [`Message`](../../EchoHub.Core/Models/Message.cs.md) and [`Attachment`](../../EchoHub.Core/Models/Attachment.cs.md) model the document and media relationships with cascade deletes to maintain referential integrity; and [`ServerStatsReport`](../../EchoHub.Core/Models/ServerStatsReport.cs.md) records runtime metrics. This design keeps persistence concerns isolated from business logic while ensuring consistent, queryable access to all EchoHub data.

## Dependencies
- `DbContext`
- [`User`](../../EchoHub.Core/Models/User.cs.md)
- [`Channel`](../../EchoHub.Core/Models/Channel.cs.md)
- [`Message`](../../EchoHub.Core/Models/Message.cs.md)
- [`Attachment`](../../EchoHub.Core/Models/Attachment.cs.md)
- `RefreshToken`
- [`ChannelMembership`](../../EchoHub.Core/Models/ChannelMembership.cs.md)
- [`InviteCode`](../../EchoHub.Core/Models/InviteCode.cs.md)
- [`ServerStatsReport`](../../EchoHub.Core/Models/ServerStatsReport.cs.md)

## Dependency APIs
- property [`User`](../../EchoHub.Core/Models/User.cs.md) (`src/EchoHub.Core/Models/RefreshToken.cs`)
- class [`Channel`](../../EchoHub.Core/Models/Channel.cs.md) (`src/EchoHub.Core/Models/Channel.cs`)
  - property `Guid Id`
  - property `string Name`
  - property `string? Topic`
  - property `bool IsPublic`
  - property `bool IsSystem`
  - property `string? PasswordHash`
  - property `string? EncryptionSalt`
  - property `string? WrappedRoomKey`
  - property `DateTimeOffset CreatedAt`
  - property `Guid CreatedByUserId`
  - property `List<Message> Messages`
- property [`Message`](../../EchoHub.Core/Models/Message.cs.md) (`src/EchoHub.Core/Models/Attachment.cs`)
- class [`Attachment`](../../EchoHub.Core/Models/Attachment.cs.md) (`src/EchoHub.Core/Models/Attachment.cs`)
  - property `Guid Id`
  - property `Guid MessageId`
  - property `Message? Message`
  - property `AttachmentKind Kind`
  - property `string Url`
  - property `string FileName`
  - property `long FileSize`
  - property `string? AsciiPreview`
- property `RefreshToken` (`src/EchoHub.Client/Config/ClientConfig.cs`)
- class [`ChannelMembership`](../../EchoHub.Core/Models/ChannelMembership.cs.md) (`src/EchoHub.Core/Models/ChannelMembership.cs`)
  - property `Guid UserId`
  - property `Guid ChannelId`
  - property `DateTimeOffset JoinedAt`
- class [`InviteCode`](../../EchoHub.Core/Models/InviteCode.cs.md) (`src/EchoHub.Core/Models/InviteCode.cs`)
  - property `Guid Id`
  - property `string Code`
  - property `Guid CreatedByUserId`
  - property `string CreatedByUsername`
  - property `DateTimeOffset CreatedAt`
  - property `DateTimeOffset? ExpiresAt`
  - property `int MaxUses`
  - property `int UseCount`
- class [`ServerStatsReport`](../../EchoHub.Core/Models/ServerStatsReport.cs.md) (`src/EchoHub.Core/Models/ServerStatsReport.cs`)
  - property `Guid Id`
  - property `DateTimeOffset GeneratedAt`
  - property `DateTimeOffset PeriodStart`
  - property `DateTimeOffset PeriodEnd`
  - property `double WindowHours`
  - property `int MessagesSent`
  - property `int FilesUploaded`
  - property `long BytesUploaded`
  - property `int NewMembers`
  - property `int ActiveMembers`
  - property `int Connections`
  - property `int Disconnections`
  - …and 5 more member(s) not shown

## Notes
- The `EchoHubDbContext` relies on SQLite as the backing store when not configured externally; ensure the application process has write access to the base directory where `echohub.db` is created.