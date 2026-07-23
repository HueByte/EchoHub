# IUserService

> **File:** `src/EchoHub.Core/Contracts/IUserService.cs`  
> **Kind:** interface

```csharp
public interface IUserService
```


IUserService defines a contract for asynchronous user-management operations within EchoHub.Core. It exposes methods to register and authenticate users, retrieve profiles by username or by ID, update profile details, and set a user's avatar. Implementations of this interface serve as the single logical boundary for user lifecycle concerns, allowing REST endpoints and the IRC gateway to funnel through a consistent surface and enabling easier testing and swapping of storage or identity providers. The RegisterUserAsync method acknowledges server configuration: when Server:Registration is set to "invite", an inviteCode is required; when set to "closed", new accounts are rejected; all such flows funnel through this service.

## Remarks
By centralizing these operations behind IUserService, the rest of the system depends on a stable, testable contract rather than concrete data stores or authentication mechanisms. It coordinates with the UserOperationResult wrapper to communicate success or failure and, for retrieval operations, to surface user data returned on success, keeping error handling consistent across the application.

## Example
```csharp
// Example usage of the IUserService contract
var result = await userService.RegisterUserAsync("alice", "P@ssw0rd", displayName: "Alice", inviteCode: "INV-123");
if (result.IsSuccess)
{
    // registration succeeded; you can proceed with login or profile fetch
}
```

```csharp
var profile = await userService.GetUserProfileAsync("alice");
if (profile != null)
{
    // use profile data
}
```

## Notes
- If you call UpdateProfileAsync with all arguments as null, the operation may be a no-op; only pass the fields you intend to update.
- GetUserByIdAsync returns a UserProfileDto?; handle the null case when the user does not exist.
- For registration, ensure your server's registration policy (invite vs closed) is aligned with your inviteCode usage; otherwise registration may fail.