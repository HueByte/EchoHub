using Serilog;
using Terminal.Gui.App;

namespace EchoHub.Client;

/// <summary>
/// Eliminates repeated Task.Run/try/catch/app.Invoke(ShowError) boilerplate.
/// Runs async work on a background thread and routes exceptions to the UI.
/// </summary>
public static class AsyncRunner
{
    public static void Run(
        IApplication app,
        Func<Task> work,
        Action<string> showError,
        string errorPrefix,
        string? logContext = null)
    {
        Task.Run(async () =>
        {
            try
            {
                await work();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{Context} failed", logContext ?? errorPrefix);
                app.Invoke(() => showError($"{errorPrefix}: {ex.Message}"));
            }
        });
    }
}
