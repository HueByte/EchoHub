namespace EchoHub.Server.Services;

public sealed class FileCleanupService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileCleanupService> _logger;

    public FileCleanupService(IConfiguration configuration, ILogger<FileCleanupService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = _configuration.GetValue("Storage:CleanupIntervalHours", 1);
        var retentionDays = _configuration.GetValue("Storage:RetentionDays", 30);
        var storagePath = _configuration["Storage:Path"]
            ?? Path.Combine(AppContext.BaseDirectory, "uploads");

        _logger.LogInformation(
            "File cleanup service started — interval: {Hours}h, retention: {Days}d, path: {Path}",
            intervalHours, retentionDays, storagePath);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);

            try
            {
                CleanupOldFiles(storagePath, retentionDays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during file cleanup");
            }
        }
    }

    private void CleanupOldFiles(string storagePath, int retentionDays)
    {
        if (!Directory.Exists(storagePath))
            return;

        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        var files = Directory.GetFiles(storagePath);
        var deleted = 0;

        foreach (var file in files)
        {
            var createdAt = File.GetCreationTimeUtc(file);
            if (createdAt < cutoff)
            {
                try
                {
                    File.Delete(file);
                    deleted++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old file: {File}", file);
                }
            }
        }

        if (deleted > 0)
            _logger.LogInformation("File cleanup: deleted {Count} files older than {Days} days", deleted, retentionDays);
    }
}
