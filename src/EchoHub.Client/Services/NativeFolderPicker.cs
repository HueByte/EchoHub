using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Serilog;

namespace EchoHub.Client.Services;

public enum PickerOutcome
{
    /// <summary>The user picked a folder (<see cref="FolderPickResult.Path"/> is set).</summary>
    Chosen,

    /// <summary>The native dialog ran but the user cancelled it.</summary>
    Cancelled,

    /// <summary>No native picker is available on this machine (headless, missing tool, etc.).</summary>
    Unavailable,
}

public sealed record FolderPickResult(PickerOutcome Outcome, string? Path);

/// <summary>
/// Opens the OS-native folder chooser (Windows Explorer, macOS Finder, Linux GTK/KDE) by shelling
/// out, so the TUI doesn't need a GUI toolkit reference. Returns <see cref="PickerOutcome.Unavailable"/>
/// when no native dialog can run, so callers can fall back to a configured path.
/// </summary>
public static class NativeFolderPicker
{
    private const string Title = "Choose your EchoHub download folder";

    public static async Task<FolderPickResult> PickFolderAsync(string? initialDir)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return await PickWindowsAsync(initialDir);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return await PickMacAsync();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return await PickLinuxAsync(initialDir);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Native folder picker failed");
        }

        return new FolderPickResult(PickerOutcome.Unavailable, null);
    }

    private static async Task<FolderPickResult> PickWindowsAsync(string? initialDir)
    {
        var safeInit = (initialDir ?? string.Empty).Replace("'", "''");
        var script = $$"""
            Add-Type -AssemblyName System.Windows.Forms
            $d = New-Object System.Windows.Forms.FolderBrowserDialog
            $d.Description = '{{Title}}'
            $d.ShowNewFolderButton = $true
            $d.SelectedPath = '{{safeInit}}'
            if ($d.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) { [Console]::Out.Write($d.SelectedPath) }
            """;

        // -EncodedCommand avoids all quoting issues; FolderBrowserDialog needs an STA thread.
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        var (started, _, stdout) = await RunAsync("powershell.exe",
            ["-STA", "-NoProfile", "-NonInteractive", "-EncodedCommand", encoded]);

        if (!started)
            return new FolderPickResult(PickerOutcome.Unavailable, null);
        return string.IsNullOrWhiteSpace(stdout)
            ? new FolderPickResult(PickerOutcome.Cancelled, null)
            : new FolderPickResult(PickerOutcome.Chosen, stdout);
    }

    private static async Task<FolderPickResult> PickMacAsync()
    {
        var (started, exit, stdout) = await RunAsync("osascript",
            ["-e", $"POSIX path of (choose folder with prompt \"{Title}\")"]);

        if (!started)
            return new FolderPickResult(PickerOutcome.Unavailable, null);
        return exit == 0 && !string.IsNullOrWhiteSpace(stdout)
            ? new FolderPickResult(PickerOutcome.Chosen, stdout)
            : new FolderPickResult(PickerOutcome.Cancelled, null);
    }

    private static async Task<FolderPickResult> PickLinuxAsync(string? initialDir)
    {
        // No graphical session → no native picker.
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"))
            && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")))
            return new FolderPickResult(PickerOutcome.Unavailable, null);

        var zenityArgs = new List<string> { "--file-selection", "--directory", $"--title={Title}" };
        if (!string.IsNullOrWhiteSpace(initialDir))
            zenityArgs.Add($"--filename={initialDir!.TrimEnd('/')}/");

        var (zStarted, zExit, zOut) = await RunAsync("zenity", zenityArgs);
        if (zStarted)
            return zExit == 0 && !string.IsNullOrWhiteSpace(zOut)
                ? new FolderPickResult(PickerOutcome.Chosen, zOut)
                : new FolderPickResult(PickerOutcome.Cancelled, null);

        var (kStarted, kExit, kOut) = await RunAsync("kdialog",
            ["--getexistingdirectory", string.IsNullOrWhiteSpace(initialDir) ? "." : initialDir!]);
        if (kStarted)
            return kExit == 0 && !string.IsNullOrWhiteSpace(kOut)
                ? new FolderPickResult(PickerOutcome.Chosen, kOut)
                : new FolderPickResult(PickerOutcome.Cancelled, null);

        return new FolderPickResult(PickerOutcome.Unavailable, null);
    }

    private static async Task<(bool Started, int ExitCode, string StdOut)> RunAsync(string fileName, IEnumerable<string> args)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        try
        {
            using var process = Process.Start(psi);
            if (process is null)
                return (false, -1, string.Empty);

            var stdout = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            return (true, process.ExitCode, stdout.Trim());
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            // Executable not found on PATH → treat as "no native picker".
            return (false, -1, string.Empty);
        }
    }
}
