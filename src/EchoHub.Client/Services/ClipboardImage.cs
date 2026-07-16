using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Serilog;
using SixLabors.ImageSharp;

namespace EchoHub.Client.Services;

/// <summary>
/// Reads raw image data from the OS clipboard (e.g. an image copied from a browser, or a
/// Win+Shift+S screenshot), which terminals cannot paste as text. Always returns PNG bytes:
/// clipboard PNG data is passed through, clipboard bitmaps (CF_DIB) are re-encoded.
/// </summary>
public static class ClipboardImage
{
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47];

    public static bool TryGetPng(out byte[] png)
    {
        png = [];
        try
        {
            if (OperatingSystem.IsWindows())
                return TryGetWindows(out png);
            if (OperatingSystem.IsLinux())
                return TryGetLinux(out png);
            if (OperatingSystem.IsMacOS())
                return TryGetMacOS(out png);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Reading an image from the clipboard failed");
        }

        return false;
    }

    private static bool IsPng(byte[] data) =>
        data.Length > PngMagic.Length && data.AsSpan(0, PngMagic.Length).SequenceEqual(PngMagic);

    /// <summary>
    /// Converts clipboard DIB bytes (a BITMAPINFOHEADER/V4/V5 + optional palette/masks + pixel
    /// data, i.e. a .bmp file without its 14-byte file header) to PNG. Returns null when the
    /// data is malformed or not decodable as a bitmap.
    /// </summary>
    public static byte[]? DibToPng(byte[] dib)
    {
        if (dib.Length < 40)
            return null;

        var headerSize = BitConverter.ToInt32(dib, 0);
        if (headerSize < 40 || headerSize > dib.Length)
            return null;

        var bitCount = BitConverter.ToUInt16(dib, 14);
        var compression = BitConverter.ToUInt32(dib, 16);
        var clrUsed = BitConverter.ToUInt32(dib, 32);

        // Pixel data offset: file header + info header + color masks + palette.
        // BI_BITFIELDS masks follow a plain 40-byte header; larger headers embed them.
        var maskBytes = headerSize == 40 && compression == 3 ? 12
                      : headerSize == 40 && compression == 6 ? 16
                      : 0;
        var paletteEntries = clrUsed != 0 ? clrUsed
                           : bitCount <= 8 ? 1u << bitCount
                           : 0u;
        var pixelOffset = (uint)(14 + headerSize + maskBytes) + paletteEntries * 4;

        var bmp = new byte[14 + dib.Length];
        bmp[0] = (byte)'B';
        bmp[1] = (byte)'M';
        BitConverter.TryWriteBytes(bmp.AsSpan(2), (uint)bmp.Length);
        BitConverter.TryWriteBytes(bmp.AsSpan(10), pixelOffset);
        dib.CopyTo(bmp, 14);

        try
        {
            using var image = Image.Load(bmp);
            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            return ms.ToArray();
        }
        catch (Exception ex) when (ex is ImageFormatException or InvalidOperationException)
        {
            Log.Warning(ex, "Clipboard DIB could not be decoded as a bitmap");
            return null;
        }
    }

    // ── Windows: "PNG" / "image/png" registered formats, then CF_DIB ─────────

    private const uint CfDib = 8;

    [SupportedOSPlatform("windows")]
    private static bool TryGetWindows(out byte[] png)
    {
        png = [];

        // Browsers register a "PNG" (Chromium) or "image/png" (some apps) clipboard format
        // preserving transparency; CF_DIB is synthesized by Windows for everything else
        // (screenshots, image editors), so together these cover all image sources.
        var pngFormat = RegisterClipboardFormatW("PNG");
        var mimeFormat = RegisterClipboardFormatW("image/png");

        var hasAny = (pngFormat != 0 && IsClipboardFormatAvailable(pngFormat))
                  || (mimeFormat != 0 && IsClipboardFormatAvailable(mimeFormat))
                  || IsClipboardFormatAvailable(CfDib);
        if (!hasAny)
            return false;

        var opened = false;
        for (var attempt = 0; attempt < 5 && !opened; attempt++)
            opened = OpenClipboard(IntPtr.Zero);
        if (!opened)
            return false;

        try
        {
            foreach (var format in new[] { pngFormat, mimeFormat })
            {
                if (format == 0 || !IsClipboardFormatAvailable(format))
                    continue;
                var data = ReadHGlobal(GetClipboardData(format));
                if (data is not null && IsPng(data))
                {
                    png = data;
                    return true;
                }
            }

            if (IsClipboardFormatAvailable(CfDib)
                && ReadHGlobal(GetClipboardData(CfDib)) is { } dib
                && DibToPng(dib) is { } converted)
            {
                png = converted;
                return true;
            }

            return false;
        }
        finally
        {
            CloseClipboard();
        }
    }

    [SupportedOSPlatform("windows")]
    private static byte[]? ReadHGlobal(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
            return null;

        var ptr = GlobalLock(handle);
        if (ptr == IntPtr.Zero)
            return null;

        try
        {
            var size = (int)GlobalSize(handle);
            if (size <= 0)
                return null;
            var data = new byte[size];
            Marshal.Copy(ptr, data, 0, size);
            return data;
        }
        finally
        {
            GlobalUnlock(handle);
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

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint RegisterClipboardFormatW(string lpszFormat);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nuint GlobalSize(IntPtr hMem);

    // ── Linux: image/png via wl-paste or xclip ────────────────────────────────

    [SupportedOSPlatform("linux")]
    private static bool TryGetLinux(out byte[] png)
    {
        png = [];
        var data = RunForBytes("wl-paste", ["--type", "image/png"])
                ?? RunForBytes("xclip", ["-selection", "clipboard", "-t", "image/png", "-o"]);
        if (data is null || !IsPng(data))
            return false;

        png = data;
        return true;
    }

    // ── macOS: pngpaste (brew install pngpaste), when present ────────────────

    [SupportedOSPlatform("macos")]
    private static bool TryGetMacOS(out byte[] png)
    {
        png = [];
        var data = RunForBytes("pngpaste", ["-"]);
        if (data is null || !IsPng(data))
            return false;

        png = data;
        return true;
    }

    private static byte[]? RunForBytes(string fileName, IEnumerable<string> args)
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

            using var ms = new MemoryStream();
            process.StandardOutput.BaseStream.CopyTo(ms);
            process.WaitForExit(2000);
            return process.ExitCode == 0 && ms.Length > 0 ? ms.ToArray() : null;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            return null; // tool not installed
        }
    }
}
