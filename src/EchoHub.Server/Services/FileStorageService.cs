namespace EchoHub.Server.Services;

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
