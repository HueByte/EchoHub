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
- [DeleteChannelAsync](#deletechannelasync)
- [DeleteMessageAsync](#deletemessageasync)
- [Dispose](#dispose)
- [DownloadFileToTempAsync](#downloadfiletotempasync)
- [EnsureAuthenticated](#ensureauthenticated)
- [EnsureSuccessAsync](#ensuresuccessasync)
- [GetChannelsAsync](#getchannelsasync)
- [GetContentType](#getcontenttype)
- [GetEncryptionKeyAsync](#getencryptionkeyasync)
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
- [SendUrlAsync](#sendurlasync)
- [SetTokens](#settokens)
- [UnbanUserAsync](#unbanuserasync)
- [UnmuteUserAsync](#unmuteuserasync)
- [UpdateChannelTopicAsync](#updatechanneltopicasync)
- [UpdateProfileAsync](#updateprofileasync)
- [UploadAvatarAsync](#uploadavatarasync)
- [UploadFileAsync](#uploadfileasync)

---

## ApiClient
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** class

```csharp
public sealed class ApiClient : IDisposable
```


A small, higher-level HTTP client that centralizes authentication token management and the EchoHub REST surface. Use ApiClient when you want a single consumer-facing object to perform user registration, login/refresh/logout flows, profile and channel operations, file uploads/downloads, and moderation actions without wiring up HttpClient and token handling yourself.

## Remarks
ApiClient wraps an HttpClient and keeps the current access/refresh tokens and their expiry. It exposes Token and RefreshToken for callers that need them and an OnTokensRefreshed event that can be observed when the client updates its stored tokens. The class also provides authenticated request helpers (AuthenticatedGetAsync and AuthenticatedRequestAsync) that will attempt an automatic token refresh on HTTP 401 responses and, if refresh fails, return the original 401 to the caller (per the inline comments). GetValidTokenAsync exists specifically to supply a valid access token (refreshing if the token is about to expire) and is noted as being used by the SignalR connection code as a token provider.

## Example
```csharp
using var api = new ApiClient("https://api.example.com");

// Authenticate
var login = await api.LoginAsync("alice", "s3cr3t");
if (login != null)
{
    // Call into REST surface
    var channels = await api.GetChannelsAsync();
    // Use GetValidTokenAsync when constructing a SignalR connection token provider
    var token = await api.GetValidTokenAsync();
}

// Dispose when finished (releases underlying HttpClient)
```

## Notes
- GetValidTokenAsync will refresh the stored token if it expires within 60 seconds (per the inline comment). Callers that keep long-lived connections (e.g., SignalR) should use this method as their token provider.
- LogoutAsync is described as a "best-effort" logout in the source; server-side session invalidation may not be guaranteed if the network call fails.
- AuthenticatedGetAsync and AuthenticatedRequestAsync attempt a refresh on 401; if the refresh attempt fails the original 401 response is preserved and returned to the caller (so callers must handle authentication failures themselves).
- ApiClient implements IDisposable; callers should dispose the instance to release the underlying HttpClient.


---

## ApiClient (constructor)
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** constructor

```csharp
public ApiClient(string baseUrl)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `baseUrl` | `string` | — |


Initializes a new ApiClient instance for communicating with the API at the given base URL. It normalizes the base URL by trimming a trailing slash and initializes an HttpClient with BaseAddress set to that URL so all subsequent requests can be made with relative endpoints.

## Remarks
By centralizing HttpClient creation here, the ApiClient guarantees a single, consistent base address and centralized HTTP configuration for downstream requests. This design makes it easy to swap targets (e.g., dev, staging, prod) by changing the baseUrl at construction time, without touching the request logic.

## Example
```csharp
// Create a client configured for the production API
var client = new ApiClient("https://api.example.com/");
```

## Notes
- Be aware that new Uri(baseUrl) requires an absolute URL; passing a relative or malformed URL will throw UriFormatException. Ensure baseUrl is a well-formed absolute URI (including scheme).
- This constructor creates a new HttpClient instance per ApiClient. If many clients are created, consider reusing HttpClient or using IHttpClientFactory to avoid socket exhaustion in long-running applications.

---

## BaseUrl
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** property

```csharp
public string BaseUrl
```


BaseUrl is a read-only string property that exposes the root URL used by the ApiClient to compose its HTTP requests. It identifies the base endpoint for all API calls, so consumers can understand where requests are targeted and, when needed, log or adjust endpoints without inspecting individual request construction. Because it only exposes a getter, the value is set during object construction and remains immutable for the lifetime of the instance, ensuring consistent routing of API calls.

## Remarks
Centralizes the API root address so all request URIs can be built relative to a single value, ensuring consistency across the client. It supports environment-specific configuration by allowing a different base URL to be supplied per deployment without altering the request-building logic. The read-only nature of this property communicates that the base URL is fixed for the lifetime of the ApiClient instance, promoting predictable behavior.

## Notes
- Ensure a valid non-null base URL is provided to avoid failed requests.

---

## RefreshToken
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** property

```csharp
public string? RefreshToken => _refreshToken
```


Provides read-only access to the client's current refresh token. The value is a nullable string and may be null if no token has been retrieved yet. This property enables callers to observe authentication state and decide whether to trigger a token refresh or perform related diagnostics without mutating the internal token state.

## Remarks

By exposing the private _refreshToken via a getter, the class preserves encapsulation while offering visibility into the authentication state. This helps coordinate refresh flows and debugging without introducing mutability from external code. Consumers must handle the nullable nature of the value and avoid assuming a token is always present.

## Notes

- The refresh token is sensitive data; avoid logging it or displaying it in UI.
- The property can return null; check for null before using the value.
- Updates to _refreshToken occur internally; external code cannot modify it through this property.

---

## Token
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** property

```csharp
public string? Token => _accessToken
```


Token is a read-only, nullable string property that exposes the current access token stored in the private field `_accessToken`. It returns the token value when available, or null if no token has been established yet.

## Remarks

Token provides a safe, public access point to the current authentication token without exposing how tokens are stored or refreshed. By exposing it as a read-only property, the class can evolve its internal token management (refresh, rotation) without breaking callers. Callers should treat the value as sensitive data and avoid logging or displaying it.

## Notes

- The value may be null; verify before use.
- Do not log or expose the token; treat it as sensitive data.

---

## AssignRoleAsync
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


AssignRoleAsync sends a JSON payload to the backend to assign a role to a user. It ensures authentication, posts to /api/moderation/role with username and role, and validates the response.

## Remarks
It acts as a focused wrapper around the moderation API, consolidating authentication, request construction, and error handling in a single call. The method uses a typed AssignRoleRequest payload to avoid manual JSON assembly and relies on EnsureSuccessAsync to surface non-success HTTP statuses as exceptions. The resulting operation completes only after the server has acknowledged the change, allowing callers to proceed with confidence.

## Notes
- Throws if authentication is missing or the server returns an error status (as enforced by EnsureAuthenticated and EnsureSuccessAsync).
- The response is disposed after the call via `using` to ensure resources are released promptly.
- The API contract is tied to the AssignRoleRequest DTO; changes to the server-side payload or endpoint would require updating the client DTO.

---

## AuthenticatedGetAsync
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


Executes a GET request against the given URL and automatically refreshes the authentication token if the server responds with 401 Unauthorized. If a refresh token is available, it calls RefreshTokenAsync and retries the request once; otherwise it returns the original 401 response. The caller is responsible for disposing the returned HttpResponseMessage.

## Remarks

This abstraction hides the common pattern of refreshing an access token and retrying a single request, reducing boilerplate for callers that need authenticated data. It centralizes the refresh logic behind a single helper, ensuring consistent behavior across GET operations and reducing the risk of forgetting to refresh on 401. The use of a single retry means a second network call is made only when a refresh is possible, and a failed refresh leaves the original 401 intact.

## Example

```csharp
// Example: within the same class
var response = await AuthenticatedGetAsync("https://api.example.com/data");
// process response (e.g., read content, check StatusCode, etc.)
```

## Notes

- Potential race condition: if many concurrent requests trigger a token refresh, coordinate RefreshTokenAsync calls to avoid duplicate refreshes.
- The method returns the final HttpResponseMessage; the caller must dispose it in all code paths. Exceptions may propagate for non-HTTP errors (e.g., network failures) instead of being turned into a 401.

---

## AuthenticatedRequestAsync
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


Executes a provided asynchronous request and, if the response is 401 Unauthorized and a refresh token is available, refreshes the token and retries the request once. The caller is responsible for disposing the HttpResponseMessage returned by this method.

## Remarks

Centralizes token refresh logic for authenticated API calls, reducing boilerplate at call sites. It only triggers a refresh when a 401 Unauthorized is returned and a refresh token is available, avoiding unnecessary requests. If refreshing succeeds, the same request factory is invoked again to retry; the original response is disposed to prevent leaks, and the final response is returned to the caller. If refreshing fails, or the retry throws, the original 401 is returned and no exception escapes.

## Notes

- Refresh failures are swallowed; you will receive the original 401 rather than an exception.
- The requestFactory is invoked twice for a single call; ensure it is safe to replay (e.g., no side effects beyond the request creation).


---

## BanUserAsync
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


Bans a user by issuing an authenticated HTTP POST to the moderation endpoint with a BanRequest payload that may include a reason. Use this when your application needs to enforce a ban programmatically from the client layer, abstracting away the HTTP details and error handling.

## Remarks
BanUserAsync encapsulates the server interaction behind the API client, requiring authentication and validating the response. It builds the request URL with Uri.EscapeDataString(username) to safely handle usernames that may contain characters requiring encoding, and constructs the payload via BanRequest(reason). After dispatching the request, it awaits the response and throws on non-success status through EnsureSuccessAsync. Callers simply await the Task to surface any exceptions as needed.

## Example
```csharp
// Example: ban a user with a reason
await apiClient.BanUserAsync("eve", "Violation of policy");
```

## Notes
- Requires authentication; if the caller is not authenticated, EnsureAuthenticated will fail up-front.
- The username is URL-escaped in the path (Uri.EscapeDataString) to prevent malformed URLs when usernames contain special characters.
- EnsureSuccessAsync is used to validate the HTTP response; non-success status codes will raise exceptions for the caller to handle.
- The BanRequest payload is constructed with the provided reason; passing null results in a BanRequest without a reason value.

---

## CreateChannelAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<ChannelDto?> CreateChannelAsync(string name, string? topic = null, bool isPublic = true)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `name` | `string` | — |
| `topic` | `string?` | `null` |
| `isPublic` | `bool` | `true` |

**Returns:** `Task<ChannelDto?>`


Creates a new chat channel on the server using the provided name, optional topic, and visibility flag. This asynchronous method handles authentication, builds the request payload (CreateChannelRequest), posts it to the server via POST /api/channels, ensures the response indicates success, and returns the deserialized ChannelDto representing the created channel. If the server does not include a response body, the method may return null.

## Remarks
By encapsulating the authentication check and HTTP round-trip, this method provides a stable, server-aligned surface for channel management. It relies on shared data transfer objects (ChannelDto, CreateChannelRequest) and a consistent error handling pathway (EnsureSuccessAsync), aligning with other API-management operations in EchoHub.Client. The isPublic flag determines the channel's visibility on the server, and topic is optional, allowing callers to set contextual metadata at creation time.

## Example
```csharp
ChannelDto? channel = await client.CreateChannelAsync("General", "General discussion");
```

## Notes
- Requires an authenticated context; attempting to call without authentication may result in an exception from EnsureAuthenticated().
- The return type is ChannelDto?, so callers should handle potential null when the server returns no content.


---

## DeleteChannelAsync
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


Deletes a channel on the server by name through an authenticated HTTP DELETE request. The method first ensures the client is authenticated, then sends a DELETE to /api/channels/{Uri.EscapeDataString(channelName)} using Uri.EscapeDataString to safely encode the channel name in the URL, and finally validates the response with EnsureSuccessAsync. The operation completes when the server confirms success; any authentication or non-success responses surface as exceptions to the caller.

## Remarks
This method encapsulates the standard pattern for performing an authenticated REST operation against the EchoHub API. By enforcing authentication via EnsureAuthenticated and wrapping the HTTP call with AuthenticatedRequestAsync, it provides consistent error handling and transport setup for destructive actions like deletion. The channelName is URL-encoded to prevent issues with special characters in the path. This method does not return a value; callers rely on the absence of an exception to infer success.

## Notes
- It will throw if authentication fails or if the server responds with a non-success status; error propagation occurs through EnsureSuccessAsync and the underlying HTTP client. 
- The channel name is embedded in the URL path; Uri.EscapeDataString ensures it is safely encoded for transport in the request URI.


---

## DeleteMessageAsync
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


Deletes a moderation message by its GUID. The method first ensures the caller is authenticated, then issues an HTTP DELETE to /api/moderation/messages/{messageId} inside an authenticated request wrapper, and finally validates the response with EnsureSuccessAsync. It returns a Task and does not expose the HTTP response content.

## Remarks
This wrapper encapsulates the standard authenticated API call pattern used for moderation operations, centralizing authentication checks and uniform error handling. It promotes a domain-focused usage (removing a moderation message) rather than manual HTTP plumbing, making it easier to mock in tests and to reason about failure semantics across related API calls.

## Example
```csharp
// Example: delete a moderation message by ID
await client.DeleteMessageAsync(Guid.NewGuid());
```

## Notes
- This method does not return a value; failures (e.g., authentication issues, not-found, or server errors) are surfaced via exceptions from EnsureSuccessAsync.
- The API endpoint requires an authenticated context; ensure the client is authenticated before invoking this method to avoid a failed call at EnsureAuthenticated().

---

## Dispose
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public void Dispose()
```

**Returns:** `void`


Dispose delegates to the internal _http.Dispose, releasing the resources held by the ApiClient when disposed. Call Dispose when you are finished with the client to ensure the underlying HTTP resources are freed.

## Remarks
Dispose is a simple delegation that ties the ApiClient's cleanup to the lifecycle of its internal HTTP client. This keeps resource management localized to the wrapper while relying on the inner _http instance to perform the actual disposal. It assumes _http is non-null at disposal time; if _http is null, this call will throw a NullReferenceException. For a more robust pattern, consider implementing the full IDisposable pattern and guarding disposal of all owned resources.

## Notes
- This method will throw NullReferenceException if _http is null at disposal time.
- If there are additional disposable resources owned by the class, dispose them here as well or implement the complete dispose pattern to ensure proper cleanup.

---

## DownloadFileToTempAsync
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


Downloads a file via an authenticated HTTP request, writes it to a temporary EchoHub directory with a GUID-based filename, and returns the path to the created file. Use this when you need a local, disposable copy of a remote file for short-lived processing, rather than streaming directly from the response.

## Remarks
This symbol centralizes the common pattern of fetching a remote file and persisting it locally in a temporary location. It ensures the request runs under the authenticated session, validates the HTTP response, and streams the content to a uniquely named file under the EchoHub temp folder, returning the path for subsequent use. By encapsulating this flow, callers avoid duplicating temp-file management and get a consistent, collision-free temporary storage strategy.

## Example
```csharp
// Example usage: obtain a local temp file path for a downloaded asset
string path = await apiClient.DownloadFileToTempAsync("docs/manual.pdf", "manual.pdf");
// Use the file at 'path' as needed, then delete when finished
```

## Notes
- The caller is responsible for deleting the returned temporary file when it is no longer needed.
- The function writes to a dedicated EchoHub subdirectory under the system temp path and uses a GUID-based filename to avoid collisions.
- If authentication fails or the HTTP response indicates an error, an exception will be thrown during authentication or EnsureSuccessAsync.

---

## EnsureAuthenticated
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
private void EnsureAuthenticated()
```

**Returns:** `void`


Ensures the API client is authenticated before performing operations that require an access token by checking the private _accessToken field. If the token is missing or empty, it throws an InvalidOperationException with the message "Not authenticated. Call LoginAsync or RegisterAsync first."

## Remarks
Internally used by other API methods to fail fast when authentication hasn't occurred. It centralizes the precondition of having a non-empty access token, reducing duplication and ensuring consistent error reporting when authentication is required.

## Notes
- Does not verify token expiry or perform refresh; it only ensures a token is present.
- Because it is private, it expresses a local guard rather than a public authentication contract; external callers should rely on the public API methods to enforce authentication.

---

## EnsureSuccessAsync
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


Ensures that an HTTP response is successful and, if not, throws a descriptive HttpRequestException. It short-circuits on success, and on failure it attempts to extract a meaningful error message from the response body (preferring a JSON property named error or Error). If no such property is found or the body cannot be parsed, it falls back to the response status code and reason phrase before throwing HttpRequestException with the assembled message.

## Remarks
Centralizes HTTP error handling for the API client to provide consistent failure messages across calls. It prefers structured error information when the server returns JSON, but remains robust against non-JSON bodies, empty content, or missing error fields by using the status information as a fallback. Being a private static helper, it is an internal implementation detail designed to be invoked after an HttpResponseMessage is obtained from a request.

## Notes
- The method assumes the response object is non-null; passing null will throw a NullReferenceException.
- It searches for a JSON property named "error" (lowercase) and, if not found, for "Error" (capitalized). If neither is present or parsing fails, the status code and reason phrase are used as the basis for the error message.

---

## GetChannelsAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<List<ChannelDto>> GetChannelsAsync()
```

**Returns:** `Task<List<ChannelDto>>`


Fetches the list of channels available to the authenticated user by issuing an HTTP GET to /api/channels, deserializing the response as [`PaginatedResponse<ChannelDto>`](../../EchoHub.Core/DTOs/CommonDtos.cs.md), and returning the Items as a `List<ChannelDto>`. It ensures the user is authenticated before the request and gracefully handles missing data by returning an empty list when no items are present.

## Remarks
This wrapper centralizes channel retrieval, consolidating authentication, HTTP, and JSON deserialization into a single, strongly-typed call. It relies on the standard error-handling helpers (EnsureAuthenticated, EnsureSuccessAsync) to provide consistent failure semantics. It treats the server response as a [`PaginatedResponse<ChannelDto>`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) and returns its Items collection directly; paging controls are not surfaced to callers.

## Example
```csharp
// Example usage: retrieve channels and inspect the result count
var channels = await apiClient.GetChannelsAsync();
Console.WriteLine($"Retrieved {channels.Count} channels.");
```

## Notes
- The method requires a currently authenticated session; ensure authentication is established before calling.
- The return value is an empty list if the server returns null or contains no items; paging is not exposed by this method.

---

## GetContentType
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


GetContentType is a private static helper that maps a file name to its MIME type by inspecting the file's extension. It normalizes the extension to lowercase and returns a corresponding MIME string, defaulting to application/octet-stream for unknown types; use it when you need a consistent Content-Type for a file payload instead of scattering MIME strings across the codebase.

## Remarks
GetContentType centralizes the extension-to-MIME mapping for the class, reducing duplication and ensuring consistent behavior wherever a file's Content-Type is determined. It relies on Path.GetExtension to extract the extension and string.ToLowerInvariant to normalize it, so changes to the mapping stay localized here. Because the method is private static, it remains a low-level implementation detail of the containing type, making it easy to adjust or extend the supported types without affecting public APIs.

## Notes
- Passing null for fileName will throw an ArgumentNullException due to the initial extension extraction; callers should ensure a non-null value is supplied.
- Filenames without an extension will map to the default application/octet-stream; if a specific type is required, consider validating the extension before calling this helper.

---

## GetEncryptionKeyAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<string> GetEncryptionKeyAsync()
```

**Returns:** `Task<string>`


Retrieves the server-provided encryption key for the authenticated client by issuing an authenticated GET request to /api/server/encryption-key. Call this when you need the server-generated key to perform client-side encryption or to configure downstream components that rely on the server-provided key.

## Remarks
Encapsulates the authentication check, HTTP call, and JSON deserialization into a single, reusable operation for retrieving the server encryption key. This centralizes error handling (such as empty responses) and keeps encryption-related logic decoupled from transport concerns.

## Example
```csharp
// Example usage
var key = await client.GetEncryptionKeyAsync();
// Use 'key' to perform client-side encryption
```

## Notes
- The Key value may be null if the server returns a null Key; callers should validate before use.
- A non-success HTTP response or an empty payload will result in an exception (EnsureSuccessAsync or the JSON read path) propagating to the caller.
- Each call fetches a fresh key from the server; no client-side caching is performed.

---

## GetServerInfoAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<ServerStatusDto?> GetServerInfoAsync()
```

**Returns:** `Task<ServerStatusDto?>`


Fetches the current server status by issuing an HTTP GET to /api/server/info and deserializing the JSON payload into a ServerStatusDto. This asynchronous wrapper on the underlying HTTP client exposes a strongly-typed, nullable result, allowing callers to await the server status without handling HTTP boilerplate themselves.

## Remarks

Encapsulates the HTTP communication pattern used by ApiClient to talk to the server and convert responses into domain DTOs. By centralizing the endpoint path and JSON deserialization, it keeps UI or service code focused on consuming server data rather than parsing HTTP responses. The nullable return type signals that a caller should be prepared for a null result in edge cases.

## Source Code

```csharp
public async Task<ServerStatusDto?> GetServerInfoAsync()
{
    var info = await _http.GetFromJsonAsync<ServerStatusDto>("/api/server/info");
    return info;
}
```

## Dependencies

- ServerStatusDto

## Dependency APIs (verified signatures)

The REAL, parser-verified API surface of this symbol's collaborators:

- record [`ServerStatusDto`](../../EchoHub.Core/DTOs/ServerDtos.cs.md) (`src/EchoHub.Core/DTOs/ServerDtos.cs`)

## Symbol To Document

- Name: GetServerInfoAsync
- Kind: method
- File: src/EchoHub.Client/Services/ApiClient.cs
- Language: csharp
- ID: bcec1c4a-219c-4212-aa28-cd383baa004a

---

## GetUserProfileAsync
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


Gets the profile data for a specific username by issuing an authenticated GET to the server and deserializing the JSON payload into a UserProfileDto. Use this helper when you need a fully-formed profile object within an authenticated session, instead of hand-assembling the request, handling authentication, status checks, and JSON deserialization yourself.

## Remarks
This method encapsulates the pattern of making an authenticated HTTP request and translating the response into a strongly-typed DTO. By performing URL escaping and centralized success checking, it hides boilerplate concerns from callers and keeps ApiClient cohesive with other API surface methods.

## Notes
- Requires authentication; calling without an authenticated session will fail.
- The username is escaped via Uri.EscapeDataString to ensure a valid URL.
- The JSON payload is deserialized to UserProfileDto; changes to the API or DTO shape can break deserialization or mapping, so consumers should validate results accordingly.

---

## GetValidTokenAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<string?> GetValidTokenAsync()
```

**Returns:** `Task<string?>`


Returns a valid access token, refreshing it if it is expired or about to expire; if no token exists, it returns null. If the token is within 60 seconds of expiry and a refresh token is available, it attempts an asynchronous refresh via RefreshTokenAsync and then returns the (possibly refreshed) token.

## Remarks
Encapsulates the token lifecycle for EchoHub usage by hiding the refresh logic behind a single call and is intended to be used by EchoHubConnection as the SignalR token provider. Relies on internal fields `_accessToken`, `_expiresAt`, and `_refreshToken` to decide whether to refresh. If the refresh fails, the method swallows the exception and returns the current token, leaving authentication failure handling to the caller. There is no synchronization here; concurrent calls may trigger overlapping refresh attempts.

## Notes
- If `_accessToken` is null or empty, the method returns null.
- If there is no valid `_refreshToken`, the method will not attempt a refresh even when near expiry.
- Exceptions thrown by `RefreshTokenAsync` are swallowed; a caller may still receive an expired token if refresh fails.

---

## KickUserAsync
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


KickUserAsync kicks a user from moderation by username by posting an authenticated request to the moderation API, escaping the username in the URL and sending a KickRequest with an optional reason; it throws if the server indicates failure.

## Remarks
This method encapsulates the common moderation action behind the client, abstracting away the HTTP details and ensuring authentication and consistent error handling. It relies on Uri.EscapeDataString to safely include usernames in the route and uses a shared EnsureSuccessAsync step to surface server-side errors as exceptions. By taking a nullable reason, it supports both quick kicks and reason-guided moderation without requiring separate endpoints.

## Example
```csharp
// Example usage
await apiClient.KickUserAsync("spammer42", "Harassment in multiple channels");
```

## Notes
- URL-encoding ensures usernames with special characters don't break the route.
- If reason is null, the KickRequest is created with no reason; the API may treat that as an unvoiced kick.
- This method is asynchronous and will throw on failure; call sites should handle exceptions accordingly.
- It assumes authentication is configured prior to invocation (the call to EnsureAuthenticated()).

---

## LoginAsync
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


LoginAsync authenticates a user by posting a LoginRequest to the server and returning the resulting LoginResponse. It builds the request from the provided username and password, submits it to /api/auth/login, ensures the response indicates success, deserializes the body into a LoginResponse, stores the authentication tokens, and returns the result.

## Remarks
LoginAsync exists to centralize the authentication flow within the client. It guarantees consistent error handling, JSON (de)serialization, and token management, so callers don't have to implement these concerns themselves. By encapsulating the /api/auth/login contract and coordinating with LoginRequest and LoginResponse, it provides a single, testable surface for login behavior and token issuance.

## Example
```csharp
var client = new ApiClient(httpClient);
var result = await client.LoginAsync("alice", "password123");
```

## Notes
- If the response body cannot be deserialized into a LoginResponse or is null, an InvalidOperationException is thrown with the message "Login returned empty response.".
- The method updates the client’s authentication state via SetTokens(result); callers should be aware that this is a side effect that affects subsequent authenticated requests.

---

## LoginWithRefreshTokenAsync
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


Exchanges a refresh token for a new authentication token set by posting a RefreshRequest to the server's refresh endpoint, then stores the resulting tokens and returns the LoginResponse. Use this to quietly renew tokens when an access token expires instead of forcing the user to sign in again.

## Remarks
Encapsulates the token-refresh flow behind ApiClient, hiding HTTP transport details from callers and providing a single point for token persistence. It relies on the server returning a LoginResponse containing the new tokens and uses EnsureSuccessAsync to fail fast on non-success responses. The final call to SetTokens(result) ensures the local credential store stays in sync with the server.

## Example
```csharp
// Example usage: refresh tokens with an existing refresh token
var result = await apiClient.LoginWithRefreshTokenAsync("refresh-token-value");
```

## Notes
- If the server responds with a non-success status, EnsureSuccessAsync will throw and tokens will not be updated.
- If the response content is empty, an InvalidOperationException is thrown to signal an unexpected refresh result.

---

## LogoutAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task LogoutAsync()
```

**Returns:** `Task`


LogoutAsync performs a best-effort logout sequence. If a refresh token exists, it sends a POST to /api/auth/logout with a RefreshRequest payload to invalidate the session on the server. The call is wrapped in a try/catch that swallows any exception, so failures to reach or authenticate with the server do not propagate to callers. After the attempt (whether it succeeded or not), the method clears the local authentication state by nulling _accessToken and _refreshToken and by removing the Authorization header from the HttpClient, ensuring subsequent requests are made unauthenticated.

## Remarks
Centralizes logout behavior in the API client so callers don’t need to duplicate token cleanup logic or remember to invalidate the server-side session. It prioritizes a clean client state and resilience to network/server failures, at the cost of not surfacing server logout failures to the caller.

## Example
```csharp
await apiClient.LogoutAsync();
```

## Notes
- The server logout attempt is best-effort; exceptions during the call are swallowed, so logout failure does not throw.
- If there is no refresh token, the method still clears client-side tokens and the Authorization header.
- After logout, _accessToken and _refreshToken are null and the HttpClient Authorization header is cleared, which prevents authenticated requests until a new login occurs.

---

## MuteUserAsync
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


MuteUserAsync mutes a specific user by sending a moderation request to the server. It requires authentication and posts a MuteRequest to the moderation endpoint /api/moderation/mute/{username} (with the username URL-escaped); the operation completes when the server responds with success. The method accepts a username, plus optional durationMinutes and reason values that are serialized into the request payload.

## Remarks
This is a small client wrapper around a backend moderation API. It encapsulates the HTTP details (URL construction, payload shape) and uniform success handling, so callers can express the intent to mute without dealing with HTTP boilerplate. Requiring authentication up front enforces that only authorized users can perform moderation actions.

## Example
```csharp
// Mute a user for 60 minutes with a reason
await apiClient.MuteUserAsync("john_doe", durationMinutes: 60, reason: "spamming");
```

## Notes
- Passing null for durationMinutes or reason results in those fields being omitted from the request payload; server-side handling will determine the resulting mute.

---

## NukeChannelAsync
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


NukeChannelAsync deletes a moderation channel by name via the API. It executes within an authenticated context, building a DELETE request to /api/moderation/channels/{Uri.EscapeDataString(channelName)}/nuke and verifying the server reports success. Call this when you need to purge or reset a channel through the moderation backend instead of issuing raw HTTP requests yourself.

## Remarks
Encapsulates the remote operation behind a single API client method, shielding callers from HTTP details and standardizing authentication and error handling. The channel name is escaped to prevent route-parameter issues, and the response is disposed promptly via the using statement, with success validated by EnsureSuccessAsync.

## Example
```csharp
await apiClient.NukeChannelAsync("general");
```

## Notes
- This operation is destructive; nuking a channel may purge data associated with it. Use with care.
- Exceptions may be thrown for authentication failures or non-success responses; callers should handle accordingly.
- The method uses Uri.EscapeDataString to safely encode the channel name in the URL.

---

## RefreshTokenAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task RefreshTokenAsync()
```

**Returns:** `Task`


RefreshTokenAsync exchanges the current refresh token for new access and refresh tokens by posting a RefreshRequest to the server at /api/auth/refresh. It requires a non-empty refresh token; if absent, it throws an InvalidOperationException. On a successful HTTP response, it reads a LoginResponse from the body and applies the new tokens through SetTokens.

## Remarks
Token refresh is implemented inside the API client to centralize authentication concerns and to ensure consistent token state management across the application. It encapsulates the HTTP call, response validation, and state update behind a single call, so callers don't manage tokens directly. This design reduces the surface area where token lifetimes are handled and makes token-related behavior easier to test.

## Example
```csharp
// Common usage: refresh tokens when the access token has expired
await apiClient.RefreshTokenAsync();
```

## Notes
- The method throws an InvalidOperationException if there is no refresh token available.
- If the server returns no content for the LoginResponse, an InvalidOperationException is thrown.
- Non-success HTTP status codes are surfaced through EnsureSuccessAsync as part of the refresh flow.

---

## RegisterAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<LoginResponse> RegisterAsync(string username, string password, string? displayName = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string` | — |
| `password` | `string` | — |
| `displayName` | `string?` | `null` |

**Returns:** `Task<LoginResponse>`


Registers a new user by posting the provided credentials to the server, validates the HTTP response, and returns the LoginResponse while persisting authentication tokens. Use this when a user completes the signup flow, encapsulating the HTTP call, response handling, and token storage behind a single API.

## Remarks
Encapsulates the registration workflow, hiding the details of HTTP communication and JSON (de)serialization behind a simple method. It delegates the token management to SetTokens, ensuring that a successful registration immediately makes the tokens available for subsequent authenticated calls.

## Notes
- If the server response cannot be read as a LoginResponse, the method throws an InvalidOperationException with a message indicating an empty response.
- On a successful response, the method mutates client state by calling SetTokens(result) to persist authentication tokens for future requests.

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


Sends a URL to a channel by posting a SendUrlRequest to the server after ensuring the caller is authenticated, and returns the resulting MessageDto when the operation succeeds. Use this method when you need to programmatically share a link into a specific channel from the client, with an optional size parameter that influences server-side handling of the URL.

## Remarks
High-level encapsulation of the HTTP interaction: the method constructs the endpoint path by escaping the channel name, sends a JSON payload containing the URL, and performs a standard success check before deserializing the response. By centralizing authentication, URL escaping, and error handling, it provides a stable, reusable surface for posting URLs into channels and keeps the caller focused on business intent rather than transport details.

## Notes
- Returns null if the response body does not contain a MessageDto (the return type is MessageDto?), so callers should handle null.
- If the server reports an error, EnsureSuccessAsync will throw; callers should handle exceptions as part of error handling.
- The optional size parameter is appended as a query string (?size=value) and only included when size is non-null.

---

## SetTokens
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


Updates the client's authentication state using the provided LoginResponse: it stores the access token, refresh token, and expiration, configures the HttpClient to send the access token with every request, and notifies listeners that tokens have been refreshed. Call this after a successful login or when tokens are renewed to ensure subsequent API calls carry the current Bearer token.

## Remarks

By centralizing token handling in SetTokens, the ApiClient guarantees a single consistent source of truth for credentials. The HTTP layer is updated immediately so that subsequent requests include the latest token, and the OnTokensRefreshed event provides a hook for persisting tokens or updating UI without scattering token-management logic elsewhere.

## Notes

- This method assumes a non-null LoginResponse with valid Token, RefreshToken, and ExpiresAt values; callers should ensure the result is well-formed before invoking.
- Overwrites the Authorization header on the HttpClient; if there are other authentication schemes in use, this behavior should be intentional.

---

## UnbanUserAsync
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


UnbanUserAsync unbans a user by issuing a POST request to the moderation unban endpoint for the supplied username, after ensuring the caller is authenticated. It escapes the username for URL safety, sends an empty payload, and awaits confirmation of success.

## Remarks
Leverages the client's authentication and error-handling conventions to provide a single, discoverable method for unbanning users. Centralizing this operation avoids duplicating URL construction or HTTP-call boilerplate across the codebase and aligns unban behavior with other moderation actions.

## Notes
- Throws when authentication fails or the response signals failure; exceptions propagate to callers to enable consistent error handling.
- Username is URL-escaped to prevent malformed paths; server-side validation remains the responsibility of the API.

---

## UnmuteUserAsync
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


UnmuteUserAsync unmutes a previously muted user by issuing an authenticated POST request to the server's moderation API. It URL-escapes the provided username, posts an empty payload to /api/moderation/unmute/{username}, and awaits the result, completing the returned Task when the operation succeeds or throwing on error.

## Remarks
Ensures the caller is authenticated before performing the request. The username is encoded with Uri.EscapeDataString to safely handle spaces and special characters in the URL. The response is validated via EnsureSuccessAsync, so non-success HTTP status codes surface as exceptions to the caller. This method centralizes moderation action semantics in the API client, avoiding repetitive HTTP boilerplate across moderation endpoints.

## Notes
- Authentication is required; if the caller is not authenticated, the method will fail fast via EnsureAuthenticated.
- The request payload is an empty object; the endpoint relies on the username in the URL path.
- The method returns a Task; there is no result to inspect on success, and exceptions indicate failure.


---

## UpdateChannelTopicAsync
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


Updates the topic for a channel on the server using the authenticated client. The method builds an UpdateTopicRequest from the provided topic (null clears the topic), sends a PUT request to /api/channels/{channelName}/topic with JSON payload, validates the response, and returns the updated ChannelDto parsed from the response body. If the topic is null, the server clears the topic. The call returns null if the response body is empty. This method encapsulates authentication, request serialization, and JSON deserialization, so callers do not need to assemble HTTP requests manually.

## Remarks
Encapsulates authentication, HTTP communication, and JSON (de)serialization behind a thin, strongly-typed surface. The UpdateTopicRequest payload and ChannelDto response keep business logic separate from transport concerns. The Uri.EscapeDataString usage prevents path traversal and encoding issues when channel names contain special characters.

## Notes
- Passing null clears the channel's topic on the server; non-null sets a new topic.
- Topic content is sent as JSON via PUT; this operation is idempotent for a given topic value.

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


Updates the authenticated user's profile by sending the provided UpdateProfileRequest to the server via a JSON PUT to /api/users/profile. It requires authentication, validates the HTTP response, and returns the updated UserProfileDto from the response body when available.

## Remarks
Encapsulates a common server interaction: updating profile data from the client while enforcing authentication and consistent error handling. The method flow—ensure the user is authenticated, perform an authenticated HTTP PUT, verify success, then deserialize the response—isolates HTTP concerns from business logic. This wrapper is particularly suited for profile-management UI flows where the client needs to reflect server-side updates reliably.

## Notes
- Requires an authenticated user; EnsureAuthenticated() runs before issuing the HTTP call.
- The server response is read as JSON into UserProfileDto; if the response has no content, the returned value may be null.
- The PUT call uses PutAsJsonAsync to send the UpdateProfileRequest payload to /api/users/profile; the payload shape must align with the server API expectations.

---

## UploadAvatarAsync
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


Uploads a user avatar by sending the provided image stream as a multipart/form-data POST to the server after ensuring the user is authenticated. It returns the AvatarAscii value from the server response (or null if the response doesn't include one), encapsulating HTTP construction, authentication, and response parsing behind a straightforward async API.

## Remarks

Purpose: centralizes the avatar upload workflow in the client so callers don't need to assemble HTTP requests themselves. It relies on the authentication infrastructure (EnsureAuthenticated and AuthenticatedRequestAsync) and uses a server-provided AvatarUploadResponse to surface the avatar representation. The method derives the content type from the filename and transmits the image as a multipart form field named "file", keeping the details of HTTP multiplexing hidden from callers.

## Example

```csharp
// Common usage: upload a local avatar image and obtain its ASCII representation
using var stream = File.OpenRead("path/to/avatar.png");
string? ascii = await apiClient.UploadAvatarAsync(stream, "avatar.png");
```

## Notes

- The input image stream is disposed by this method as part of disposing the HttpContent; callers should not reuse the stream after the call.
- The return value is nullable; if the server does not provide AvatarAscii, this method returns null.
- This method requires authentication; a failure to authenticate will typically throw from EnsureAuthenticated/AuthenticatedRequestAsync.

---

## UploadFileAsync
> **File:** `src/EchoHub.Client/Services/ApiClient.cs`  
> **Kind:** method

```csharp
public async Task<MessageDto?> UploadFileAsync(string channelName, Stream fileStream, string fileName, string? size = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `fileStream` | `Stream` | — |
| `fileName` | `string` | — |
| `size` | `string?` | `null` |

**Returns:** `Task<MessageDto?>`


Uploads a file to a specific channel by sending a multipart/form-data request to the server. It authenticates the current user, attaches the provided file stream as a file named fileName under the field "file", and returns the server's MessageDto payload (or null if the response has no content).

## Remarks
This method centralizes the mechanics of uploading a file to a channel: it handles authentication, marshals the file into a multipart form body, and deserializes the server's response into a typed MessageDto. The MIME type is inferred from the fileName via GetContentType(fileName), the channel name is URL-encoded for the request path, and an optional size query parameter is appended when size is provided. After posting, the response is validated for success and deserialized as MessageDto.

## Example
```csharp
// Example usage
using var stream = File.OpenRead("path/to/file.png");
var message = await apiClient.UploadFileAsync("general", stream, "file.png");
```

## Notes
- The provided stream is disposed as part of the HTTP content lifecycle; do not reuse the same stream after the call.
- The Content-Type is derived from the fileName; ensure the file extension maps to an appropriate MIME type in GetContentType.
- The return type is MessageDto?; if the server returns no content or a non-JSON body, deserialization may yield null or throw, so callers should handle the potential null result gracefully.

---