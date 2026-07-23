# AvatarHelper

> **File:** `src/EchoHub.Client/Services/AvatarHelper.cs`  
> **Kind:** class

```csharp
internal static class AvatarHelper
```


AvatarHelper provides a single entry point to upload an avatar from either a local file path or a remote URL by converting the target into a `Stream`, then delegating the actual upload to `ApiClient.UploadAvatarAsync`. It abstracts away the file I/O and HTTP fetch logic, ensuring callers don't need to manage streams or HTTP requests themselves. It returns the server's ASCII art response as a `string?` and guarantees the `Stream` is disposed after the upload.

## Remarks
AvatarHelper isolates avatar uploading behind a focused API, so higher-level code doesn't need to know whether the source is a local file or a URL. It accepts either a local path or an HTTP(S) URL, resolves a valid `fileName` (defaulting to `avatar.png` when the URL doesn't supply one), and streams the content to `ApiClient.UploadAvatarAsync`. The helper ensures proper resource management by disposing the `Stream` after the upload, and it centralizes the cross-cutting concern of avatar uploads to a single place.