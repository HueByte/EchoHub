using Serilog;

namespace EchoHub.Client.Services;

/// <summary>
/// Ensures the application's directory is on the system PATH so users
/// can run 'echohub' from any terminal session.
/// </summary>
public static class PathSetup
{
    private const string PathMarker = "# Added by EchoHub";

    /// <summary>
    /// Checks if the app directory is on PATH; if not, adds it persistently.
    /// On Windows: modifies user-level PATH environment variable.
    /// On Linux/macOS: appends an export line to shell profile files.
    /// </summary>
    public static void EnsureOnPath()
    {
        try
        {
            var appDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (IsOnPath(appDir))
                return;

            if (OperatingSystem.IsWindows())
                AddToWindowsPath(appDir);
            else
                AddToUnixPath(appDir);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not add app directory to PATH");
        }
    }

    private static bool IsOnPath(string directory)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        var separator = OperatingSystem.IsWindows() ? ';' : ':';

        return pathVar
            .Split(separator, StringSplitOptions.RemoveEmptyEntries)
            .Any(p => string.Equals(
                p.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                directory,
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal));
    }

    private static void AddToWindowsPath(string directory)
    {
        var userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";

        // Double-check against user PATH specifically (process PATH includes system + user)
        if (userPath.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Any(p => string.Equals(p.TrimEnd('\\', '/'), directory, StringComparison.OrdinalIgnoreCase)))
            return;

        var newPath = string.IsNullOrEmpty(userPath) ? directory : userPath + ";" + directory;
        Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
        Log.Information("Added {Directory} to user PATH", directory);
    }

    private static void AddToUnixPath(string directory)
    {
        var exportLine = $"export PATH=\"{directory}:$PATH\" {PathMarker}";
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Target the most common shell profiles
        string[] profiles = [
            Path.Combine(home, ".profile"),
            Path.Combine(home, ".bashrc"),
            Path.Combine(home, ".zshrc")
        ];

        var added = false;
        foreach (var profile in profiles)
        {
            if (!File.Exists(profile))
                continue;

            var content = File.ReadAllText(profile);
            if (content.Contains(directory))
                continue; // Already present (manual or previous run)

            File.AppendAllText(profile, $"\n{exportLine}\n");
            added = true;
            Log.Information("Added PATH export to {Profile}", profile);
        }

        // If no profile existed, create .profile
        if (!added)
        {
            var fallback = Path.Combine(home, ".profile");
            File.AppendAllText(fallback, $"\n{exportLine}\n");
            Log.Information("Created PATH export in {Profile}", fallback);
        }
    }
}
