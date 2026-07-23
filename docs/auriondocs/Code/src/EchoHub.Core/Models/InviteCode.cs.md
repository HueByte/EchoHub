# InviteCode

> **File:** `src/EchoHub.Core/Models/InviteCode.cs`  
> **Kind:** class

```csharp
public class InviteCode
```


A data model representing a single registration invitation. When the server is configured with `Server:Registration = "invite"`, new accounts (REST and IRC alike) must present a valid, unexpired, and not-fully-used code to register. The `InviteCode` tracks the invitation's identity and policy: the persistent identifier `Id`, the required invitation value `Code` (marked `required` in the model), who created it (`CreatedByUserId` and `CreatedByUsername`), and when it was created (`CreatedAt`). The invitation may expire via `ExpiresAt` (null meaning it never expires), and its usage is controlled by `MaxUses` with current usage stored in `UseCount`. By default, a new invite is single-use (`MaxUses` = 1) and `CreatedAt` is initialized to the current UTC moment. This class is intended to be stored and consulted by the registration workflow to enforce invite-based onboarding.