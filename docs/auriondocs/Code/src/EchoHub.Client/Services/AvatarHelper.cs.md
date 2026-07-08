# AvatarHelper

> **File:** `src/EchoHub.Client/Services/AvatarHelper.cs`  
> **Kind:** class

```csharp
internal static class AvatarHelper
```


AvatarHelper centralizes avatar upload: UploadAsync accepts a target that can be a local file path or an HTTP(S) URL, resolves it to a stream, and uploads it via ApiClient, returning the server's ASCII art response. Use this helper to avoid duplicating URL/file handling and to keep upload logic consistent across the codebase.

## Remarks
AvatarHelper isolates I/O concerns (URL vs. filesystem) from the network call, making the upload path testable and reusable. It derives a file name from the URL or local path and applies a safe default (avatar.png) when the URL lacks a name, ensuring the API receives a sensible filename. The decision to perform the HTTP request inline (creating a new HttpClient per call) and to stream the payload directly under a using block emphasizes simplicity and request-scoped resource management; adapters behind ApiClient do the actual upload, enabling mocking during tests.

## Example
```csharp
var api = new ApiClient("https://echohub.example/api");
string? result = await AvatarHelper.UploadAsync(api, "https://example.com/avatar.jpg");
// result contains the ASCII art returned by the server
```

## Notes
- The method may throw FileNotFoundException if the local path does not exist.
- When targeting a URL, the method downloads the content into memory (MemoryStream) using a per-call HttpClient; large avatars could incur high memory usage and repeated HttpClient creation may have performance implications. Consider size limits or reusing HttpClient in a broader hosting context if uploading large avatars frequently.
