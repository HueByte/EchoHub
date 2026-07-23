# InviteCode

> **File:** `src/EchoHub.Core/Models/InviteCode.cs`  
> **Kind:** class

```csharp
public class InviteCode
```


Represents a registration invitation code used to gate account creation when the server's registration mode is set to invite. An InviteCode captures the unique identifier, the actual code string, who created it, and when it was created, plus optional expiration and per-invite usage constraints. When a new REST or IRC account is created and the system is configured for invite-based registration, the incoming code must match an existing InviteCode that has not expired and that has remaining uses.

## Remarks
InviteCode acts as a persistence-side contract for invitation-based onboarding. It separates the concerns of registration gating from user data and provides a straightforward way to enforce expiration and single-use or limited-use policies at the data layer. The server's registration flow should consult these properties to validate a code before creating a new account and to record each use via UseCount, potentially preventing additional uses after MaxUses is reached.

## Example
```csharp
// Example usage: initialize a new invite code that will expire in 7 days and allow up to 5 uses
Guid adminUserId = Guid.NewGuid();
var invite = new InviteCode
{
    Id = Guid.NewGuid(),
    Code = "INVITE-2026-ACME",
    CreatedByUserId = adminUserId,
    CreatedByUsername = "admin",
    CreatedAt = DateTimeOffset.UtcNow,
    ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
    MaxUses = 5,
    UseCount = 0
};
```

## Notes
- Use of 'required' Code property ensures that a code value is provided when constructing instances; compile-time enforcement.
- ExpiresAt null means never expires; If ExpiresAt is not set, the code is perpetual.
- The class does not implement persistence or concurrency control; UseCount and MaxUses must be enforced by the application or data layer.