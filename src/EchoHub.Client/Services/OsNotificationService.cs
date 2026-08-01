using OsNotifications;

using Serilog;

namespace EchoHub.Client.Services;

public sealed class OsNotificationService
{
    static OsNotificationService()
    {
        Notifications.SetGuiApplication(true);
    }

    /// <summary>
    /// Shows an OS notification with the given title and optional body. 
    /// </summary>
    /// <param name="title"></param>
    /// <param name="body"></param>
    public void Show(string title, string? body = null)
    {
        try
        {
            Notifications.ShowNotification(title, body ?? string.Empty);
        }
        catch (PlatformNotSupportedException ex)
        {
            Log.Warning(ex, "OS notifications not supported on this platform");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to show OS notification");
        }
    }
}
