using System.Text;
using EchoHub.Server.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EchoHub.Tests;

/// <summary>
/// <see cref="FileStorageService"/> round-trips against a real temp directory: save, resolve by
/// id (extension-agnostic), bulk id scan, and delete.
/// </summary>
public sealed class FileStorageServiceTests : IDisposable
{
    private readonly string _dir;
    private readonly FileStorageService _service;

    public FileStorageServiceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "echohub-filestore-" + Guid.NewGuid().ToString("N"));
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:Path"] = _dir })
            .Build();
        _service = new FileStorageService(config);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    private static Stream StreamOf(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public void Constructor_CreatesStorageDirectory()
    {
        Assert.True(Directory.Exists(_dir));
    }

    [Fact]
    public async Task SaveFile_WritesContentAndReturnsResolvablePath()
    {
        var (fileId, filePath) = await _service.SaveFileAsync(StreamOf("hello world"), "note.txt");

        Assert.True(File.Exists(filePath));
        Assert.Equal("hello world", await File.ReadAllTextAsync(filePath));
        Assert.Equal(filePath, _service.GetFilePath(fileId));
    }

    [Fact]
    public async Task SaveFile_PreservesExtension()
    {
        var (fileId, _) = await _service.SaveFileAsync(StreamOf("x"), "photo.PNG");

        var path = _service.GetFilePath(fileId);

        Assert.NotNull(path);
        Assert.Equal(".PNG", Path.GetExtension(path));
    }

    [Fact]
    public async Task SaveFile_GeneratesDistinctIdsForSameFileName()
    {
        var (id1, _) = await _service.SaveFileAsync(StreamOf("a"), "dup.txt");
        var (id2, _) = await _service.SaveFileAsync(StreamOf("b"), "dup.txt");

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void GetFilePath_UnknownId_ReturnsNull()
    {
        Assert.Null(_service.GetFilePath(Guid.NewGuid().ToString()));
    }

    [Fact]
    public async Task GetStoredFileIds_ReturnsAllSavedIds()
    {
        var (id1, _) = await _service.SaveFileAsync(StreamOf("a"), "a.txt");
        var (id2, _) = await _service.SaveFileAsync(StreamOf("b"), "b.bin");

        var ids = _service.GetStoredFileIds();

        Assert.Contains(id1, ids);
        Assert.Contains(id2, ids);
        Assert.Equal(2, ids.Count);
    }

    [Fact]
    public async Task DeleteFile_RemovesFile()
    {
        var (fileId, filePath) = await _service.SaveFileAsync(StreamOf("gone soon"), "temp.dat");

        _service.DeleteFile(fileId);

        Assert.False(File.Exists(filePath));
        Assert.Null(_service.GetFilePath(fileId));
    }

    [Fact]
    public void DeleteFile_UnknownId_DoesNotThrow()
    {
        var ex = Record.Exception(() => _service.DeleteFile(Guid.NewGuid().ToString()));

        Assert.Null(ex);
    }
}
