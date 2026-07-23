# EchoHubDbContext

> **File:** `src/EchoHub.Server/Data/EchoHubDbContext.cs`  
> **Kind:** class

```csharp
public class EchoHubDbContext : DbContext
```


EchoHubDbContext serves as the EF Core persistence gateway for EchoHub's domain model. It exposes `DbSet<User>`, `DbSet<Channel>`, `DbSet<Message>`, `DbSet<Attachment>`, `DbSet<RefreshToken>`, `DbSet<ChannelMembership>`, `DbSet<InviteCode>`, and `DbSet<ServerStatsReport>`, enabling queries and updates against the underlying SQLite store. When not configured by the application host, it configures a file-based SQLite database named echohub.db located under the application base directory, providing a simple local data store for development and testing. In OnModelCreating it enforces domain rules through keys, indices, field length constraints, and relationship mappings (for example, a Channel has many Messages; a Message has many Attachments; ChannelMembership uses a composite key of UserId and ChannelId), ensuring data integrity and cascade behaviors across related entities.