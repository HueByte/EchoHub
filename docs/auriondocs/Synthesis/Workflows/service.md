# Adding a new service

> *Workflow template auto-derived from 8 existing exemplar(s).*

Adding a new service

When you need to add reusable, application-level behaviour (file handling, message processing, background tasks, etc.), add a new service type and connect it to the existing wiring points. This pattern shows a concrete reference implementation you can model, where to place the new type, and which files in the repository were detected as the likely registration/consumption sites.

## Reference implementation

```csharp
public class FileStorageService
{
    private readonly string _storagePath;

    public FileStorageService(IConfiguration configuration)
    {
        _storagePath = configuration["Storage:Path"]
            ?? Path.Combine(AppContext.BaseDirectory, "uploads");

        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task<(string fileId, string filePath)> SaveFileAsync(Stream stream, string fileName)
    {
        var fileId = Guid.NewGuid().ToString();
        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{fileId}{extension}";
        var filePath = Path.Combine(_storagePath, storedFileName);

        using var fileStream = File.Create(filePath);
        await stream.CopyToAsync(fileStream);

        return (fileId, filePath);
    }

    public string? GetFilePath(string fileId)
    {
        var files = Directory.GetFiles(_storagePath, $"{fileId}.*");

        return files.Length > 0 ? files[0] : null;
    }

    public void DeleteFile(string fileId)
    {
        var filePath = GetFilePath(fileId);

        if (filePath is not null && File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
```

## Where it lives

Service types are placed in the src/EchoHub.Server/Services folder and follow the naming pattern shown by the existing types: a descriptive name followed by the Service suffix (for example, ChannelService, ChatService, FileStorageService). Use that folder and the Service suffix when adding a new service class.

## Wiring

The symbol-graph detected the following files as wiring sites where services are registered or consumed: src/EchoHub.Server/Program.cs and src/EchoHub.Server/Controllers/ChannelsController.cs. Open those files to see how existing services are registered or injected and to add the new service registration or constructor consumption; these are the only wiring-site files named by the detection step.

## Existing examples

- [`ChannelService`](../../Code/src/EchoHub.Server/Services/ChannelService.cs.md)
- [`ChatService`](../../Code/src/EchoHub.Server/Services/ChatService.cs.md)
- [`FileCleanupService`](../../Code/src/EchoHub.Server/Services/FileCleanupService.cs.md)
- [`FileStorageService`](../../Code/src/EchoHub.Server/Services/FileStorageService.cs.md)
- [`ImageToAsciiService`](../../Code/src/EchoHub.Server/Services/ImageToAsciiService.cs.md)
- [`LinkEmbedService`](../../Code/src/EchoHub.Server/Services/LinkEmbedService.cs.md)
- [`MessageEncryptionService`](../../Code/src/EchoHub.Server/Services/MessageEncryptionService.cs.md)
- [`MuteExpirationService`](../../Code/src/EchoHub.Server/Services/MuteExpirationService.cs.md)

---
*Synthesised by Aurion on 2026-07-08 17:10:23 UTC*
