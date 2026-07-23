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
  - [SendUrlAsync](#sendurlasync)
  - [SetTokens](#settokens)
  - [UnbanUserAsync](#unbanuserasync)
  - [UnmuteUserAsync](#unmuteuserasync)
  - [UpdateChannelTopicAsync](#updatechanneltopicasync)
  - [UpdateProfileAsync](#updateprofileasync)
  - [UploadAvatarAsync](#uploadavatarasync)

---

## ApiClient
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** class

```csharp
public sealed class ApiClient : IDisposable
```


A high-level HTTP client that encapsulates the application's server API surface and manages authentication state (access token, refresh token and expiration). Reach for `ApiClient` when you need a single reusable component to perform user authentication (`RegisterAsync`, `LoginAsync`, `LoginWithRefreshTokenAsync`, `LogoutAsync`), query chat data (`GetChannelsAsync`, `GetChannelMetaAsync`, `GetChannelCryptoAsync`), upload/download assets (`UploadAvatarAsync`, `DownloadFileToTempAsync`) and send messages (`SendMessageWithAttachmentsAsync`, `SendUrlAsync`) while keeping token refresh logic co-located with the HTTP interactions. The client exposes `BaseUrl`, the current `Token`/`RefreshToken`, and an `OnTokensRefreshed` event consumers can subscribe to.

## Remarks
`ApiClient` centralizes network calls and authentication for the client-side application: it owns a single `HttpClient` instance (`_http`), stores the current tokens and expiration, and provides convenience methods that map to common server endpoints (login/registration, channels, invites, user profile, file operations and message sending). It also provides `GetValidTokenAsync` specifically for token providers (for example, a [`EchoHubConnection`](EchoHubConnection.cs.md) SignalR token provider) so callers can obtain a token that will be refreshed if it is about to expire. Because these responsibilities cross-cut the UI and real-time layers, the class exists to avoid scattering token refresh and HTTP wiring throughout the codebase.

## Notes
- Token refresh timing and concurrency: the implementation intends to "Refresh if token expires within 60 seconds" (see `GetValidTokenAsync`), and one comment notes that callers should "Return current token and let the caller handle auth failure." The source does not show explicit synchronization around token refresh, so concurrent callers could trigger multiple simultaneous refresh attempts.
- Attachments and encrypted channels: `SendMessageWithAttachmentsAsync` requires that for end-to-end encrypted channels each file has a declared kind and a room-encrypted preview; these must be provided in the same order as the files so the server's attachment index aligns. Non-image attachments use an empty preview string; the caption is also room-encrypted.
- Logout and disposal: `LogoutAsync` is described as a "best-effort logout" (errors are non-fatal). The type implements `IDisposable`, so callers should dispose the `ApiClient` instance when finished to allow it to release its resources.


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


The `ApiClient` constructor initializes the client to target a REST API at the provided `baseUrl`. It trims any trailing slash from the input, assigns the result to `BaseUrl`, and creates a new `HttpClient` stored in `_http` with its `BaseAddress` set to a `Uri` built from `BaseUrl`. This setup enables subsequent requests to use relative endpoints against the configured base address.

## Remarks
By centralizing base URL handling and HTTP client initialization, this constructor ensures a consistent, reusable `HttpClient` instance per `ApiClient` and avoids URL-assembly errors caused by trailing slashes.

## Notes
- Passing `null` for `baseUrl` will throw a `NullReferenceException` when `TrimEnd('/')` is invoked.
- Passing an empty or otherwise invalid URL will throw a `UriFormatException` when constructing `new Uri(BaseUrl)`.


---

### BaseUrl
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** property

```csharp
public string BaseUrl
```


This read-only property exposes the base endpoint URL the `ApiClient` uses to build full request URIs. Reading `BaseUrl` helps diagnostics and test scenarios where you need to know or confirm the target server without inspecting internal configuration.

---

### RefreshToken
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** property

```csharp
public string? RefreshToken => _refreshToken
```


The `RefreshToken` property exposes the current refresh token stored by the client as a read-only accessor. It returns the private backing field `_refreshToken` as a nullable `string`, meaning callers can read the value but cannot assign a new one through this property.

---

### Token
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** property

```csharp
public string? Token => _accessToken
```


Public property `Token` is a read-only accessor that forwards to the private field `_accessToken`, returning its current value as a nullable string. It does not transform or mutate state; its purpose is to expose the managed token to callers who need to inspect or reuse the token value. Since the underlying field can be `null` before a token is obtained, callers should handle a possible null result when using `Token`.

## Remarks
This straightforward forwarder exists to surface the internal token while preserving encapsulation of the backing field. It supports diagnostics, testing, and scenarios where downstream components require the raw token without duplicating token management logic, without re-exposing the field directly.

## Notes
- The value can be `null` until a token is obtained; always null-check before use.

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


Assigns a server role to a user by performing an authenticated HTTP POST to `"/api/moderation/role"` with an [`AssignRoleRequest`](../../EchoHub.Core/DTOs/ModerationDtos.cs.md) that contains the target `username` and `role`. It enforces authentication via `EnsureAuthenticated()`, sends the request asynchronously through `AuthenticatedRequestAsync`, and validates the response with `EnsureSuccessAsync`. The method returns a `Task` and completes when the server confirms the assignment, or raises an exception if authentication fails or the server returns an error.

## Remarks
By centralizing moderation actions in `ApiClient`, this method provides a single, authentication-aware path for role management. It hides HTTP details and enforces consistent error handling by combining `EnsureAuthenticated()`, `AuthenticatedRequestAsync`, and `EnsureSuccessAsync`, so callers can rely on a uniform success/exception model across moderation operations.

## Notes
- This method returns a `Task` and does not produce a value; success is signaled by completion.
- Failures can arise from lack of authentication, server-side permission checks, or other HTTP errors surfaced by `EnsureSuccessAsync`.
- The call payload relies on [`AssignRoleRequest`](../../EchoHub.Core/DTOs/ModerationDtos.cs.md); changes to that DTO will affect this method.

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


This private helper performs an HTTP GET to the specified `url` and, if the response is `HttpStatusCode.Unauthorized` and a non-empty `_refreshToken` is available, refreshes the token via `RefreshTokenAsync()` and retries the request once. The caller must dispose of the returned `HttpResponseMessage`.

## Remarks
By centralizing this token-refresh pattern for GETs, it reduces duplicated boilerplate at call sites while ensuring a consistent retry policy. If the refresh cannot be completed, the original 401 is returned, preserving standard authorization semantics. The method also disposes the intermediate response during a retry to avoid resource leaks.

## Notes
- The final `HttpResponseMessage` returned by this method must be disposed by the caller after processing.
- A token refresh is attempted only when `_refreshToken` is non-empty and the initial response is `HttpStatusCode.Unauthorized`; otherwise, no refresh is performed.

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


Executes a request produced by the provided `requestFactory` and, if the initial response is `HttpStatusCode.Unauthorized` and a refresh token is available, refreshes the token via `RefreshTokenAsync()` and retries the request once. The original response is disposed before the retry response is returned, and the caller is responsible for disposing the final `HttpResponseMessage`.

## Remarks
Centralizes the token-refresh retry pattern for authenticated API calls. It hides token management behind a single helper so callers can issue requests without duplicating refresh logic, while ensuring a single, well-scoped retry path. Disposing the intermediate response avoids leaks, and the final response is returned for the caller to manage lifetime.

## Notes
- If `RefreshTokenAsync()` fails (throws) or the refresh path throws, the exception is swallowed and the method returns the original 401 response, signaling to the caller that re-authentication is needed.
- Only one automatic retry is performed; a second 401 will be returned as-is.

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


Bans a user identified by the provided `username` by performing an authenticated HTTP POST to the moderation endpoint, optionally including a `reason` via a [`BanRequest`](../../EchoHub.Core/DTOs/ModerationDtos.cs.md). The username is escaped with `Uri.EscapeDataString(username)` when constructing the URL, and the operation is validated by `EnsureSuccessAsync` after the HTTP call.

## Remarks
This method provides a focused client-side wrapper around the server's moderation API to ban a user. It ensures the caller is authenticated, encodes the target username for safe URL usage, and sends a [`BanRequest`](../../EchoHub.Core/DTOs/ModerationDtos.cs.md) containing the optional `reason`. By funneling the action through `AuthenticatedRequestAsync` and `EnsureSuccessAsync`, it enforces consistent authentication and error handling across moderation operations, keeping concerns separated from higher-level business logic.

---

### CreateChannelAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<ChannelDto?> CreateChannelAsync(string name, string? topic = null, bool isPublic = true,
        string? password = null, string? encryptionSalt = null, string? wrappedRoomKey = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `name` | `string` | — |
| `topic` | `string?` | `null` |
| `isPublic` | `bool` | `true` |
| `password` | `string?` | `null` |
| `encryptionSalt` | `string?` | `null` |
| `wrappedRoomKey` | `string?` | `null` |

**Returns:** `Task<ChannelDto?>`


CreateChannelAsync creates a new chat channel on the server by sending an authenticated POST to `"/api/channels"` with a [`CreateChannelRequest`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) constructed from the provided parameters, and returns a `ChannelDto?` from the response content when successful. Use this helper when you need to create a channel with a name and optional topic, visibility, and security settings without writing the HTTP boilerplate yourself.

## Remarks
This method centralizes the channel creation workflow in the client: authenticate, construct a [`CreateChannelRequest`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) from the provided parameters, POST to `"/api/channels"`, ensure the response indicates success, and deserialize a [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) from the response content. It keeps higher-level code free from repetitive HTTP boilerplate and ensures consistent error handling via `EnsureAuthenticated` and `EnsureSuccessAsync`.

## Notes
- The method returns `ChannelDto?`; if the server returns no content or invalid JSON, the result may be `null`. Callers should check for `null` before use.
- Requires authentication; `EnsureAuthenticated()` enforces this, so unauthenticated callers will fail.
- The parameters `password`, `encryptionSalt`, and `wrappedRoomKey` relate to channel security; supply them only when creating secure channels and be mindful of security implications.

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


Creates a new invite by posting a [`CreateInviteRequest`](../../EchoHub.Core/DTOs/InviteDtos.cs.md) to `/api/invites` after ensuring authentication, and returns the deserialized `InviteDto?` from the response. Provide optional `maxUses` and `expiresInHours` to control usage limits and expiry.

## Remarks
This method hides the transport details of invite creation, centralizing authentication, request serialization, error handling, and JSON deserialization into a single, strongly-typed call. By returning an `InviteDto?`, it cleanly signals the possibility of no content while enforcing a consistent contract via [`InviteDto`](../../EchoHub.Core/DTOs/InviteDtos.cs.md) and [`CreateInviteRequest`](../../EchoHub.Core/DTOs/InviteDtos.cs.md).

## Example
```csharp
var invite = await apiClient.CreateInviteAsync(maxUses: 5, expiresInHours: 24);
```

## Notes
- The return value may be `null` if the response body is empty or cannot be deserialized as an [`InviteDto`](../../EchoHub.Core/DTOs/InviteDtos.cs.md).
- The method calls `EnsureAuthenticated()`, so unauthenticated callers will observe an exception if authentication has not been established.
- Providing `null` for both parameters relies on server defaults; if you need explicit control, supply non-null values for `maxUses` and/or `expiresInHours`.

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


Deletes the channel named `channelName` by issuing an authenticated HTTP DELETE to `/api/channels/{Uri.EscapeDataString(channelName)}`. It first calls `EnsureAuthenticated` to guarantee the caller is authorized, then performs the request via `AuthenticatedRequestAsync` using `_http.DeleteAsync(...)`, and finally awaits `EnsureSuccessAsync` to validate the response.

## Remarks
Using `EnsureAuthenticated` and `AuthenticatedRequestAsync` centralizes authentication flow and uniform error handling for API calls in the client. Escaping the channel name with `Uri.EscapeDataString` prevents malformed URLs and potential issues when channel names contain special characters.

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


Deletes a moderation message identified by `messageId` by issuing an HTTP DELETE to `/api/moderation/messages/{messageId}` after ensuring the client is authenticated. It uses `AuthenticatedRequestAsync` to execute the request against the shared `_http` client and then validates the server response with `EnsureSuccessAsync` before returning.

## Remarks
Like other mutating API calls, `DeleteMessageAsync` enforces authentication via `EnsureAuthenticated` and delegates the HTTP invocation to `AuthenticatedRequestAsync`. This pattern centralizes authentication, request execution, and response validation around a shared `_http` client, ensuring consistent behavior for moderation mutations.

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


Deletes the currently authenticated user's account after re-confirming intent with the provided password. The method ensures the caller is authenticated (`EnsureAuthenticated`), issues an HTTP `DELETE` to `"/api/users/me"` with a [`DeleteAccountRequest`](../../EchoHub.Core/DTOs/AccountDtos.cs.md) payload containing the `password` (constructed via `JsonContent.Create`), and validates the response with `EnsureSuccessAsync`.

## Remarks
Encapsulates the account-deletion flow behind a client API, centralizing authentication checks, request construction, and error handling for a destructive user action. Provides a single, consistent path to delete the user's account, preventing boilerplate duplication across the UI layer and aligning with other API client methods.

## Example
```csharp
await apiClient.DeleteMyAccountAsync("Pa$$w0rd");
```

## Notes
- Destructive action: irreversible; ensure the user truly intends to delete their account before invoking this.
- Password is transmitted in the request body as part of [`DeleteAccountRequest`](../../EchoHub.Core/DTOs/AccountDtos.cs.md); ensure transport security and proper credential hygiene.

---

### Dispose
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public void Dispose()
```

**Returns:** `void`


Disposes the ApiClient's internal HTTP resource by delegating to `_http.Dispose()`. This ensures that the underlying HTTP client and its resources are released when the ApiClient is disposed.

## Remarks

This is a straightforward delegation in the IDisposable pattern. It relies on `_http` implementing `IDisposable` and being non-null; ensure `_http` is initialized before disposing to avoid potential `NullReferenceException`.

## Notes

- Potential `NullReferenceException` if `_http` is null; consider guarding or ensuring initialization guarantees.

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


Downloads a resource from `relativeUrl` using an authenticated GET, streams the response to a uniquely named temporary file under the EchoHub temp directory, and returns the created file path as a `string`. It authenticates via `EnsureAuthenticated()`, fetches the payload with `AuthenticatedGetAsync(relativeUrl)`, and writes the response body to disk without buffering the entire content.

## Remarks
By centralizing authentication, streaming, and temporary-file management, this abstraction reduces boilerplate at call sites and helps callers avoid loading large payloads into memory. The implementation uses streaming (`ReadAsStreamAsync` followed by `CopyToAsync`) to minimize memory usage and guarantees a unique temporary file name with a `Guid`.

## Example
```csharp
// Example usage assumes an instance named `client` of `ApiClient`
string tempPath = await client.DownloadFileToTempAsync("reports/annual.pdf", "annual.pdf");
```

## Notes
- The method returns a path to a temporary file located under the EchoHub temp directory; there is no automatic cleanup, so callers should delete the file when it is no longer needed.

---

### EnsureAuthenticated
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
private void EnsureAuthenticated()
```

**Returns:** `void`


Ensures that the client is authenticated by validating that the private field `_accessToken` is not null or empty; if it is missing, it throws an `InvalidOperationException` with guidance to call `LoginAsync` or `RegisterAsync` first. This guard is typically invoked at the start of API calls to fail fast when authentication has not yet been established.

## Remarks
By centralizing the authentication precondition, this guard prevents accidental unauthorized requests and provides a consistent error when the client isn't authenticated. It keeps authentication logic in one place, making future enhancements (such as token validation or refresh handling) easier to implement without duplicating checks across multiple call sites. The private scope communicates that this is an internal invariant of the API client, not part of its public surface.

## Notes
- The check only verifies presence of `_accessToken`; it does not validate expiry, issuer, or revocation. A non-empty token can still be invalid at runtime, causing a request to fail after this guard passes.
```
// Token could be expired or revoked even though `_accessToken` is non-empty.
// EnsureAuthenticated() won't catch this; a later API call will fail.
```
- Because the method is private, external code cannot invoke it directly, so callers rely on the class's internal usage pattern to uphold the authentication precondition. If a future path bypasses this guard, authentication requirements might be violated.


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


Ensures that a non-success `HttpResponseMessage` is surfaced as a descriptive `HttpRequestException` by inspecting the response and extracting a meaningful error message. For successful responses it returns immediately; for failures it builds a message from the status code and reason phrase, then tries to read and parse a JSON body to use an `error`/`Error` property when available, otherwise falling back to the raw body.

## Remarks
Centralizes HTTP error reporting in the client. It uses the `Content` payload and attempts to parse JSON with `JsonDocument` to surface server-provided error details, improving diagnosability across the API surface. If the body cannot be parsed or no error field is present, the message remains based on the status code and reason phrase.

## Notes
- The body reading is wrapped in a try/catch that suppresses parsing errors; if parsing fails, the message is based on the initial status code and reason phrase.
- It only recognizes top-level `error` or `Error` properties; nested fields are ignored.
- The method is `private` and `static`, serving as an internal helper within the containing class and not part of the public API.

---

### ExportMyDataAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<string> ExportMyDataAsync()
```

**Returns:** `Task<string>`


Downloads the authenticated caller's full data export as raw JSON text by first ensuring the caller is authenticated, performing an authenticated GET against `/api/users/me/export`, validating the response with `EnsureSuccessAsync`, and returning the response body as a `string`. This `ExportMyDataAsync` method serves as a concise, high-level entry point when you need a portable copy of the current user's data without writing repetitive HTTP boilerplate.

## Remarks
By encapsulating authentication, request dispatch, and success verification, `ExportMyDataAsync` provides a single, consistent path for obtaining a user's data export. It hides HTTP plumbing from callers and pairs with downstream deserialization to produce typed representations if needed. It relies on the client authentication flow (`EnsureAuthenticated`) and the standard success check (`EnsureSuccessAsync`).

## Notes
- ``ExportMyDataAsync`` returns a raw JSON `string`; callers should deserialize it into typed objects as needed.
- The payload can be large; callers should be mindful of memory usage when exporting very large user datasets.

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


Gets the crypto metadata for a specific channel, including whether end-to-end encryption is enabled and the key-derivation salt, returning a [`ChannelCryptoDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) when the channel exists. If the channel doesn't exist, it returns null; the call requires authentication and performs an authenticated GET to `/api/channels/{channelName}/crypto` with `channelName` escaped via `Uri.EscapeDataString`.

## Remarks
By wrapping this HTTP call in a single client method, the library provides a consistent, strongly-typed view of a channel's crypto settings and hides HTTP details from callers. It also centralizes the NotFound -> null convention, so callers can distinguish a missing channel from other failures without adding boilerplate.

## Notes
- When the channel is missing, the method yields null; other HTTP errors throw via `EnsureSuccessAsync`, so callers should be prepared to handle exceptions for non-NotFound failures.
- The channel name is escaped with `Uri.EscapeDataString`, preventing path issues with special characters.
- JSON deserialization relies on the shape of [`ChannelCryptoDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md); mismatches or deserialization errors can surface as exceptions during `ReadFromJsonAsync`.

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


Fetches a channel's public, human-facing metadata used by the `/meta` command. It returns a [`ChannelMetaDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) containing the message count, number of unique posters, an estimated size, the channel's creation date, and the room id when the metadata exists; if no metadata exists for the channel, it returns `null`. The call first authenticates via `EnsureAuthenticated()`, URL-encodes the channel name using `Uri.EscapeDataString`, performs an HTTP GET to `/api/channels/{channelName}/meta` with `AuthenticatedGetAsync`, returns `null` on a `NotFound` response, enforces success via `EnsureSuccessAsync`, and deserializes the response body to [`ChannelMetaDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) with `ReadFromJsonAsync<ChannelMetaDto>()`.

---

### GetChannelsAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<List<ChannelDto>> GetChannelsAsync()
```

**Returns:** `Task<List<ChannelDto>>`


Gets the channels for the currently authenticated user by issuing an authenticated GET to `/api/channels` (via `AuthenticatedGetAsync("/api/channels")`), validating the response with `EnsureSuccessAsync`, and deserializing the payload into a [`PaginatedResponse<ChannelDto>`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) before returning the collection `paginated?.Items ?? []`.

## Remarks
By encapsulating the HTTP call, authentication, and pagination unwrap, this method provides a single, testable surface for retrieving channels and ensures callers always receive a non-null list. It relies on the server to provide a [`PaginatedResponse<ChannelDto>`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) payload; if the server returns no items the method yields an empty list rather than null, smoothing downstream collection handling.

## Example
```csharp
List<ChannelDto> channels = await client.GetChannelsAsync();
```

## Notes
- Returns an empty list when there are no items in the response instead of null.
- Exceptions from authentication or HTTP failure propagate (e.g., via `EnsureAuthenticated` / `EnsureSuccessAsync`).

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


Determines the MIME content type for a file name by inspecting its extension. The private static method `GetContentType` extracts the extension with `Path.GetExtension(fileName)` and normalizes it to lower case using `ToLowerInvariant()`, then maps known extensions to their standard MIME types. It covers common image formats (`.jpg`/`.jpeg` → `image/jpeg`, `.png` → `image/png`, `.gif` → `image/gif`, `.webp` → `image/webp`), text and document types (`.txt` → `text/plain`, `.pdf` → `application/pdf`), and falls back to `application/octet-stream` for unknown extensions.

## Remarks
This function centralizes the mapping from file extensions to MIME types so all call sites share the same logic. The switch expression keeps the mapping compact and extensible, with a safe default of `application/octet-stream` for unknown extensions. It is intended for internal use by the HTTP client to populate Content-Type headers from filenames rather than duplicating mime-type logic across callers.

## Notes
- Null input is not guarded; a null `fileName` will cause a `NullReferenceException` when calling `ToLowerInvariant()` on the extracted extension. Ensure the argument is non-null before invoking this method, or wrap the call with a null-check.

---

### GetEncryptionKeyAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<string> GetEncryptionKeyAsync()
```

**Returns:** `Task<string>`


GetEncryptionKeyAsync fetches the server's encryption key in an authenticated context by issuing a GET to `/api/server/encryption-key`, validating the response with `EnsureSuccessAsync`, deserializing the payload to [`EncryptionKeyResponse`](../../EchoHub.Core/DTOs/ServerDtos.cs.md) via `ReadFromJsonAsync<EncryptionKeyResponse>()`, and returning `result.Key`. If the server returns an empty encryption key payload, it throws `InvalidOperationException("Server returned empty encryption key response.")`.

---

### GetInvitesAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<List<InviteDto>> GetInvitesAsync()
```

**Returns:** `Task<List<InviteDto>>`


GetInvitesAsync retrieves the current user's invites by performing an authenticated GET request to `/api/invites` and deserializing the JSON payload into a `List<InviteDto>`. It enforces authentication up-front and ensures a successful HTTP response before returning the deserialized collection (or an empty list if the payload is null).

## Remarks
GetInvitesAsync encapsulates the standard flow for retrieving a protected resource: ensure authentication, issue a GET to `/api/invites`, and deserialize the response into a `List<InviteDto>`. It yields a non-null list to callers (empty when the server returns no content) and keeps HTTP/JSON concerns hidden behind a stable API.

## Notes
- A null payload is coerced into an empty list to avoid nulls for callers.
- No paging is handled; this reads a single page of results from `/api/invites` and will not automatically fetch subsequent pages.

---

### GetServerInfoAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<ServerStatusDto?> GetServerInfoAsync()
```

**Returns:** `Task<ServerStatusDto?>`


Fetches the current server status by issuing an asynchronous HTTP GET to `"/api/server/info"` and deserializing the response into a [`ServerStatusDto`](../../EchoHub.Core/DTOs/ServerDtos.cs.md) via the underlying HTTP client. It returns the resulting `ServerStatusDto?`, or `null` if the payload is absent, providing a typed, convenient entry point for clients that need server health information.

## Remarks
This method acts as a typed façade over the raw HTTP call, binding the `/api/server/info` endpoint to the [`ServerStatusDto`](../../EchoHub.Core/DTOs/ServerDtos.cs.md) contract. By encapsulating this endpoint behind `GetServerInfoAsync`, callers gain a stable API surface that is easier to mock in tests and evolve without changing consuming code.

## Notes
- Non-success HTTP status codes will throw (e.g., `HttpRequestException`), so callers should handle exceptions as part of their error handling strategy.
- The JSON payload must conform to the shape of [`ServerStatusDto`](../../EchoHub.Core/DTOs/ServerDtos.cs.md); any schema drift can lead to deserialization failures.

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


GetUserProfileAsync fetches the profile for a given `username` by asserting authentication via `EnsureAuthenticated()`, issuing an authenticated GET with `AuthenticatedGetAsync($"/api/users/{Uri.EscapeDataString(username)}/profile")`, and deserializing the response into a `UserProfileDto?` via `ReadFromJsonAsync<UserProfileDto>()`. It should be used whenever you need a typed representation of a user's profile rather than crafting HTTP calls yourself; it centralizes URL construction, auth, and JSON deserialization in one place.

## Remarks
This symbol encapsulates the profile-fetching pattern within the `ApiClient`, ensuring consistent error handling and response processing. It relies on `Uri` for safe username escaping, `AuthenticatedGetAsync` for the authenticated request, and `ReadFromJsonAsync` to produce a strongly-typed [`UserProfileDto`](../../EchoHub.Core/DTOs/ProfileDtos.cs.md) payload, keeping UI code focused on presentation rather than transport concerns.

## Example
```csharp
var profile = await apiClient.GetUserProfileAsync("alice");
```

## Notes
- The return type is `UserProfileDto?`, so callers must handle the possibility of a `null` result if the response body is empty or JSON null.
- The method escapes the provided `username` and targets the `/api/users/{escapedUsername}/profile` endpoint, so callers should not attempt to bypass the abstraction for manual URL manipulation.

---

### GetValidTokenAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<string?> GetValidTokenAsync()
```

**Returns:** `Task<string?>`


Returns a valid access token, refreshing it when it is near expiry. This helper is used by the [`EchoHubConnection`](EchoHubConnection.cs.md)-provided SignalR token provider to obtain a token without requiring callers to manage refresh logic themselves.

## Remarks

To minimize unnecessary refresh calls, the method returns the current token if it is still valid; it only triggers a refresh when the token expires within 60 seconds and a `_refreshToken` is available. If `RefreshTokenAsync` fails, the exception is swallowed and the current `_accessToken` is returned, leaving authentication failure handling to the caller. The check uses `DateTimeOffset.UtcNow` for a timezone-agnostic calculation of freshness. Note that there is no synchronization around refresh operations, so concurrent calls may trigger multiple refresh attempts.

## Notes

- The method returns `null` when `_accessToken` is null or empty, signaling that no token is currently available.
- If `_expiresAt` indicates imminent expiry but `_refreshToken` is missing, a potentially expired or invalid token may be returned.
- Refresh failures are swallowed; downstream logic should verify the resulting token and act on authentication failures accordingly.

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


`KickUserAsync` asynchronously kicks a user by `username` through the moderation API, optionally including a `reason`. It ensures the client is authenticated with `EnsureAuthenticated()`, posts a [`KickRequest`](../../EchoHub.Core/DTOs/ModerationDtos.cs.md) to `/api/moderation/kick/{Uri.EscapeDataString(username)}` via `AuthenticatedRequestAsync`, and validates the result with `EnsureSuccessAsync`.

## Remarks

Centralizes moderation actions on the client side by wrapping the HTTP call, ensuring authentication, and standardizing error handling for moderation endpoints. It composes with the shared `_http` client and uses `Uri.EscapeDataString` to safely embed the `username` in the request path.

## Notes

- The `username` is URL-escaped via `Uri.EscapeDataString` to handle special characters.
- If the API returns an error, `EnsureSuccessAsync` will throw an exception; callers should handle it accordingly.

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


`LoginAsync` encapsulates the client login flow: it constructs a [`LoginRequest`](../../EchoHub.Core/DTOs/AuthDtos.cs.md) from the supplied `username` and `password`, posts it to the authentication endpoint via `_http.PostAsJsonAsync`, calls `EnsureSuccessAsync` to enforce a successful status, reads a [`LoginResponse`](../../EchoHub.Core/DTOs/AuthDtos.cs.md) from `response.Content` with `ReadFromJsonAsync<LoginResponse>()`, invokes `SetTokens` to persist tokens, and returns the [`LoginResponse`](../../EchoHub.Core/DTOs/AuthDtos.cs.md). This abstracts away HTTP wiring, error handling, and token management so callers only need to provide credentials to obtain tokens.

## Remarks
LoginAsync centralizes the login protocol for the client: it wires HTTP request/response handling, error checking, and token persistence in a single method. It relies on `EnsureSuccessAsync` to throw for non-success status codes, and it guards against an empty response by throwing `InvalidOperationException` if `Content.ReadFromJsonAsync<LoginResponse>()` returns null; callers can rely on the returned [`LoginResponse`](../../EchoHub.Core/DTOs/AuthDtos.cs.md) to carry tokens after `SetTokens` has run.

## Notes
- It throws `InvalidOperationException` if the login response body is empty.
- On success, it calls `SetTokens` to persist authentication tokens for subsequent requests.


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


LoginWithRefreshTokenAsync encapsulates the client-side token-refresh flow: it posts a [`RefreshRequest`](../../EchoHub.Core/DTOs/AuthDtos.cs.md) containing the provided `refreshToken` to `/api/auth/refresh`, ensures a successful HTTP response via `EnsureSuccessAsync`, deserializes a [`LoginResponse`](../../EchoHub.Core/DTOs/AuthDtos.cs.md) from the response content, updates the stored tokens with `SetTokens`, and returns the resulting [`LoginResponse`](../../EchoHub.Core/DTOs/AuthDtos.cs.md). Use this method when you have a valid refresh token and want to obtain new tokens in a single, consistent operation instead of duplicating the HTTP call and token persistence logic.

## Remarks
By encapsulating the refresh flow behind the client boundary, this method ensures a single, consistent approach to exchanging a `refreshToken` for new tokens, with uniform error handling and token persistence via `SetTokens`. It coordinates the HTTP request, success validation, payload deserialization, and token state management, so callers don't have to duplicate boilerplate or risk divergent token-update semantics.

## Notes
- Deserialization errors will bubble up if the response payload cannot be parsed as [`LoginResponse`](../../EchoHub.Core/DTOs/AuthDtos.cs.md).
- The method throws `InvalidOperationException` when the response body is empty to signal that a valid [`LoginResponse`](../../EchoHub.Core/DTOs/AuthDtos.cs.md) was not received.

---

### LogoutAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task LogoutAsync()
```

**Returns:** `Task`


LogoutAsync asynchronously logs out the current user. If a `_refreshToken` exists, it attempts to invalidate the server-side session by posting a `RefreshRequest(_refreshToken)` to `/api/auth/logout` via `_http.PostAsJsonAsync`, swallowing any exceptions to provide a best-effort logout. It then clears the client state by setting `_accessToken` and `_refreshToken` to `null` and removing the `Authorization` header from `_http.DefaultRequestHeaders`.

## Remarks
LogoutAsync centralizes sign-out behavior: it tries to invalidate the server-side session when a `_refreshToken` is present, then always clears local credentials to prevent further authenticated calls. The best-effort approach (catch-swallow) favors responsiveness over guaranteed server termination, which is acceptable for most clients but not a guarantee in all environments. It coordinates with the HTTP client state by clearing the `Authorization` header so no stale tokens accompany future requests.

## Notes
- Best-effort logout swallows exceptions; callers should not rely on server-side termination in all failure scenarios.
- After calling, tokens are cleared and the `Authorization` header is removed, so subsequent requests are unauthenticated until re-authentication occurs.

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


MuteUserAsync mutes a specified user by posting a [`MuteRequest`](../../EchoHub.Core/DTOs/ModerationDtos.cs.md) to the moderation API endpoint `"/api/moderation/mute/{username}"`, after ensuring the caller is authenticated via `EnsureAuthenticated()`. The username is escaped with `Uri.EscapeDataString`, and the operation accepts an optional `durationMinutes` and an optional `reason`; the call completes when `EnsureSuccessAsync` confirms the response.

## Remarks
This method serves as a focused wrapper around the moderation API, consolidating authentication, request payload construction via [`MuteRequest`](../../EchoHub.Core/DTOs/ModerationDtos.cs.md), and centralized success handling via `EnsureSuccessAsync`. It enables client code to perform user moderations without dealing with low-level HTTP details or URL construction, aligning with other moderation helper methods.

## Example
```csharp
// Example: mute a user for 60 minutes with a reason
await apiClient.MuteUserAsync("user123", durationMinutes: 60, reason: "violation of rules");
```

## Notes
- This method requires authentication; if not authenticated, `EnsureAuthenticated()` will trigger a failure.
- Both `durationMinutes` and `reason` are optional; passing null will rely on server-side defaults or policy.
- The username is URL-escaped to safely form the request URL in `"/api/moderation/mute/{username}"`.

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


NukeChannelAsync issues an authenticated HTTP DELETE to remove a moderation channel identified by `channelName`. It begins by calling `EnsureAuthenticated()` to enforce credentials, then executes the request wrapped in `AuthenticatedRequestAsync` against the endpoint `/api/moderation/channels/{Uri.EscapeDataString(channelName)}/nuke` using `_http.DeleteAsync`, and finally validates the response with `EnsureSuccessAsync` before returning.

## Remarks
This method acts as a focused helper for performing the destructive operation of removing a moderation channel. By encapsulating authentication (`EnsureAuthenticated`) and consistent HTTP handling (`AuthenticatedRequestAsync` plus `EnsureSuccessAsync`), it provides a stable, discoverable entry point for channel-nuking that aligns with other moderation endpoints in the client.

## Notes
- `channelName` must be non-null; `Uri.EscapeDataString` will throw on null, so callers should validate input before invoking this method.

---

### RefreshTokenAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task RefreshTokenAsync()
```

**Returns:** `Task`


RefreshTokenAsync refreshes the client's authentication by sending the current `_refreshToken` to the server at `` `/api/auth/refresh` `` via POST and updating tokens with `` `SetTokens` `` on success. It guards against a missing `_refreshToken` by throwing `` `InvalidOperationException` `` when it is null or empty, then posts the request, ensures the HTTP response indicates success with `` `EnsureSuccessAsync` ``, and requires a non-null `` [`LoginResponse`](../../EchoHub.Core/DTOs/AuthDtos.cs.md) `` to apply new tokens. If the response body is empty, it throws `` `InvalidOperationException` ``.

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


Registers a new user by posting a [`RegisterRequest`](../../EchoHub.Core/DTOs/AuthDtos.cs.md) to `/api/auth/register`, validating the response, reading a [`LoginResponse`](../../EchoHub.Core/DTOs/AuthDtos.cs.md) from the content, and updating the client's tokens with `SetTokens` before returning the result as a [`LoginResponse`](../../EchoHub.Core/DTOs/AuthDtos.cs.md).
This is the onboarding entry point you call when creating an account with `username` and `password`, optionally supplying `displayName` and/or `inviteCode`.

## Remarks
Encapsulates the end-to-end registration flow: payload creation, HTTP transport, response handling, and token synchronization, so callers don't manage these concerns directly. It relies on `_http` to `PostAsJsonAsync` a [`RegisterRequest`](../../EchoHub.Core/DTOs/AuthDtos.cs.md), `EnsureSuccessAsync` to enforce successful HTTP statuses, and `SetTokens` to persist authentication state after a successful registration.

## Notes
- Non-success HTTP responses throw via `EnsureSuccessAsync`; handle to surface user-friendly errors.
- If the server returns an empty body, an `InvalidOperationException` is thrown with the message `"Registration returned empty response."`.
- Optional parameters `displayName` and `inviteCode` may be omitted (null) and will be serialized accordingly in the [`RegisterRequest`](../../EchoHub.Core/DTOs/AuthDtos.cs.md) payload.

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


Asynchronously rekeys a channel, given by `channelName`, by posting a [`RekeyChannelRequest`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) to the server once the client is authenticated. It sends JSON to the endpoint `/api/channels/{Uri.EscapeDataString(channelName)}/rekey`, awaits a successful response via `EnsureSuccessAsync`, and returns the deserialized [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) from the response body (or `null` if the response contains no content).

## Remarks
Centralizes the rekey operation behind the `ApiClient` to hide HTTP details from callers; authentication, request/response handling, and JSON deserialization are encapsulated in one place, making rekey usage simple and consistent across callers.

## Notes
- Non-success HTTP responses (for example, 404 Not Found or 403 Forbidden) surface as exceptions via `EnsureSuccessAsync`.

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


RevokeInviteAsync revokes a pending invite identified by `code` by issuing an authenticated HTTP DELETE to `/api/invites/{Uri.EscapeDataString(code)}` and then validating the result with `EnsureSuccessAsync`. Use this when you need to remove a specific invite from the server instead of attempting a manual HTTP call; the operation requires authentication (`EnsureAuthenticated()`) and goes through `AuthenticatedRequestAsync` for centralized error handling.

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


Sends a message to a channel with optional text and one or more attachments. When used on end-to-end encrypted channels, each attachment includes a declared kind and a room-encrypted preview, and the caption is encrypted as well. The method builds a `MultipartFormDataContent` payload containing the message `content` and, for each attachment, the file stream plus optional `kind` and `preview` fields, then posts to the channel messages API and returns the parsed [`MessageDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) from the response.

## Remarks
This method centralizes channel-message sending with attachments behind an authentication boundary. It hides the multipart construction and per-attachment encoding from callers, while preserving the server's expectation that per-file `kind` and `preview` fields appear in the same order as the attachments. The URL path uses `Uri.EscapeDataString` to safely encode the `channelName`, and an optional `size` query parameter can be supplied to influence the shape of the response.

## Notes
- Only emit per-file `kind` and `preview` fields when `att.DeclaredKind` is non-null; this supports encrypted channels while keeping behavior sensible for non-encrypted use cases.
- The request is sent via an authenticated wrapper and uses `POST` to `/api/channels/{channelName}/messages` with an optional `size` query parameter when provided.
- Attachment content types are derived from the file name and set on the corresponding `StreamContent`; mismatches between file type and filename may affect server handling.

---

### SendUrlAsync
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


Posts the provided `url` to a specific channel by issuing a POST with a [`SendUrlRequest`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) and returning the resulting `MessageDto?`. The operation requires authentication, URL-encodes the channel name with `Uri.EscapeDataString(channelName)`, and supports an optional `size` parameter that appends a `?size=...` query; the response is validated via `EnsureSuccessAsync` and deserialized into a [`MessageDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) from the response content.

## Remarks

This method encapsulates a server endpoint for sharing external URLs as channel messages, centralizing authentication, path encoding, and JSON (de)serialization behind a single helper. It keeps API usage consistent across callers and reduces boilerplate by hiding the HTTP details behind `ApiClient`.

## Notes

- The return type is `MessageDto?`; callers should account for possible null if the response body is empty.
- The `size` query is only appended when `size` is not `null`; otherwise, the request path omits the query.
- The channel name is encoded with `Uri.EscapeDataString` to prevent path injection or malformed URLs.

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


Sets authentication state from a [`LoginResponse`](../../EchoHub.Core/DTOs/AuthDtos.cs.md) by copying `result.Token`, `result.RefreshToken`, and `result.ExpiresAt` into the private members `_accessToken`, `_refreshToken`, and `_expiresAt`, then configures the HTTP client's authorization header to apply the Bearer token to future requests by assigning a new `AuthenticationHeaderValue` to `_http.DefaultRequestHeaders.Authorization`. It also triggers the `OnTokensRefreshed` event to notify subscribers that the token set has been updated; this method is typically called after a successful login or token refresh to wire the tokens into the HTTP client.

## Remarks
By centralizing token handling within the API client, token state and request authorization headers stay in sync across all outgoing calls. The `OnTokensRefreshed` event provides a hook for UI updates or dependent services to react to token changes without scattering header mutations across call sites.

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


UnbanUserAsync unbans a previously banned user by issuing an authenticated POST request to the moderation API at /api/moderation/unban/{username}. It first ensures the caller is authenticated, escapes the username for the URL, and sends an empty JSON payload. After receiving the response, it validates success via EnsureSuccessAsync. The method returns a Task and should be awaited by callers when they want to unban a user and await the operation’s completion.

## Remarks
This method demonstrates the client’s pattern for authenticated, state-changing moderation actions, wrapping a REST endpoint in a typed API call and using EnsureAuthenticated/AuthenticatedRequestAsync to perform the request before validating the response with EnsureSuccessAsync.

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


UnmuteUserAsync lifts a user's mute by ensuring authentication and issuing an authenticated POST to `/api/moderation/unmute/{Uri.EscapeDataString(username)}` via `AuthenticatedRequestAsync` that calls `_http.PostAsJsonAsync(..., new { })`, followed by `EnsureSuccessAsync` to verify the result.

Use this when your moderation flow needs to lift a mute on a specific user.

## Remarks

- It relies on the shared authentication pattern (`EnsureAuthenticated` and `AuthenticatedRequestAsync`) to centralize authorization and error handling for moderation calls.
- Encoding the `username` with `Uri.EscapeDataString` prevents malformed URLs when usernames contain special characters.
- The method returns `Task` and does not expose a value; success is indicated by completing normally, while non-success responses throw.

## Notes

- Throws on non-success HTTP responses; callers should handle exceptions or propagate them as part of their error handling strategy.

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


Updates the topic for a channel by issuing an authenticated HTTP PUT request to the API endpoint `/api/channels/{channelName}/topic` with an [`UpdateTopicRequest`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) payload, and returns the updated [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) from the response when the operation succeeds. This method encapsulates authentication, request serialization, and response deserialization so callers can change a channel’s topic without handling low-level HTTP details.

## Remarks
This method acts as a focused API client wrapper that centralizes concerns like authentication, HTTP communication, and error handling behind a strongly-typed surface. It URL-encodes the channel name for safety and uses a dedicated [`UpdateTopicRequest`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) DTO to keep the API contract isolated from domain models, returning a [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) that reflects the post-update channel state.

## Example
```csharp
// Most common usage: update the topic of a channel and obtain the updated channel representation
ChannelDto? updated = await client.UpdateChannelTopicAsync("general", "Welcome to the General channel");
```

## Notes
- The server determines how a null `topic` is interpreted (e.g., clearing the topic vs. rejecting the request); rely on the server contract for semantics.
- Non-success HTTP responses will throw via `EnsureSuccessAsync`, so callers should handle potential exceptions accordingly.

---

### UpdateProfileAsync
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


Ensures the current user is authenticated, then issues an HTTP PUT to `/api/users/profile` with the [`UpdateProfileRequest`](../../EchoHub.Core/DTOs/ProfileDtos.cs.md) payload and returns the updated profile as a [`UserProfileDto`](../../EchoHub.Core/DTOs/ProfileDtos.cs.md). It uses `AuthenticatedRequestAsync` and `PutAsJsonAsync` for the request, `EnsureSuccessAsync` to verify the response, and `ReadFromJsonAsync<UserProfileDto>()` to deserialize the result.

## Remarks
It centralizes authentication and error handling for profile updates, providing a single, strongly-typed contract for client code. This wrapper mirrors the server API surface at `/api/users/profile`, ensuring consistency between client calls and server expectations while keeping callers focused on business logic rather than HTTP plumbing.

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


Uploads a user avatar by streaming the provided image as multipart/form-data to the server endpoint `/api/users/avatar` after ensuring the caller is authenticated. The image is wrapped in a `MultipartFormDataContent` with a `StreamContent` whose `ContentType` is derived from `GetContentType(fileName)`, the request is posted via `AuthenticatedRequestAsync` to `_http.PostAsync(...)`, the response is deserialized with `ReadFromJsonAsync<AvatarUploadResponse>()`, and the method returns `AvatarAscii` (or `null` if absent).

## Remarks
Encapsulates avatar-upload logic behind a single API that handles authentication, HTTP payload construction, and JSON deserialization, reducing duplication for callers and giving a single contract for server-side [`AvatarUploadResponse`](../../EchoHub.Core/DTOs/ProfileDtos.cs.md). It coordinates the HTTP client, content builders, and the [`AvatarUploadResponse`](../../EchoHub.Core/DTOs/ProfileDtos.cs.md) contract to produce a simple ASCII avatar string, decoupling UI concerns from transport details.

## Example
```csharp
// Example: typical usage
string? ascii = await client.UploadAvatarAsync(imageStream, "avatar.png");
```


---