# IUserService

> **File:** `src/EchoHub.Core/Contracts/IUserService.cs`  
> **Kind:** interface

```csharp
public interface IUserService
```


IUserService defines an asynchronous contract for user-related operations in EchoHub. It exposes methods to register and authenticate users, retrieve user profiles by username or by id, update profile fields, and set an avatar using ASCII art. Implementations act as a boundary between higher-level application logic and underlying data stores or external services, returning a UserOperationResult to indicate success or failure for operations that mutate state. Call sites typically await these tasks and branch based on IsSuccess to handle success or error cases.

## Remarks
This interface abstracts user-centric use cases behind a small, testable surface, enabling swapping implementations (e.g., in tests or multiple environments) without touching callers. The nullable parameters on UpdateProfileAsync imply optional updates; callers can pass null to leave fields unchanged, though exact semantics are application-defined. GetUserProfileAsync and GetUserByIdAsync return possibly null results, signaling that the requested user profile might not exist. UserOperationResult is the common result wrapper used by mutating operations to indicate success or failure and carry a resulting user profile when appropriate.

## Notes
- Nullability on profile-fetch methods means callers must handle nulls.
- Mutating methods return UserOperationResult; always check IsSuccess before relying on returned data.