# ChannelService

> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** class

*Figure: How ChannelService works.*

```mermaid
%%{init: {'theme':'base','themeVariables':{'background':'#faf7ef','primaryColor':'#f0e2c2','primaryTextColor':'#1f2840','primaryBorderColor':'#8a7548','secondaryColor':'#d9efec','secondaryBorderColor':'#1d8a80','secondaryTextColor':'#1f2840','tertiaryColor':'#f2ebd8','tertiaryBorderColor':'#8a7548','tertiaryTextColor':'#1f2840','lineColor':'#1d8a80','titleColor':'#1f2840','fontSize':'14px','edgeLabelBackground':'#faf7ef','clusterBkg':'#f2ebd8','clusterBorder':'#8a7548','actorBkg':'#f0e2c2','actorBorder':'#8a7548','actorTextColor':'#1f2840','actorLineColor':'#8a7548','signalColor':'#1d8a80','signalTextColor':'#1f2840','activationBkgColor':'#d9efec','activationBorderColor':'#1d8a80','noteBkgColor':'#f2ebd8','noteBorderColor':'#8a7548','noteTextColor':'#1f2840','labelBoxBkgColor':'#f0e2c2','labelBoxBorderColor':'#8a7548','labelTextColor':'#1f2840','transitionColor':'#1d8a80','transitionLabelColor':'#1f2840','stateLabelColor':'#1f2840','altBackground':'#f2ebd8'}}}%%
flowchart TB
ChannelService["ChannelService: CreateChannelAsync(creatorUserId, name, topic, isPublic)"]
ValidationConstants["Validate name: if blank -> Fail ValidationFailed ('Channel name is required'); else normalize and test ValidationConstants.ChannelNameRegex()"]
ChannelError["Return ChannelOperationResult.Fail(..., ChannelError.*)"]
EchoHubDbContext["Create scope; get EchoHubDbContext; check db.Channels.Any(c => c.Name == channelName)"]
Channel["Instantiate Channel entity and db.Channels.Add(channel)"]
ChannelMembership["Add ChannelMembership for creator (db.ChannelMemberships.Add)"]
ChannelDto["Create ChannelDto(dto)"]
ChannelOperationResult["Return ChannelOperationResult.Success(dto)"]

ChannelService --> ValidationConstants
ValidationConstants -->|invalid name| ChannelError
ValidationConstants -->|regex invalid| ChannelError
ValidationConstants -->|valid| EchoHubDbContext
EchoHubDbContext -->|exists| ChannelError
EchoHubDbContext -->|not exists| Channel
Channel --> ChannelMembership
ChannelMembership --> EchoHubDbContext
EchoHubDbContext --> ChannelDto
ChannelDto --> ChannelOperationResult
```

```csharp
public class ChannelService : IChannelService
```


Provides channel-management operations used by higher-level features (and by IChannelService callers) such as listing channels visible to a user, creating channels, updating topics, deleting channels and ensuring membership. Use this service when you need the server-side business rules and persistence logic for channels instead of accessing the DbContext directly.

## Remarks
ChannelService is a thin business layer that enforces validation and authorization rules and mediates all database access for channels. Each public method creates a short-lived IServiceScope and resolves EchoHubDbContext from it so the service itself can be registered with a longer lifetime (for example as a singleton) while still using a scoped DbContext per operation. It also delegates presence-related concerns to the PresenceTracker and emits structured logs via `ILogger<ChannelService>`.

## Notes
- Channel names are normalized to lowercase and trimmed before validation and persistence; callers should treat channel names as case-insensitive.
- Creation enforces a regex-based name policy and rejects empty or whitespace names. Topic text has a maximum length check (ValidationConstants.MaxChannelTopicLength).
- The creator of a channel is automatically added as a ChannelMembership at creation time; the CreatedByUserId is used to enforce who may update the topic (only the creator can change it).
- GetChannelsAsync calls EnsureDefaultChannelAsync before listing channels to guarantee the existence of the default channel; callers looking for the default channel can rely on it being present after this method completes.
- CreateChannelAsync checks existence with AnyAsync before inserting, but concurrent callers could still race; the database should enforce a unique constraint on channel name to guarantee correctness under concurrent creates.
- Each operation uses a fresh IServiceScope/DbContext (using-var ensures disposal). This avoids sharing a DbContext across calls but means ambient transaction behavior does not cross method boundaries.