# IUserService

> **File:** `src/EchoHub.Core/Contracts/IUserService.cs`  
> **Kind:** interface

```csharp
public interface IUserService
```


`IUserService` is the asynchronous contract for common user-account operations: registration, authentication, and profile access. Implementations may back these calls with REST, an IRC gateway, or other transports, but callers interact with this interface to perform login, account creation, and profile queries without coupling to a specific transport.

## Remarks

By returning `Task<UserOperationResult>` for mutating operations and `Task<UserProfileDto?>` for profile queries, the interface cleanly models success/failure and optional data. The [`UserOperationResult`](../DTOs/CommonDtos.cs.md) type provides `Success(UserProfileDto user)` and `Fail(UserError error, string message)` helpers, enabling implementations to construct consistent outcomes. The `RegisterUserAsync` method carries a server-policies cue in its comment: when `Server:Registration = "invite"`, an `inviteCode` is required; in `"closed"` mode, new accounts are refused. This centralizes registration policy at the service boundary and avoids scattering policy checks across call sites.

## Example

```csharp
// Example usage of IUserService
public async Task DemoAsync(IUserService userService)
{
    var reg = await userService.RegisterUserAsync("alice", "Secret123", inviteCode: "INVITE-42");
    if (reg.IsSuccess)
    {
        var profile = await userService.GetUserProfileAsync("alice");
        // Use profile as needed
    }
}
```

## Notes

- The `inviteCode` parameter is context-sensitive and should be supplied when the server is configured with `Server:Registration = "invite"`; otherwise it may be omitted.
- All methods are asynchronous; callers should `await` the results and branch on `UserOperationResult.IsSuccess` as appropriate. 
- [`GetUserProfileAsync`](../../EchoHub.Client/Services/ApiClient.cs.md) and `GetUserByIdAsync` return `UserProfileDto?`, reflecting the possibility that a user profile may not be found or accessible in certain contexts.
