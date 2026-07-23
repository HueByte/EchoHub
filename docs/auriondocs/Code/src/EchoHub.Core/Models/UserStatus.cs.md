# UserStatus

> **File:** `src/EchoHub.Core/Models/UserStatus.cs`  
> **Kind:** enum

```csharp
public enum UserStatus
{
    Online,
    Away,
    DoNotDisturb,
    Invisible
}
```


Represents the current presence state of a user in EchoHub, used by UI presence indicators and presence logic throughout the app. Use Online when the user is connected and active, Away when the user is idle, DoNotDisturb to signal notifications should be minimized, and Invisible when the user should not appear online to others.