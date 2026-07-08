# EchoHubDbContext

> **File:** `src/EchoHub.Server/Data/EchoHubDbContext.cs`  
> **Kind:** class

```csharp
public class EchoHubDbContext : DbContext
```


EchoHubDbContext is the Entity Framework Core DbContext that coordinates the EchoHub data model for the server, exposing `DbSet<User>`, `DbSet<Channel>`, `DbSet<Message>`, `DbSet<RefreshToken>`, and `DbSet<ChannelMembership>`. It encapsulates the persistence configuration and entity mappings, including unique constraints, relationships, and field lengths, so domain models remain clean POCOs while EF handles storage concerns. By default, it targets a local SQLite database named echohub.db located in the application base directory when not configured externally, and it implements a DateTimeOffset storage strategy to accommodate SQLite limitations.

## Remarks
The DbContext acts as the central persistence boundary for the EchoHub domain, wiring entity relationships (users, channels, messages, memberships, and tokens) and enforcing invariants at the data layer (e.g., unique usernames, unique channel names, and cascade deletions). It also centralizes provider-specific concerns, such as the SQLite path and the DateTimeOffset conversion strategy, ensuring consistent behavior across the data model.

## Notes
- Unique constraints: usernames (Users) and channel names (Channels) are unique, preventing duplicates at the database level. 
- Relationships and cascade deletes: Channel has many Messages; ChannelMembership ties Users to Channels with cascade delete behavior on both sides, so removing a Channel or a User cleans up related records.
- DateTimeOffset handling: a ValueConverter is prepared to convert DateTimeOffset values to Unix milliseconds for storage, addressing SQLite's limitations with DateTimeOffset in ORDER BY clauses.
- Configuration guard: OnConfiguring only applies the SQLite data source when options are not already configured (allowing external configuration via DI or test setups).
