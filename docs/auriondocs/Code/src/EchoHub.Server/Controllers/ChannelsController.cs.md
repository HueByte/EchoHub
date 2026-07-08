# ChannelsController

> **File:** `src/EchoHub.Server/Controllers/ChannelsController.cs`  
> **Kind:** class

*Figure: How ChannelsController works.*

```mermaid
%%{init: {'theme':'base','themeVariables':{'background':'#faf7ef','primaryColor':'#f0e2c2','primaryTextColor':'#1f2840','primaryBorderColor':'#8a7548','secondaryColor':'#d9efec','secondaryBorderColor':'#1d8a80','secondaryTextColor':'#1f2840','tertiaryColor':'#f2ebd8','tertiaryBorderColor':'#8a7548','tertiaryTextColor':'#1f2840','lineColor':'#1d8a80','titleColor':'#1f2840','fontSize':'14px','edgeLabelBackground':'#faf7ef','clusterBkg':'#f2ebd8','clusterBorder':'#8a7548','actorBkg':'#f0e2c2','actorBorder':'#8a7548','actorTextColor':'#1f2840','actorLineColor':'#8a7548','signalColor':'#1d8a80','signalTextColor':'#1f2840','activationBkgColor':'#d9efec','activationBorderColor':'#1d8a80','noteBkgColor':'#f2ebd8','noteBorderColor':'#8a7548','noteTextColor':'#1f2840','labelBoxBkgColor':'#f0e2c2','labelBoxBorderColor':'#8a7548','labelTextColor':'#1f2840','transitionColor':'#1d8a80','transitionLabelColor':'#1f2840','stateLabelColor':'#1f2840','altBackground':'#f2ebd8'}}}%%
flowchart TB
A["User: find NameIdentifier claim (User)"]
B{"Is userIdClaim null?"}
C["Return 401 Unauthorized (ErrorResponse)"]
D{"Endpoint: GET / POST / PUT"}
A --> B
B -- "yes" --> C
B -- "no" --> D
D -- "GET" --> G
D -- "POST" --> H
D -- "PUT" --> M
G["GetChannels: clamp offset/limit and call IChannelService.GetChannelsAsync(userId, offset, limit)"]
G --> E["Return 200 Ok(result)"]
H["CreateChannel: bind CreateChannelRequest and call IChannelService.CreateChannelAsync(...) -> ChannelOperationResult"]
H --> H1["Call IChannelService.CreateChannelAsync(Guid(userId), request.Name, request.Topic, request.IsPublic) -> ChannelOperationResult"]
H1 --> I{"ChannelOperationResult.IsSuccess?"}
I -- "no" --> J["MapChannelError(ChannelOperationResult) -> return ChannelError"]
I -- "yes" --> K["If result.Channel.IsPublic then IChatService.BroadcastChannelUpdatedAsync(result.Channel)"]
K --> L["Return 201 Created('/api/channels/{result.Channel.Name}', result.Channel)"]
M["UpdateTopic: bind UpdateTopicRequest and call IChannelService.UpdateTopicAsync(Guid(userId), channel, request.Topic) -> ChannelOperationResult"]
M --> M1["Call IChannelService.UpdateTopicAsync(Guid(userId), channel, request.Topic) -> ChannelOperationResult"]
M1 --> N{"ChannelOperationResult.IsSuccess?"}
N -- "no" --> J
N -- "yes" --> O["IChatService.BroadcastChannelUpdatedAsync(result.Channel, channel.ToLowerInvariant().Trim())"]
O --> P["Return 200 Ok(result.Channel)"]
J --> Q["Return mapped error response (ChannelError)"]
```

```csharp
[ApiController]
[Route("api/channels")]
[Authorize]
[EnableRateLimiting("general")]
public class ChannelsController : ControllerBase
```


Exposes HTTP endpoints for listing, creating, updating, deleting and uploading content to channels; intended to be used by authenticated clients and to centralize request validation, rate-limiting and broadcasting of public-channel changes rather than letting callers interact with the lower-level services directly.

## Remarks
This controller is an API surface that orchestrates channel-related operations and delegates business logic to injected services (IChannelService for channel CRUD, IChatService for broadcasting updates, plus several helpers for storage, image processing and encryption). It enforces authentication and applies rate-limiting attributes at the controller and action level, and it performs basic input normalization/validation (for example clamping paging parameters and normalizing channel names when broadcasting updates).

## Notes
- All endpoints require an authenticated user: the controller reads ClaimTypes.NameIdentifier and returns Unauthorized(ErrorResponse) if the claim is missing.
- The GetChannels endpoint enforces paging constraints (offset >= 0, limit clamped to 1..100) — clients should expect server-side truncation of requested limits.
- File upload endpoint(s) are subject to additional rate-limiting and request size limits (RequestSizeLimit and RequestFormLimits referencing HubConstants.MaxFileSizeBytes); clients must respect these limits to avoid rejected requests.