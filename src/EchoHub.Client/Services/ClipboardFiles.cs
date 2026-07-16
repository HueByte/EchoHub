using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Serilog;

namespace EchoHub.Client.Services;

/// <summary>
/// Reads file paths that live on the OS clipboard as a *file list* (e.g. after copying a file in
/// Explorer/Finder/Nautilus), which terminals do not paste as text. Lets Ctrl+V attach a copied
/// file directly instead of requiring the user to paste a raw path.
/// </summary>
public static class ClipboardFiles
{
    public static bool TryGetFiles(out List<string> files)
    {
        files = [];
        try
        {
            if (OperatingSystem.IsWindows())
                return TryGetWindows(out files);
            if (OperatingSystem.IsLinux())
                return TryGetLinux(out files);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Reading files from the clipboard failed");
        }

        // macOS and everything else: no file-list clipboard support (text paste still works).
        return false;
    }

    // ── Windows: CF_HDROP via the Win32 clipboard ────────────────────────────

    private const uint CfHdrop = 15;

    [SupportedOSPlatform("windows")]
    private static bool TryGetWindows(out List<string> files)
    {
        files = [];
        if (!IsClipboardFormatAvailable(CfHdrop))
            return false;

        // The clipboard may briefly be held by another process; a few quick retries cover that.
        var opened = false;
        for (var attempt = 0; attempt < 5 && !opened; attempt++)
            opened = OpenClipboard(IntPtr.Zero);
        if (!opened)
            return false;

        try
        {
            var hDrop = GetClipboardData(CfHdrop);
            if (hDrop == IntPtr.Zero)
                return false;

            var count = DragQueryFileW(hDrop, 0xFFFFFFFF, null, 0);
            for (uint i = 0; i < count; i++)
            {
                var len = DragQueryFileW(hDrop, i, null, 0);
                if (len == 0)
                    continue;

                var sb = new StringBuilder((int)len + 1);
                DragQueryFileW(hDrop, i, sb, (uint)sb.Capacity);
                var path = sb.ToString();
                if (File.Exists(path))
                    files.Add(path);
            }

            return files.Count > 0;
        }
        finally
        {
            CloseClipboard();
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint DragQueryFileW(IntPtr hDrop, uint iFile, StringBuilder? lpszFile, uint cch);

    // ── Linux: text/uri-list from the clipboard via xclip or wl-paste ─────────

    [SupportedOSPlatform("linux")]
    private static bool TryGetLinux(out List<string> files)
    {
        files = [];

        var output = RunForOutput("wl-paste", ["--type", "text/uri-list", "--no-newline"])
            ?? RunForOutput("xclip", ["-selection", "clipboard", "-t", "text/uri-list", "-o"]);
        if (string.IsNullOrWhiteSpace(output))
            return false;

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!line.StartsWith("file://", StringComparison.Ordinal))
                continue;
            try
            {
                var path = new Uri(line).LocalPath;
                if (File.Exists(path))
                    files.Add(path);
            }
            catch (UriFormatException) { /* skip malformed entry */ }
        }

        return files.Count > 0;
    }

    private static string? RunForOutput(string fileName, IEnumerable<string> args)
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
                return null;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(2000);
            return process.ExitCode == 0 ? output : null;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            return null; // tool not installed
        }
    }
}
