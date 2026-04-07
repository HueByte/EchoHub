# Authentication

## User Registration

A new user creates an account on a server. The client sends credentials via REST,
the server hashes the password, issues JWT tokens, and the client stores the
refresh token for "Remember Me" sessions.

```mermaid
sequenceDiagram
    participant UI as ConnectDialog
    participant AO as AppOrchestrator
    participant CM as ConnectionManager
    participant API as ApiClient
    participant Auth as AuthController
    participant US as UserService
    participant JWT as JwtTokenService
    participant DB as SQLite

    UI->>AO: ConnectDialogResult(IsRegister: true)
    AO->>CM: ConnectAsync(dialogResult)
    CM->>API: RegisterAsync(username, password)
    API->>Auth: POST /api/auth/register
    Auth->>US: RegisterUserAsync(username, password, displayName)
    US->>US: Validate (regex, length, uniqueness)
    US->>DB: INSERT User (BCrypt hash)
    US-->>Auth: UserOperationResult.Success
    Auth->>JWT: GenerateAccessToken(user)
    JWT-->>Auth: (token, expiresAt) [15 min]
    Auth->>JWT: GenerateRefreshToken()
    JWT-->>Auth: Base64 random (64 bytes)
    Auth->>DB: INSERT RefreshToken (SHA256 hash)
    Auth-->>API: LoginResponse
    API->>API: SetTokens() — store in memory + set Bearer header
    API-->>CM: LoginResponse
    CM->>CM: Wire OnTokensRefreshed for config persistence
    CM->>CM: Continue to connection setup (see Connection Flow)
```

---

## User Login

Returning user authenticates with username/password or a saved refresh token.

```mermaid
sequenceDiagram
    participant UI as ConnectDialog
    participant CM as ConnectionManager
    participant API as ApiClient
    participant Auth as AuthController
    participant US as UserService
    participant DB as SQLite

    alt Saved refresh token (Remember Me)
        UI->>CM: ConnectDialogResult(SavedRefreshToken: "...")
        CM->>API: LoginWithRefreshTokenAsync()
        API->>Auth: POST /api/auth/refresh
        Auth->>DB: Lookup token by SHA256 hash
        Auth->>DB: Revoke old token, issue new pair
        Auth-->>API: LoginResponse (rotated tokens)
    else Username + Password
        UI->>CM: ConnectDialogResult(IsRegister: false)
        CM->>API: LoginAsync(username, password)
        API->>Auth: POST /api/auth/login
        Auth->>US: AuthenticateUserAsync(username, password)
        US->>DB: Fetch user, BCrypt.Verify(password, hash)
        US->>DB: Update LastSeenAt
        US-->>Auth: UserOperationResult.Success
        Auth-->>API: LoginResponse
    end
    API->>API: SetTokens()
```

---

## Token Refresh

Access tokens expire after 15 minutes. The client auto-refreshes transparently
before requests and on 401 responses. Refresh tokens are rotated on each use.

```mermaid
sequenceDiagram
    participant SR as SignalR / HTTP Request
    participant API as ApiClient
    participant Auth as AuthController
    participant DB as SQLite
    participant Config as config.json

    SR->>API: GetValidTokenAsync() or HTTP 401
    API->>API: Token expires within 60s?
    alt Proactive refresh (SignalR token provider)
        API->>Auth: POST /api/auth/refresh (old refresh token)
    else Reactive refresh (HTTP 401 retry)
        API->>Auth: POST /api/auth/refresh (old refresh token)
    end
    Auth->>DB: Lookup by SHA256 hash
    Auth->>DB: Revoke old refresh token
    Auth->>DB: INSERT new RefreshToken
    Auth-->>API: LoginResponse (new token pair)
    API->>API: SetTokens() — update Bearer header
    API-->>API: Fire OnTokensRefreshed event
    API-->>Config: Persist new refresh token (if Remember Me)
    API->>SR: Retry original request with new token
```
