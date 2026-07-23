# ApiClient.cs

> **Source:** `src/EchoHub.Client/Services/ApiClient.cs`

## Contents

- [ApiClient](#apiclient)
  - [ApiClient (constructor)](#apiclient-constructor)
  - [BaseUrl](#baseurl)
  - [RefreshToken](#refreshtoken)
  - [Token](#token)
  - [AssignRoleAsync](#assignroleasync)
  - [AuthenticatedGetAsync](#authenticatedgetasync)
  - [AuthenticatedRequestAsync](#authenticatedrequestasync)
  - [BanUserAsync](#banuserasync)
  - [CreateChannelAsync](#createchannelasync)
  - [CreateInviteAsync](#createinviteasync)
  - [DeleteChannelAsync](#deletechannelasync)
  - [DeleteMessageAsync](#deletemessageasync)
  - [DeleteMyAccountAsync](#deletemyaccountasync)
  - [Dispose](#dispose)
  - [DownloadFileToTempAsync](#downloadfiletotempasync)
  - [EnsureAuthenticated](#ensureauthenticated)
  - [EnsureSuccessAsync](#ensuresuccessasync)
  - [ExportMyDataAsync](#exportmydataasync)
  - [GetChannelCryptoAsync](#getchannelcryptoasync)
  - [GetChannelMetaAsync](#getchannelmetaasync)
  - [GetChannelsAsync](#getchannelsasync)
  - [GetContentType](#getcontenttype)
  - [GetEncryptionKeyAsync](#getencryptionkeyasync)
  - [GetInvitesAsync](#getinvitesasync)
  - [GetServerInfoAsync](#getserverinfoasync)
  - [GetUserProfileAsync](#getuserprofileasync)
  - [GetValidTokenAsync](#getvalidtokenasync)
  - [KickUserAsync](#kickuserasync)
  - [LoginAsync](#loginasync)
  - [LoginWithRefreshTokenAsync](#loginwithrefreshtokenasync)
  - [LogoutAsync](#logoutasync)
  - [MuteUserAsync](#muteuserasync)
  - [NukeChannelAsync](#nukechannelasync)
  - [RefreshTokenAsync](#refreshtokenasync)
  - [RegisterAsync](#registerasync)
  - [RekeyChannelAsync](#rekeychannelasync)
  - [RevokeInviteAsync](#revokeinviteasync)
  - [SendMessageWithAttachmentsAsync](#sendmessagewithattachmentsasync)
  - [SetTokens](#settokens)
  - [UnbanUserAsync](#unbanuserasync)
  - [UnmuteUserAsync](#unmuteuserasync)
  - [UpdateChannelTopicAsync](#updatechanneltopicasync)
  - [UploadAvatarAsync](#uploadavatarasync)
- [SendUrlAsync](#sendurlasync)
- [UpdateProfileAsync](#updateprofileasync)

---

## ApiClient
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** class

```csharp
public sealed class ApiClient : IDisposable
```


A high-level HTTP client for EchoHub that centralizes authentication (login, refresh, logout) and common server operations (channels, messages, uploads, invites, profile). Use ApiClient when you want a single, in-memory service to manage access/refresh tokens, provide a token for long-lived connections (SignalR), and call the server's REST endpoints through convenient async methods.

## Remarks
This class wraps an HttpClient and keeps the current access and refresh tokens in memory, exposing them via the Token and RefreshToken properties and notifying consumers through the OnTokensRefreshed event. GetValidTokenAsync is intended as the token provider for long-lived connections and will proactively refresh an access token that is near expiry (the implementation refreshes if the token expires within about 60 seconds). Methods that return DTOs commonly use nullable results to indicate "not found" or absence.

## Example
```csharp
// Create the client and log in
using var api = new ApiClient("https://api.example.com");
var login = await api.LoginAsync("alice", "s3cret");

// Persist tokens or react when they've been refreshed
api.OnTokensRefreshed += () =>
{
    var latestAccess = api.Token;
    var latestRefresh = api.RefreshToken;
    // Save to secure storage if needed
};

// Use the token provider for a SignalR connection or call APIs that need auth
var tokenForSignalR = await api.GetValidTokenAsync();
var channels = await api.GetChannelsAsync();
```

## Notes
- GetValidTokenAsync may refresh the token when it will expire within ~60 seconds; callers should still handle server-side authorization failures.
- LogoutAsync is implemented as "best-effort" — it may not always revoke server-side state; clear persisted tokens yourself if you saved them.
- Tokens are stored only in memory by this client. To persist them across restarts, subscribe to OnTokensRefreshed and save Token/RefreshToken externally.

---

### ApiClient (constructor)
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** constructor

```csharp
public ApiClient(string baseUrl)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `baseUrl` | `string` | — |


Initializes an ApiClient with a base URL, normalizes the URL by removing any trailing slash, and configures an HttpClient whose BaseAddress is that normalized URL. This ensures that subsequent requests can be issued with relative URIs against the configured API host.

## Remarks
This constructor centralizes HTTP client initialization for the API client, enforcing a consistent base address across calls. By normalizing the base URL, it prevents subtle URI mistakes when composing requests. It encapsulates the HttpClient setup so callers don’t have to manage base addresses or client lifetimes directly.

## Example
```csharp
var client = new ApiClient("https://api.example.com/");
```

## Notes
- baseUrl must be non-null; passing null will throw a NullReferenceException at TrimEnd('/').
- If baseUrl is not a well-formed absolute URL, creating new Uri(BaseUrl) will throw UriFormatException.


---

### BaseUrl
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** property

```csharp
public string BaseUrl
```


BaseUrl is a read-only string property that exposes the root URL the ApiClient uses to construct request URIs. It provides a single source of truth for the API endpoint so callers can reason about where requests are sent without mutating the value at runtime.

## Remarks
BaseUrl serves as the canonical root for all REST calls performed by the ApiClient. Centralizing the endpoint simplifies testing across environments (dev, staging, prod) because the rest of the client can build URIs without caring about the actual host. Since it is read-only, the value is established during construction; to change environments you create a new client instance with a different BaseUrl. It integrates with the request pipeline by being the starting point for path composition.

## Notes
- Value is set in constructor; to change it, instantiate new ApiClient with a different BaseUrl.
- When combining with path segments, avoid manual string concatenation; prefer proper Uri or relative path builders to prevent double slashes.

---

### RefreshToken
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** property

```csharp
public string? RefreshToken => _refreshToken
```


The RefreshToken property is a read-only accessor that returns the current value of the private _refreshToken field. Because it is nullable, callers should be prepared for a null value when no token is stored or when the token has been cleared.

## Remarks
The property provides a lightweight way to observe or forward the stored refresh token without permitting mutation. It reinforces centralized token management by keeping updates to _refreshToken out of the consumer's hands, while still enabling advanced scenarios such as diagnostics or token-forwarding workflows that require the current token value.

## Notes
- The value may be null; always account for a possible null return in calling code. 
- Do not log or transmit the token in telemetry or UI streams; treat it as sensitive information and guard its exposure.


---

### Token
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** property

```csharp
public string? Token => _accessToken
```


Exposes the current access token as a read-only, nullable string. This property simply forwards the value stored in the private backing field _accessToken, providing a stable API surface for callers that need to read the token without mutating the field. Since the value can be null when no token is present, callers should handle null gracefully, for example by guarding token-dependent logic or by omitting the token from headers when it is absent.

## Remarks
This property acts as a stable abstraction over the token storage, decoupling callers from how and where the token is stored. It allows the rest of the API client to evolve its token management (for example, refreshing tokens or lazy loading) without affecting callers that just need to read the current value. It also signals that token retrieval is a side-effect-free operation, reinforcing a read-only contract for consumers.

---

### AssignRoleAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task AssignRoleAsync(string username, ServerRole role)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string` | — |
| `role` | [`ServerRole`](../../EchoHub.Core/Models/ServerRole.cs.md) | — |

**Returns:** `Task`


AssignRoleAsync assigns a server-side role to a user by issuing an authenticated POST request to the moderation API. It builds an AssignRoleRequest from the provided username and role and posts it to /api/moderation/role. The call is preceded by EnsureAuthenticated() and followed by EnsureSuccessAsync(response), so authentication is enforced and failures are surfaced as exceptions. The method is asynchronous and returns a Task that completes when the server confirms success.

---

### AuthenticatedGetAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
private async Task<HttpResponseMessage> AuthenticatedGetAsync(string url)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `url` | `string` | — |

**Returns:** `Task<HttpResponseMessage>`


Performs a GET request against the provided URL and, if the response is 401 Unauthorized and a non-empty refresh token is available, automatically refreshes the token and retries the request. The original response is disposed before replacing it with the retried response. The caller is responsible for disposing the final HttpResponseMessage returned by this method. If the token refresh fails, the original response is returned unchanged.

## Remarks
By centralizing this refresh-and-retry logic, the API client avoids duplicating authentication-handling boilerplate across multiple calls and ensures a consistent behavior when a token has expired. The method keeps retry logic deliberately simple: at most one retry is attempted, and only when a refresh token exists, to prevent potential retry storms and loops.

## Notes
- Exceptions thrown by RefreshTokenAsync are swallowed; if the refresh process fails, the original 401 (or other status) is returned without propagating an error.
- The initial response is disposed when a retry occurs to avoid leaking resources; the caller must dispose the final response they receive.

---

### AuthenticatedRequestAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
private async Task<HttpResponseMessage> AuthenticatedRequestAsync(Func<Task<HttpResponseMessage>> requestFactory)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `requestFactory` | `Func<Task<HttpResponseMessage>>` | — |

**Returns:** `Task<HttpResponseMessage>`


Performs an HTTP request with automatic token refresh on a 401 Unauthorized response. You supply a factory that creates the request; if the call yields 401 and a refresh token is present, it refreshes the token and retries once. The caller is responsible for disposing the returned HttpResponseMessage.

## Remarks
Encapsulates the common pattern of refreshing an access token to keep HTTP call sites concise and consistent. It enforces a single refresh-and-retry cycle to avoid repeated token refresh attempts and unintended multiple requests. If the refresh fails or there is no refresh token, the original response is returned, allowing callers to handle authentication failures uniformly. The returned HttpResponseMessage should always be disposed by the caller.

## Example
```csharp
// Inside the class that defines AuthenticatedRequestAsync, assuming a configured httpClient exists
HttpResponseMessage response = await AuthenticatedRequestAsync(() => httpClient.GetAsync("https://api.example.com/protected"));
```

## Notes
- If a 401 is observed without a valid refresh token, the call returns the original 401 without attempting a refresh.
- The final HttpResponseMessage must be disposed by the caller; the method disposes the original response only when a retry occurs and a new response is obtained.

---

### BanUserAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task BanUserAsync(string username, string? reason = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string` | — |
| `reason` | `string?` | `null` |

**Returns:** `Task`


BanUserAsync bans a user by their username via the server's moderation API. It ensures the caller is authenticated, then issues a POST to /api/moderation/ban/{Uri.EscapeDataString(username)} with a BanRequest payload that carries the optional reason. The operation completes when EnsureSuccessAsync confirms a successful HTTP response; otherwise, it throws. Use this helper when you want to ban a user through the API client instead of assembling HTTP calls yourself.

## Remarks
This method encapsulates the authentication check and the HTTP POST, providing a single, reusable surface for moderation actions. It hides the exact endpoint path and JSON payload, reducing duplication and centralizing error handling via EnsureSuccessAsync.

## Notes
- Authentication is enforced by EnsureAuthenticated before performing the request; if the client is not authenticated, this will fail early.
- The username is URL-escaped with Uri.EscapeDataString to prevent route interpretation issues.
- BanRequest is constructed with the optional reason; passing null is allowed.

---

### CreateChannelAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<ChannelDto?> CreateChannelAsync(string name, string? topic = null, bool isPublic = true,
        string? [REDACTED:CONNECTION_STRING_PASSWORD] string? encryptionSalt = null, string? wrappedRoomKey = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `name` | `string` | — |
| `topic` | `string?` | `null` |
| `isPublic` | `bool` | `true` |
| `encryptionSalt` | `string? [REDACTED:CONNECTION_STRING_PASSWORD] string?` | `null` |
| `wrappedRoomKey` | `string?` | `null` |

**Returns:** `Task<ChannelDto?>`


Creates a new channel on the server by sending a CreateChannelRequest after ensuring the caller is authenticated. Use this method when you need to programmatically create a channel by name with optional topic and visibility settings, optionally including encryption-related data; it handles request construction, HTTP communication, and JSON deserialization into a ChannelDto.

## Remarks
This abstraction centralizes the channel-creation workflow in the client. It automatically enforces authentication, builds the payload (including optional topic, visibility, and encryption-related fields), issues the POST to the /api/channels endpoint, and deserializes the response into a ChannelDto for the caller. By encapsulating these concerns, callers avoid manual HTTP handling and keep channel creation consistent across the codebase. The method returns a ChannelDto representing the newly created channel, or null if the response body cannot be parsed into that shape.

## Notes
- Requires the client to be authenticated; an unauthenticated call will be rejected by EnsureAuthenticated/EnsureSuccessAsync.
- The JSON deserialization may yield null if the response body is empty or not compatible with ChannelDto.
- If encryptionSalt or wrappedRoomKey are provided, they influence the server-side setup for encrypted channel access; incorrect values may cause the server to reject the request.

---

### CreateInviteAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<InviteDto?> CreateInviteAsync(int? maxUses = null, int? expiresInHours = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `maxUses` | `int?` | `null` |
| `expiresInHours` | `int?` | `null` |

**Returns:** `Task<InviteDto?>`


Creates a new invite by sending a POST request to the server with an optional maximum usage limit and expiration window. The operation requires the user to be authenticated and returns the created invite data when the server responds successfully. If maxUses or expiresInHours are not supplied, the server applies its defaults. Use this method when you need to programmatically generate a shareable invite with controlled usage or expiry.

## Remarks
This method serves as a focused, typed abstraction over the invites API. It hides HTTP details (endpoint path, request payload, and response parsing) and centralizes authentication and basic success handling, so callers don’t need to manage HTTP lifecycles or error translation. It relies on InviteDto and CreateInviteRequest to shape the contract and on the underlying HTTP client to perform the request.

## Notes
- Requires authentication; calling without a valid authenticated context will cause an exception via EnsureAuthenticated.
- maxUses and expiresInHours are nullable; omitting them defers to the server’s default behavior.
- Returns InviteDto?; callers should handle potential null if the response body is empty or deserialization yields no content.

---

### DeleteChannelAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task DeleteChannelAsync(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `Task`


Deletes a channel on the server by its name. It first ensures the client is authenticated, then issues an authenticated HTTP DELETE to /api/channels/{encodedName}, where the channel name is URL-encoded to safely survive special characters. The response is validated with EnsureSuccessAsync, and the operation completes when the server confirms success. Use this method when you need to remove a channel from the backend by name, rather than performing a raw HTTP request.

## Remarks
This method encapsulates the REST pattern for removing a resource and centralizes authentication and error handling. By encoding the channel name and validating the response, it provides a reliable, reusable operation for channel management across the client. It relies on EnsureAuthenticated, AuthenticatedRequestAsync, and EnsureSuccessAsync, aligning with the client's approach to API calls.

## Example
```csharp
// Delete a channel named "general"
await apiClient.DeleteChannelAsync("general");
```

## Notes
- The channel name is URL-encoded in the request path using Uri.EscapeDataString to handle spaces and special characters.
- Non-success HTTP responses will throw exceptions via EnsureSuccessAsync; consider wrapping in try/catch if you need to surface user-friendly errors.

---

### DeleteMessageAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task DeleteMessageAsync(Guid messageId)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `messageId` | `Guid` | — |

**Returns:** `Task`


Deletes a moderation message identified by messageId by issuing an authenticated HTTP DELETE request, and then ensures the response indicates success. Use this method when you need to remove a specific moderation message through the client without dealing with authentication or direct HTTP handling.

## Remarks
This method centralizes the common pattern of authenticated server mutations: vetting authentication, performing the HTTP call through a shared AuthenticatedRequestAsync wrapper, and validating success with EnsureSuccessAsync. It hides the details of the endpoint path and response handling from callers, offering a clean, exception-driven contract where failures surface as exceptions. The operation does not return a payload; callers rely on the absence of an exception to determine success.

## Notes
- Non-success HTTP responses (such as 404, 403, or 5xx) are surfaced as exceptions via EnsureSuccessAsync.
- The call depends on a valid authenticated context; if authentication is not established, EnsureAuthenticated will throw before the HTTP request is issued.

---

### DeleteMyAccountAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task DeleteMyAccountAsync(string password)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `password` | `string` | — |

**Returns:** `Task`


Deletes the currently authenticated user's account by issuing an HTTP DELETE to /api/users/me with a JSON payload containing the provided password to confirm intent. The method first verifies the caller is authenticated, performs the request, and then asserts a successful server response. Use this when a user explicitly wants to permanently remove their own account; the password requirement helps prevent accidental deletions.

## Remarks
Encapsulates the account-deletion flow within the client to centralize authentication validation, request construction, and server response handling. The operation relies on a DeleteAccountRequest DTO to carry the password payload, aligning client and server contracts and keeping sensitive data isolated in the request body. The surrounding EnsureAuthenticated and EnsureSuccessAsync calls provide clear preconditions and postconditions for callers and tests, reducing boilerplate at call sites.

## Example
```csharp
// Example usage:
await client.DeleteMyAccountAsync("P@ssw0rd!");
```

## Notes
- This is a destructive, irreversible operation; confirm user intent and consider UX safeguards before invoking.
- The method does not return a value; failures surface as exceptions via EnsureSuccessAsync. Wrap calls in appropriate error handling if needed.
- The password is transmitted as part of the request payload; avoid logging the password and ensure transport security is in place.

---

### Dispose
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public void Dispose()
```

**Returns:** `void`


Disposes the internal HTTP resource held by this ApiClient by delegating to _http.Dispose(). Call Dispose when you’re finished using the ApiClient to ensure network resources are released promptly, rather than waiting for finalization.

## Remarks

This is a straightforward implementation of the IDisposable pattern: the outer wrapper delegates cleanup to its disposable member. By disposing the inner _http resource, the ApiClient ensures that associated network resources (such as open connections) are released in a deterministic manner as soon as the caller is done with the instance.

## Example

```csharp
using (var client = new ApiClient())
{
    // use client
}
```

## Notes

- If the internal _http resource is shared with other components, disposing the ApiClient may affect those components; ensure ownership semantics are clear.
- After Dispose is called, subsequent use of the ApiClient (or its _http) may throw ObjectDisposedException unless the class guards against use after disposal.

---

### DownloadFileToTempAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<string> DownloadFileToTempAsync(string relativeUrl, string fileName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `relativeUrl` | `string` | — |
| `fileName` | `string` | — |

**Returns:** `Task<string>`


Ensures authentication, retrieves content from the given relative URL via an authenticated GET, and validates the HTTP response. It then creates a temporary EchoHub directory under the system temp path, streams the response body to a uniquely named file there, and returns the file path.

## Remarks
Centralizes the common pattern of authenticated download and temporary-file storage, reducing duplication across callers. The method uses a GUID-based filename to avoid collisions and writes the stream directly to disk, minimizing memory usage. It returns the path to the temporary file but does not perform cleanup; callers should delete the file when it is no longer needed to avoid littering the temp directory.

## Notes
- Creates the EchoHub subdirectory in the system temporary path if it does not exist.
- Uses a GUID (with no dashes) as part of the filename to guarantee uniqueness.
- The return value is a path to the downloaded temporary file; callers are responsible for cleanup.
- No cancellation token or progress reporting is exposed by this API.

---

### EnsureAuthenticated
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
private void EnsureAuthenticated()
```

**Returns:** `void`


This private guard validates that the ApiClient is in a state suitable for making authenticated requests by ensuring the internal _accessToken is present. When the token is missing or empty, it signals misuse of the client by throwing InvalidOperationException with guidance to call LoginAsync or RegisterAsync before attempting any API calls.

## Remarks
This centralizes the authentication precondition in ApiClient, so all protected operations rely on a single, consistent guard. It communicates a clear contract: you must authenticate prior to using the client. By encapsulating the check, code duplication is reduced and the error message remains uniform across methods that require authentication.

## Notes
- The method only checks for a non-empty _accessToken; it does not validate token expiry or current validity. Expired or invalid tokens may still cause failures at the API boundary, which should be handled by higher-level logic if present.

---

### EnsureSuccessAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
private static async Task EnsureSuccessAsync(HttpResponseMessage response)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `response` | `HttpResponseMessage` | — |

**Returns:** `Task`


Converts non-success HTTP responses into a single HttpRequestException with a best-effort error message. If the response is unsuccessful, it builds a message from the status code and reason phrase, then optionally enriches it by extracting a top-level 'error' or 'Error' property from a JSON body; if those properties are absent or parsing fails, it falls back to the raw body or the status message, and finally throws HttpRequestException with that message.

## Remarks
Centralizes HTTP error handling in the client. It encapsulates the logic for translating HTTP failure responses into exceptions, so callers can catch HttpRequestException and rely on a consistent message shape. It uses JsonDocument to inspect the body for 'error'/'Error' fields and gracefully degrades when the body is not JSON or lacks those fields.

## Notes
- Parsing failures during body extraction are swallowed; the catch block is intentionally empty, so a non-JSON body or parsing error won't crash the flow but may limit message richness.
- The fallback to using the raw body in the error message can reveal server details; avoid logging or exposing this in user-facing errors.
- The method is private and intended for internal use; external callers cannot invoke it directly.

---

### ExportMyDataAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<string> ExportMyDataAsync()
```

**Returns:** `Task<string>`


Downloads the authenticated user's complete data export as raw JSON text. The method authenticates the caller, performs an authenticated GET to /api/users/me/export, ensures the response indicates success, and returns the response body as a string for downstream processing or persistence. It is suited for data portability or backup scenarios where the consumer will parse or store the JSON themselves.

## Remarks
This symbol provides a focused convenience around the ApiClient by hiding endpoint details and error handling behind EnsureAuthenticated and EnsureSuccessAsync. By returning raw JSON instead of a deserialized object, it offers maximum flexibility for downstream processing, partial deserialization, or deferred parsing in response to evolving export schemas.

## Notes
- The payload is returned as raw JSON text; there is no deserialization here, so callers should parse it if they need structured data.
- The entire payload is read into memory via ReadAsStringAsync; for very large exports this can incur noticeable memory usage. Consider streaming approaches or server-side handling if the export size is a concern.

---

### GetChannelCryptoAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<ChannelCryptoDto?> GetChannelCryptoAsync(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `Task<ChannelCryptoDto?>`


Retrieves the public cryptographic metadata for a channel, indicating whether the channel is end-to-end encrypted and the key-derivation salt. If the channel does not exist, the method returns null. It requires an authenticated context, issues an HTTP GET to /api/channels/{channelName}/crypto with channelName URI-escaped, and deserializes the JSON body into a ChannelCryptoDto.

## Remarks

Encapsulates the remote API contract for channel crypto settings, providing a single, typed entry point for clients. It centralizes authentication enforcement and error handling so callers do not need to manage low-level HTTP concerns. The NotFound (404) path is represented by a null return value, while other HTTP errors are surfaced as exceptions by EnsureSuccessAsync.

## Notes

- 404 Not Found is mapped to null; non-success statuses throw.
- Channel name is escaped with Uri.EscapeDataString to ensure a safe, well-formed URL.
- JSON deserialization relies on the ChannelCryptoDto type; changes to the API shape may require updating the DTO.

---

### GetChannelMetaAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<ChannelMetaDto?> GetChannelMetaAsync(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `Task<ChannelMetaDto?>`


Fetches a channel's human-facing metadata (message count, unique posters, estimated size, created date, room id) for the <c>/meta</c> command. Returns null if it doesn't exist. The method authenticates the client, issues an HTTP GET to /api/channels/{channelName}/meta (with the channel name URI-escaped), returns null on a 404 Not Found, validates the response, and deserializes the JSON body into a ChannelMetaDto. The operation is asynchronous and relies on the client being authenticated prior to the call.

---

### GetChannelsAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<List<ChannelDto>> GetChannelsAsync()
```

**Returns:** `Task<List<ChannelDto>>`


Fetches the channels accessible to the currently authenticated user by calling the API endpoint `/api/channels`. It handles authentication, dispatches the HTTP GET, validates the response, and deserializes the JSON payload into a paginated wrapper, finally returning the list of [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) items (or an empty list if no data is available). This method provides a simple, strongly-typed surface for consumers who need to enumerate or process channels without dealing with HTTP details or pagination concerns.

## Remarks
This symbol serves as a focused data fetch for the authenticated user's channels, encapsulating transport, auth, and error handling behind a clean API. It relies on `EnsureAuthenticated()` to confirm the user identity, `EnsureSuccessAsync(...)` to surface HTTP errors, and `ReadFromJsonAsync<PaginatedResponse<ChannelDto>>()` to transform the payload. By returning `paginated?.Items ?? []`, callers always receive a non-null collection, even when the server returns no data.

## Example
```csharp
var channels = await apiClient.GetChannelsAsync();
foreach (var channel in channels)
{
    // Process each channel as needed
}
```

## Notes
- Returns an empty list when the API returns null or no items, never null.
- The method relies on authentication and HTTP status checks; callers should be prepared to handle exceptions from authentication failures or HTTP errors.

---

### GetContentType
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
private static string GetContentType(string fileName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `fileName` | `string` | — |

**Returns:** `string`


GetContentType derives a MIME type from a filename by inspecting its extension. It centralizes a small, static mapping of common extensions to standard MIME types and falls back to application/octet-stream when the extension is unrecognized.

## Remarks

By using Path.GetExtension(fileName) and ToLowerInvariant(), the method performs case-insensitive matching and keeps the logic in a single place within ApiClient.cs, ensuring consistent Content-Type values across the client. Because it is private static, this helper is an internal concern of the API client and is not exposed as part of the public API; callers should rely on higher-level abstractions for content-type handling.

## Notes

- This method considers only the file extension and does not inspect the file contents; for security-sensitive scenarios, use content-based detection as needed.
- Unrecognized or missing extensions result in application/octet-stream, which is a safe default but may not reflect the actual content type.

---

### GetEncryptionKeyAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<string> GetEncryptionKeyAsync()
```

**Returns:** `Task<string>`


Gets the server-provided encryption key by performing an authenticated HTTP GET to /api/server/encryption-key. It first ensures an authenticated context, executes the request, verifies a successful response, deserializes the JSON payload into EncryptionKeyResponse, and returns the Key value. If the server returns no content, it throws an InvalidOperationException with the message "Server returned empty encryption key response." The caller receives the encryption key as a plain string for use in client-side encryption/decryption workflows.

## Remarks
This method centralizes the retrieval of the server-supplied encryption key, encapsulating the endpoint path, authentication, and JSON deserialization behind a simple string return. It provides a stable surface for higher layers, hiding the details of the HTTP contract and error handling while ensuring a consistent failure path when the server cannot provide a key. Because keys may rotate or change over time, callers should decide on caching strategies appropriate to their security and consistency requirements.

## Notes
- The call may throw if authentication fails, the HTTP response indicates failure, or the response body is empty (as explicitly guarded by the InvalidOperationException).
- The returned value is sensitive; avoid logging or persisting the key beyond its immediate use.

---

### GetInvitesAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<List<InviteDto>> GetInvitesAsync()
```

**Returns:** `Task<List<InviteDto>>`


GetInvitesAsync retrieves the invites for the currently authenticated user by issuing an authenticated HTTP GET to /api/invites. It ensures the caller is authenticated, verifies the HTTP response indicates success, and deserializes the JSON payload into a `List<InviteDto>`. If the response body is null, an empty collection is returned, allowing callers to handle zero invites without additional null checks.

## Remarks

By encapsulating authentication, request dispatch, and JSON deserialization, this method hides boilerplate and enforces a consistent error-handling and data-contract approach for invite retrieval. It relies on the InviteDto data contract and the HTTP response content model to produce a strongly-typed result, reducing coupling between higher-level client code and the underlying HTTP details.

## Example

```csharp
var invites = await client.GetInvitesAsync();
var total = invites.Count;
```

## Notes

- Requires a valid authentication context; calling without authentication will cause EnsureAuthenticated to throw.
- Deserialization depends on the InviteDto contract; changes to server payload or InviteDto shape may require corresponding updates to this method and its callers.

---

### GetServerInfoAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<ServerStatusDto?> GetServerInfoAsync()
```

**Returns:** `Task<ServerStatusDto?>`


Retrieves the server's current status by making an asynchronous HTTP GET request to /api/server/info and deserializing the JSON response into a ServerStatusDto. It acts as a focused wrapper around a single API endpoint, allowing consumers to obtain server health and status information without performing HTTP requests or JSON parsing themselves.

## Remarks
By delegating to the HttpClient-based call, this method encapsulates transport concerns and JSON deserialization, keeping UI components focused on rendering. It relies on an HttpClient instance provided to ApiClient (typically via dependency injection), which facilitates testing through mocks or stubs. If the API contract changes (e.g., a different endpoint or DTO), updating this single method reduces the spread of changes throughout the client.

## Notes
- This method has no CancellationToken parameter; callers that need cancellation would need to adapt the signature or apply cancellation at a higher level.
- Non-success HTTP responses or JSON deserialization errors will surface as exceptions; callers should handle HttpRequestException or JsonException as appropriate.
- The return type is ServerStatusDto?, indicating callers should check for null before use, though the underlying HTTP call can still throw before a value is produced in some error cases.

---

### GetUserProfileAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<UserProfileDto?> GetUserProfileAsync(string username)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string` | — |

**Returns:** `Task<UserProfileDto?>`


Gets the profile for a given username from the API. It requires the client to be authenticated, builds a GET request to /api/users/{username}/profile with the username safely escaped for the URL, validates that the response indicates success, and then deserializes the response JSON into a UserProfileDto. The return value is a UserProfileDto? representing the retrieved profile, or null if the payload is absent.

## Remarks
By encapsulating the HTTP call and JSON deserialization, this method provides a typed, reusable access point for user-profile data. It aligns with the ApiClient's pattern of performing authenticated requests and interpreting JSON responses, reducing boilerplate for callers.

## Example
```csharp
var profile = await client.GetUserProfileAsync("alice");
if (profile is null)
{
    // handle missing profile
}
else
{
    // use profile
}
```

## Notes
- This method requires authentication; EnsureAuthenticated is called at the start and will raise if the client is not authenticated.
- The username is URL-escaped using Uri.EscapeDataString to ensure a safe path segment.
- The return type is nullable (UserProfileDto?), so callers should handle the null case appropriately.

---

### GetValidTokenAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<string?> GetValidTokenAsync()
```

**Returns:** `Task<string?>`


Gets a valid access token asynchronously. If there is no current token, it returns null. If the token is within 60 seconds of expiry and a refresh token is available, it attempts to refresh via RefreshTokenAsync; any exception from RefreshTokenAsync is swallowed to allow the caller to handle authentication failure, and the current token is returned anyway. This method is used by EchoHubConnection as the token provider for SignalR, centralizing token management so callers obtain a usable token without duplicating refresh logic themselves.

## Remarks
This method centralizes the token lifecycle for EchoHubConnection's SignalR authentication. It reduces duplication by providing a single, reusable provider that handles refresh eligibility and fail-safe fallbacks. The refresh is best-effort; if RefreshTokenAsync fails or no refresh token is present, the method returns the current token, leaving the caller to respond to an authentication failure.

## Notes
- Returns null when there is no access token, signaling the caller to re-authenticate.
- Refresh attempts occur only when the token is near expiry (within 60 seconds) and a refresh token is available.
- There is no explicit synchronization; concurrent calls may trigger multiple refresh attempts if used from multiple threads.

---

### KickUserAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task KickUserAsync(string username, string? reason = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string` | — |
| `reason` | `string?` | `null` |

**Returns:** `Task`


KickUserAsync kicks a user by username through the moderation API. It ensures the client is authenticated, posts a KickRequest with the optional reason to the server, and validates the response, throwing on failure.

## Remarks
This abstraction encapsulates the moderation HTTP call behind a typed DTO, keeping authentication and success handling centralized and out of the caller's business logic. It uses Uri.EscapeDataString to safely embed the username in the request path and relies on KickRequest to carry the optional reason payload, promoting a clean separation between transport concerns and domain logic.

## Notes
- Requires an authenticated session; the method enforces this by calling EnsureAuthenticated() before issuing the request.
- Reason is optional; passing null results in a request with no explicit reason (as defined by KickRequest).
- Username in the URL is URL-encoded via Uri.EscapeDataString to form a safe request path.

---

### LoginAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<LoginResponse> LoginAsync(string username, string password)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string` | — |
| `password` | `string` | — |

**Returns:** `Task<LoginResponse>`


LoginAsync authenticates a user by posting the supplied credentials to the API login endpoint, deserializes the LoginResponse, and stores the resulting tokens for subsequent requests. It hides HTTP transport and error handling behind a single asynchronous surface; use it when a user signs in to obtain and apply authentication tokens.

## Remarks

LoginAsync centralizes the authentication workflow within ApiClient, encapsulating the HTTP POST, response validation, and token application behind a single method. It guarantees token updates via SetTokens after a successful login so subsequent requests are authenticated, and any HTTP errors or a missing response body surface as exceptions. The response is disposed promptly through the using var pattern, ensuring resources are freed even in error cases.

## Notes

- Throws InvalidOperationException when the login response body is empty ("Login returned empty response.").
- Non-success HTTP statuses trigger exceptions via EnsureSuccessAsync.
- Relies on the server returning a non-null LoginResponse to feed SetTokens; if the payload does not provide tokens, subsequent authenticated calls may be ineffective.

---

### LoginWithRefreshTokenAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<LoginResponse> LoginWithRefreshTokenAsync(string refreshToken)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `refreshToken` | `string` | — |

**Returns:** `Task<LoginResponse>`


Exchanges a refresh token for a new LoginResponse by posting a RefreshRequest to /api/auth/refresh, validates the HTTP response, and updates the client's stored tokens. Call this method when you need to refresh the access token without prompting the user to sign in again.

## Remarks
Centralizes the token-refresh behavior within the API client: it encapsulates the HTTP call, JSON deserialization, and token state update. It relies on EnsureSuccessAsync to surface HTTP errors and on SetTokens to persist the new tokens, guaranteeing that subsequent requests use the refreshed credentials.

## Notes
- Throws InvalidOperationException if the refresh response payload is empty (`ReadFromJsonAsync<LoginResponse>`() returns null).
- Non-success HTTP responses are surfaced via EnsureSuccessAsync; callers should handle exceptions that indicate refresh failure.
- This method mutates the client's token state via SetTokens(result); a successful return means the tokens were refreshed and are ready for use in subsequent requests.

---

### LogoutAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task LogoutAsync()
```

**Returns:** `Task`


LogoutAsync ends the current session by (optionally) notifying the server to invalidate the refresh token and then clearing local authentication state. Call this when the user signs out to ensure both server-side revocation (when possible) and client-side cleanup.

## Remarks
This method centralizes the sign-out flow in the client, coordinating server-side invalidation with a robust client-side cleanup. It uses a best-effort approach: if the server logout cannot be performed (e.g., network issues), the local sign-out still completes to prevent stale credentials from being used. Clearing the Authorization header in the HTTP client helps guarantee that subsequent requests are unauthenticated, even if other parts of the application held onto tokens in memory.

## Notes
- The server logout is best-effort; any exception during the logout request is swallowed, so callers may not be notified of server-side success.
- The method clears both tokens and the Authorization header unconditionally, ensuring no authenticated state remains in the client after invocation.

---

### MuteUserAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task MuteUserAsync(string username, int? durationMinutes = null, string? reason = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string` | — |
| `durationMinutes` | `int?` | `null` |
| `reason` | `string?` | `null` |

**Returns:** `Task`


Asynchronously mutes a user identified by username by sending a MuteRequest to the moderation endpoint. It requires authentication, constructs the URL with a safely escaped username, and posts a JSON body containing the optional duration and reason. The call completes once the server confirms success.

## Remarks
Centralizes moderation API interactions for muting and hides HTTP details from callers. It relies on MuteRequest to carry the mute parameters and on the client’s authentication and success-handling scaffolding to provide consistent behavior across the application.

## Notes
- durationMinutes is nullable; if omitted, the server may apply its default mute duration.
- reason is optional; omitting it mutes without a stated reason.
- If the server returns a non-success status, EnsureSuccessAsync will throw, propagating the failure to the caller.

---

### NukeChannelAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task NukeChannelAsync(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `Task`


NukeChannelAsync performs a server-side action to nuke a moderation channel identified by channelName. It ensures the client is authenticated, then issues an authenticated HTTP DELETE request to /api/moderation/channels/{Uri.EscapeDataString(channelName)}/nuke, and awaits the server's successful response. The method returns a Task and does not produce a value; it throws on error via EnsureSuccessAsync.

## Remarks

This wrapper abstracts a destructive moderation action behind a clear name and a consistent authorization pattern. It ensures that the operation is performed only after the client is authenticated and that a non-success HTTP response will surface an exception via EnsureSuccessAsync. This keeps the caller focused on intent (nuke the channel) rather than on HTTP details.

## Example

```csharp
// Example: Nuke a moderation channel named "general"
await apiClient.NukeChannelAsync("general");
```

## Notes

- This is a destructive admin operation; use with caution and ensure proper permissions.
- The channelName is URL-escaped to handle spaces or special characters.
- The method completes only after a successful HTTP response; exceptions are thrown for errors.


---

### RefreshTokenAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task RefreshTokenAsync()
```

**Returns:** `Task`


RefreshTokenAsync retrieves a new access token using the stored refresh token. It validates that a refresh token exists, posts it to the server's /api/auth/refresh endpoint as JSON, ensures the HTTP response indicates success, and then updates the client's token state from the returned LoginResponse.

## Remarks
RefreshTokenAsync centralizes the token renewal flow inside the API client. It encapsulates the end-to-end interaction with the authentication service: validation of prerequisites, request construction, error propagation on HTTP or deserialization failures, and mutation of token state via SetTokens. Callers should rely on it to refresh credentials when needed, rather than handling HTTP details themselves.

## Notes
- Throws InvalidOperationException when there is no refresh token available. (the early guard against missing _refreshToken)
- If the HTTP response indicates failure or the response body cannot be deserialized into a LoginResponse (i.e., it is null), the method propagates the error or throws InvalidOperationException("Token refresh returned empty response.").


---

### RegisterAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<LoginResponse> RegisterAsync(string username, string password, string? displayName = null, string? inviteCode = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string` | — |
| `password` | `string` | — |
| `displayName` | `string?` | `null` |
| `inviteCode` | `string?` | `null` |

**Returns:** `Task<LoginResponse>`


Registers a new user by sending a registration payload to the server and returning the resulting LoginResponse. It constructs a RegisterRequest from the supplied username, password, and optional displayName and inviteCode, posts it to /api/auth/register, ensures the HTTP response indicates success, deserializes a LoginResponse from the response body, stores authentication tokens via SetTokens, and then returns the result. This method encapsulates the end-to-end registration flow and hides HTTP transport and token management details from callers.

## Remarks
RegisterAsync centralizes the end-to-end registration flow: request construction, transport, error handling, response parsing, and token persistence. By funneling these concerns through a single method, it ensures consistent error semantics (via EnsureSuccessAsync) and a single token-management strategy (via SetTokens) for a coherent authentication state on the client.

## Example
```csharp
// Simple registration using only required parameters
var result = await apiClient.RegisterAsync("alice", "password");
```

## Notes
- Deserialization can throw if the response body cannot be parsed as LoginResponse.
- If the server returns a non-success HTTP status, EnsureSuccessAsync will throw before parsing the body.
- Token storage is performed by SetTokens as a side effect; callers should expect authentication state to be updated after a successful registration.

---

### RekeyChannelAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<ChannelDto?> RekeyChannelAsync(string channelName, RekeyChannelRequest request)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `request` | [`RekeyChannelRequest`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) | — |

**Returns:** `Task<ChannelDto?>`


RekeyChannelAsync asynchronously initiates a key-rotation for a specific channel by posting a RekeyChannelRequest to the server and returning the updated ChannelDto. The call first asserts the caller is authenticated, then issues a POST to /api/channels/{escaped channelName}/rekey with the request payload, awaits a successful response, and deserializes the response body into a ChannelDto. The returned value may be null if the response has no content.

---

### RevokeInviteAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task RevokeInviteAsync(string code)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `code` | `string` | — |

**Returns:** `Task`


RevokeInviteAsync revokes a previously issued invitation identified by the supplied code. It ensures the caller is authenticated, then issues an authenticated HTTP DELETE to /api/invites/{Uri.EscapeDataString(code)} and finally validates the server response via EnsureSuccessAsync.

## Remarks
By encapsulating the DELETE call behind EnsureAuthenticated/AuthenticatedRequestAsync, this method provides a single, consistent mechanism for removing invites and centralizes error handling and authentication concerns. It shields callers from HTTP details and small failure modes, while aligning with other client methods that perform authenticated operations.

## Notes
- EnsureSuccessAsync will throw on non-success HTTP responses, so callers should handle exceptions for cases like missing or already-revoked invites.
- The invite code is URL-escaped with Uri.EscapeDataString to safely embed it in the request path.
- This method relies on prior authentication; if credentials are missing or invalid, EnsureAuthenticated will fail before the request is sent.

---

### SendMessageWithAttachmentsAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<MessageDto?> SendMessageWithAttachmentsAsync(
        string channelName, string content, IReadOnlyList<OutgoingAttachment> attachments, string? size = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `content` | `string` | — |
| `attachments` | `IReadOnlyList<OutgoingAttachment>` | — |
| `size` | `string?` | `null` |

**Returns:** `Task<MessageDto?>`


Sends a chat message to a channel with optional text and one or more attachments via multipart/form-data. For end-to-end encrypted channels, each attachment declares a kind and provides an encrypted preview (empty when none), and the caption is encrypted as well to protect metadata.

## Remarks

The method centralizes the multipart upload workflow for messages with attachments, including per-attachment encryption metadata handling and content-type determination. It performs authentication, constructs the form in a deterministic order so that per-file metadata (kind and preview) remains aligned with the corresponding file, and delegates transport and response handling to helper primitives (AuthenticatedRequestAsync and EnsureSuccessAsync). The optional size parameter enables a server-side formatting variation without changing the message payload.

## Notes

- For encrypted channels, DeclaredKind must be non-null to emit per-attachment kind and preview metadata; if DeclaredKind is null, those fields are omitted for that attachment.
- Each attachment is streamed individually with a Content-Type derived from the file name, and the server expects the metadata arrays (kind/preview) to line up with the corresponding files by index.
- The channel name is URL-escaped to form the request path, and an optional size query is appended when size is provided.


---

### SetTokens
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
private void SetTokens(LoginResponse result)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `result` | [`LoginResponse`](../../EchoHub.Core/DTOs/AuthDtos.cs.md) | — |

**Returns:** `void`


Sets the local authentication state from a LoginResponse by storing the access token, refresh token, and expiration, updates the HttpClient to send a Bearer token with every request, and notifies subscribers that tokens have been refreshed.

## Remarks
This method centralizes token management for the API client. By updating the shared HttpClient.DefaultRequestHeaders.Authorization, all outgoing requests automatically include the current access token, reducing boilerplate. The OnTokensRefreshed event allows other components to react to token updates (e.g., refresh UI or trigger persistence). It relies on a well-formed LoginResponse; callers should ensure result is non-null and contains valid Token/ExpiresAt values before invoking.

## Notes
- No null checks exist for result or its properties; pass a valid LoginResponse to avoid NullReferenceException.
- DefaultRequestHeaders.Authorization is a global header on the HttpClient; concurrent token refreshes could race to set it, so coordinate refresh flows if ApiClient is used from multiple threads.

---

### UnbanUserAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task UnbanUserAsync(string username)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string` | — |

**Returns:** `Task`


UnbanUserAsync lifts a ban on a user by issuing an authenticated POST request to the moderation API. You call this when you need to programmatically remove a ban for a specific username, relying on the client’s authentication state and centralized error handling rather than crafting HTTP requests yourself.

## Remarks
This method encapsulates the moderation action behind a stable API: it first ensures the caller is authenticated, then performs the request via a wrapper that handles authentication context, and finally asserts success through a centralized error-check. The username is URL-encoded to safely embed special characters in the path, and the payload is an empty JSON object, signaling a no-content action beyond the identifer in the URL. This pattern keeps moderation actions consistent across the client and reduces boilerplate for callers.

## Example
```csharp
await apiClient.UnbanUserAsync("troublemaker42");
```

## Notes
- The call requires a valid authenticated context; unauthenticated callers will be rejected by EnsureAuthenticated. 
- The method uses Uri.EscapeDataString to safely include the username in the URL path. 
- A non-success HTTP response will throw via EnsureSuccessAsync, so callers may want to handle exceptions to surface user-friendly errors. 
- The request body is an empty object, reflecting a command-based action rather than data payload.

---

### UnmuteUserAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task UnmuteUserAsync(string username)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string` | — |

**Returns:** `Task`


UnmuteUserAsync unmutes a previously muted user by issuing a server request to the moderation API. Use this method when you need to lift a mute on a specific username from the EchoHub client. It enforces authentication, posts to the /api/moderation/unmute/{username} endpoint with an empty payload, and verifies the response signals success, abstracting away the HTTP boilerplate from callers.

## Remarks
UnmuteUserAsync is a thin wrapper around the service's moderation unmute endpoint. It centralizes authentication and error handling, ensuring consistency across moderation operations. By escaping the username in the URL, it guards against path-breaking characters and injection issues. The method returns a Task and does not expose a value; success is communicated by completing normally or by exceptions produced by EnsureSuccessAsync.

## Example
```csharp
// Example usage
await client.UnmuteUserAsync("john_doe");
```

## Notes
- The request uses POST to /api/moderation/unmute/{Uri.EscapeDataString(username)} with an empty payload.
- Authentication is required; EnsureAuthenticated() enforces this before the HTTP call.
- The method does not return a value; success is indicated by normal completion, otherwise an exception is thrown by EnsureSuccessAsync.
- Username is URL-encoded to prevent issues with special characters.

---

### UpdateChannelTopicAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<ChannelDto?> UpdateChannelTopicAsync(string channelName, string? topic)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `topic` | `string?` | — |

**Returns:** `Task<ChannelDto?>`


Updates the topic of a channel by performing an authenticated HTTP PUT to the server. Given a channel name and a desired topic, it builds UpdateTopicRequest and sends it to /api/channels/{channelName}/topic, with the channel name URL-escaped. It then validates the response and deserializes the updated ChannelDto from the response body. The method returns ChannelDto? to reflect a possibly absent payload. Use this when you need to change or clear a channel's topic in an authenticated context instead of composing the HTTP request manually.

## Remarks
This method serves as the client-facing abstraction over a REST endpoint for channel topics. It centralizes authentication handling via EnsureAuthenticated and ensures consistent error handling with EnsureSuccessAsync, so callers don't need to manage HTTP status codes directly. It also encapsulates URL-encoding of the channel name, preventing issues with special characters.

## Example
```csharp
// Example: update topic for a channel
var updated = await apiClient.UpdateChannelTopicAsync("general", "Discussions about general topics");
```

## Notes
- Topic can be null to clear the topic.
- The response is deserialized to ChannelDto; if the server returns no content, the result may be null.
- The channel name in the URL is escaped to handle spaces or special characters.

---

### UploadAvatarAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<string?> UploadAvatarAsync(Stream imageStream, string fileName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `imageStream` | `Stream` | — |
| `fileName` | `string` | — |

**Returns:** `Task<string?>`


Uploads the given image as the user's avatar by posting it as a multipart/form-data request to the server endpoint "/api/users/avatar". It ensures the caller is authenticated, builds the form data with the file attached under the field named "file", sets the content type from the file name, sends the request, validates the response, and deserializes the resulting JSON to return the AvatarAscii value (or null if missing). This method encapsulates the HTTP, content creation, and JSON parsing details, allowing callers to simply provide a stream and a filename to obtain the avatar representation.

## Remarks
This method serves as a focused wrapper around the avatar-upload workflow. It centralizes authentication enforcement, multipart payload construction, endpoint contract (field name "file" and route "/api/users/avatar"), and server deserialization into a single, reusable call. By returning AvatarAscii from AvatarUploadResponse, it decouples UI concerns from image handling, enabling lightweight representations of avatars when a full image is not required.

## Example
```csharp
// Assuming 'client' is an instance of ApiClient and the user is authenticated
using var stream = File.OpenRead("path/to/avatar.png");
string? ascii = await client.UploadAvatarAsync(stream, "avatar.png");
Console.WriteLine(ascii ?? "No avatar ASCII returned");
```

## Notes
- The request uses multipart/form-data with the form field named "file" as required by the server contract. Changes to the field name or endpoint would break this integration.
- Ensure authentication is established before calling; the method invokes EnsureAuthenticated() and may fail if the user is not authenticated.
- The method returns AvatarAscii from AvatarUploadResponse, which may be null if the server omits it or the response cannot be deserialized.

---

## SendUrlAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<MessageDto?> SendUrlAsync(string channelName, string url, string? size = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `url` | `string` | — |
| `size` | `string?` | `null` |

**Returns:** `Task<MessageDto?>`


SendUrlAsync posts a URL to the specified channel via the EchoHub HTTP API and returns the server’s MessageDto for the resulting message. Use this when you need to programmatically share a link in a channel and receive a typed representation of the posted message, with the client handling authentication, request construction, and response parsing.

## Remarks

By encapsulating the endpoint path construction, JSON payload, and authentication steps, this method reduces boilerplate for callers and enforces consistent error handling through EnsureSuccessAsync. It also escapes the channel name when building the URL to prevent routing errors caused by special characters.

## Example

```csharp
var result = await client.SendUrlAsync("general", "https://example.com", size: "1024");
```

## Notes

- The size parameter is appended as a raw query component; callers should pass URL-safe values or the method should be extended to URL-encode this value.
- The return value may be null if the response body is empty; callers should handle null appropriately.


---

## UpdateProfileAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<UserProfileDto?> UpdateProfileAsync(UpdateProfileRequest request)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `request` | [`UpdateProfileRequest`](../../EchoHub.Core/DTOs/ProfileDtos.cs.md) | — |

**Returns:** `Task<UserProfileDto?>`


Updates the authenticated user's profile by issuing a PUT request to /api/users/profile with the provided UpdateProfileRequest and returning the updated UserProfileDto. It first ensures the caller is authenticated, then performs the HTTP request, validates the response, and deserializes the JSON payload into a UserProfileDto (or null if the response has no body).

## Remarks

This method centralizes the concerns around making authenticated HTTP calls: enforcing authentication, handling HTTP success semantics, and deserializing the server payload into a typed DTO. It provides a clean, reusable surface for updating the user's profile without leaking HTTP details to callers. By wrapping EnsureSuccessAsync and JSON deserialization, it promotes consistent error handling and data shape assumptions across the client.

## Notes

- The return value may be null if the response body is empty; callers should guard against null.
- EnsureSuccessAsync will throw on non-success HTTP statuses, so error handling is centralized here.
- Deserialization uses `ReadFromJsonAsync<UserProfileDto>`; ensure the response content is JSON matching UserProfileDto.

---