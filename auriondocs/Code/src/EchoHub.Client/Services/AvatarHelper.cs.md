# AvatarHelper

> **File:** `src/EchoHub.Client/Services/AvatarHelper.cs`  
> **Kind:** class

```csharp
internal static class AvatarHelper
```


AvatarHelper centralizes the shared logic for uploading avatars by accepting either a local file path or an HTTP(S) URL, resolving the input to a stream, and uploading it via ApiClient.UploadAvatarAsync. It returns the ASCII art response from the server, providing a straightforward way to obtain the server-side representation of the uploaded avatar without duplicating local-file or network-handling code.

## Remarks

By supporting both local and remote sources behind a single UploadAsync entry point, AvatarHelper hides the mechanics of data retrieval and stream management from call sites and ensures consistent disposal of the stream. The actual upload is delegated to ApiClient, keeping concerns separated between data acquisition and server interaction. The class is internal, reinforcing its role as a reusable utility within the client layer rather than a public API.

The method propagates errors from file access, HTTP fetch, or the server upload to the caller, which is appropriate for a small, focused helper that prioritizes simplicity over internal retries or resilience policies.

## Example

```csharp
// Example usage within the same assembly
var client = new ApiClient("https://api.example.org");
string? artFromFile = await AvatarHelper.UploadAsync(client, @"C:\avatars\user.png");
string? artFromUrl  = await AvatarHelper.UploadAsync(client, "https://example.org/avatars/user.png");
```

## Notes

- Creating a new HttpClient per invocation can lead to socket exhaustion in high-throughput scenarios; consider reusing a shared HttpClient instance or using HttpClientFactory in production code.
- If the local path does not exist, a FileNotFoundException is thrown.
- When targeting a URL, if the URL's file name is missing or lacks an extension, the code defaults to using avatar.png as the upload file name.
- Exceptions from the HTTP request or the server upload propagate to the caller; there is no retry logic within this helper.