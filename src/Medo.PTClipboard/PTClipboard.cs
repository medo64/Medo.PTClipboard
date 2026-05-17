/* Josip Medved <jmedved@jmedved.com> * www.medo64.com * MIT License */

//2025-10-18: Initial version

namespace Medo;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

/// <summary>
/// Plain-text clipboard handling operations.
/// All methods are thread safe.
/// </summary>
public static class PTClipboard {

    /// <summary>
    /// Gets normal clipboard.
    /// </summary>
    public static PTMainClipboard Main {
        get {
            lock (ClipboardLock) {
                GetClipboards(out var main, out _);
                return main;
            }
        }
    }

    /// <summary>
    /// Gets primary (selection) clipboard.
    /// </summary>
    public static PTSelectionClipboard Selection {
        get {
            lock (ClipboardLock) {
                GetClipboards(out _, out var selection);
                return selection;
            }
        }
    }

    /// <summary>
    /// Clears the clipboard.
    /// It will clear both main and selection clipboard if available.
    /// </summary>
    public static void Clear() {
        lock (ClipboardLock) {
            GetClipboards(out var main, out var selection);
            if (selection.IsAvailable) { selection.Clear(); }
            if (main.IsAvailable) { main.Clear(); }
        }
    }

    /// <summary>
    /// Returns clipboard text data.
    /// Empty string is returned if the clipboard does not contain UTF-8 string.
    /// </summary>
    public static string GetText() {
        lock (ClipboardLock) {
            GetClipboards(out var main, out var selection);
            if (main.IsAvailable) {
                var text = main.GetText();
                if (text is not null && (text.Length > 0)) { return text; }
            }
            if (selection.IsAvailable) {  // use selection if nothing in main clipboard
                return selection.GetText() ?? string.Empty;
            }
            return string.Empty;
        }
    }

    /// <summary>
    /// Sets text data in UTF-8 format.
    /// </summary>
    /// <param name="text">Text.</param>
    public static void SetText(string text) {
        lock (ClipboardLock) {
            GetClipboards(out var main, out var selection);
            if (selection.IsAvailable) { selection.SetText(text); }
            if (main.IsAvailable) { main.SetText(text); }
        }
    }


    private static readonly Lock ClipboardLock = new();
    private static PTClipboardProvider? Provider;
    private static PTMainClipboard? MainClipboard;
    private static PTSelectionClipboard? SelectionClipboard;

    private static void GetClipboards(out PTMainClipboard main, out PTSelectionClipboard selection) {
        if (Provider is null || MainClipboard is null || SelectionClipboard is null) {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                try {
                    Provider = new PTClipboardWin32Provider();
                    Debug.WriteLine($"[PTClipboard] Using Win32 clipboard provider");
                } catch (NotSupportedException ex) {
                    Provider = new PTClipboardFallbackProvider();
                    Trace.WriteLine($"[PTClipboard] Using fallback clipboard provider due to error ({ex.Message})");
                }
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                try {
                    var waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") ?? "";
                    var xdgSessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") ?? "";
                    var isWaylandDisplay = waylandDisplay.StartsWith("wayland-", StringComparison.OrdinalIgnoreCase);
                    var isWaylandSessionType = xdgSessionType.Equals("wayland", StringComparison.OrdinalIgnoreCase);
                    if (isWaylandDisplay || isWaylandSessionType) {
                        Provider = new PTClipboardX11Provider();  // TODO: Wayland clipboard provider
                        Debug.WriteLine($"[PTClipboard] Using Wayland clipboard provider");
                    } else {
                        Provider = new PTClipboardX11Provider();
                        Debug.WriteLine($"[PTClipboard] Using X11 clipboard provider");
                    }
                } catch (NotSupportedException ex) {
                    Provider = new PTClipboardFallbackProvider();
                    Trace.WriteLine($"[PTClipboard] Using fallback clipboard provider due to error ({ex.Message})");
                }
            } else {
                Provider = new PTClipboardFallbackProvider();
                Trace.WriteLine($"[PTClipboard] Using fallback clipboard provider for unsupported platform");
            }
            MainClipboard = new PTMainClipboard(Provider, ClipboardLock);
            SelectionClipboard = new PTSelectionClipboard(Provider, ClipboardLock);
        }
        main = MainClipboard;
        selection = SelectionClipboard;
    }

}
