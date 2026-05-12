/* Josip Medved <jmedved@jmedved.com> * www.medo64.com * MIT License */

namespace Medo;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

/// <summary>
/// Windows clipboard handling operations.
/// </summary>
internal sealed class PTClipboardWin32Provider : PTClipboardProvider {

    internal PTClipboardWin32Provider() {
        try {
            NativeMethods.OpenClipboard(IntPtr.Zero);
            NativeMethods.CloseClipboard();
        } catch (DllNotFoundException) {
            return;  // only fail if user32.dll is not found
        }
        IsAvailable = true;
    }


    private string? SelectionContent;


    #region PTClipboardProvider

    /// <summary>
    /// Returns true if provider is available.
    /// </summary>
    public override bool IsAvailable { get; }


    /// <summary>
    /// Gets if clipboard service is available.
    /// </summary>
    public override bool IsClipboardAvailable {
        get { return true; }
    }

    /// <summary>
    /// Clears the clipboard.
    /// </summary>
    public override void ClearClipboard() {
        if (!TryOpenClipboard()) { return; }

        try {
            if (!NativeMethods.EmptyClipboard()) {
                Debug.WriteLine($"[PTClipboard:Win32] ClearClipboard(): EmptyClipboard failed ({Marshal.GetLastPInvokeError()})");
            }
        } finally {
            NativeMethods.CloseClipboard();
        }
    }

    /// <summary>
    /// Sets the clipboard text.
    /// </summary>
    /// <param name="text">Text.</param>
    public override void SetClipboardText(string text) {
        ArgumentNullException.ThrowIfNull(text);
        if (!TryOpenClipboard()) { return; }

        var bufferHandle = IntPtr.Zero;
        try {
            if (!NativeMethods.EmptyClipboard()) {
                Debug.WriteLine($"[PTClipboard:Win32] SetClipboardText(): EmptyClipboard failed ({Marshal.GetLastPInvokeError()})");
                return;
            }

            var bytes = Encoding.Unicode.GetBytes(text + '\0');
            bufferHandle = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE | NativeMethods.GMEM_ZEROINIT, (nuint)bytes.Length);
            if (bufferHandle == IntPtr.Zero) {
                Debug.WriteLine($"[PTClipboard:Win32] SetClipboardText(): GlobalAlloc failed ({Marshal.GetLastPInvokeError()})");
                return;
            }

            var bufferPtr = NativeMethods.GlobalLock(bufferHandle);
            if (bufferPtr == IntPtr.Zero) {
                Debug.WriteLine($"[PTClipboard:Win32] SetClipboardText(): GlobalLock failed ({Marshal.GetLastPInvokeError()})");
                return;
            }

            try {
                Marshal.Copy(bytes, 0, bufferPtr, bytes.Length);
            } finally {
                NativeMethods.GlobalUnlock(bufferHandle);
            }

            if (NativeMethods.SetClipboardData(NativeMethods.CF_UNICODETEXT, bufferHandle) == IntPtr.Zero) {
                Debug.WriteLine($"[PTClipboard:Win32] SetClipboardText(): SetClipboardData failed ({Marshal.GetLastPInvokeError()})");
                return;
            }

            bufferHandle = IntPtr.Zero;  // clipboard owns the memory after successful set
        } finally {
            if (bufferHandle != IntPtr.Zero) {
                NativeMethods.GlobalFree(bufferHandle);
            }
            NativeMethods.CloseClipboard();
        }
    }

    /// <summary>
    /// Gets the clipboard text.
    /// </summary>
    public override string GetClipboardText() {
        if (!TryOpenClipboard()) { return string.Empty; }

        try {
            if (!NativeMethods.IsClipboardFormatAvailable(NativeMethods.CF_UNICODETEXT)) {
                return string.Empty;
            }

            var handle = NativeMethods.GetClipboardData(NativeMethods.CF_UNICODETEXT);
            if (handle == IntPtr.Zero) {
                Debug.WriteLine($"[PTClipboard:Win32] GetClipboardText(): GetClipboardData failed ({Marshal.GetLastPInvokeError()})");
                return string.Empty;
            }

            var bufferPtr = NativeMethods.GlobalLock(handle);
            if (bufferPtr == IntPtr.Zero) {
                Debug.WriteLine($"[PTClipboard:Win32] GetClipboardText(): GlobalLock failed ({Marshal.GetLastPInvokeError()})");
                return string.Empty;
            }

            try {
                return Marshal.PtrToStringUni(bufferPtr) ?? string.Empty;
            } finally {
                NativeMethods.GlobalUnlock(handle);
            }
        } finally {
            NativeMethods.CloseClipboard();
        }
    }


    /// <summary>
    /// Returns true if selection clipboard service is available.
    /// </summary>
    public override bool IsSelectionAvailable {
        get { return false; }
    }

    /// <summary>
    /// Clears the clipboard.
    /// </summary>
    public override void ClearSelection() {
        SelectionContent = null;
    }

    /// <summary>
    /// Sets the selection clipboard text.
    /// </summary>
    /// <param name="text">Text.</param>
    public override void SetSelectionText(string text) {
        SelectionContent = text;
    }

    /// <summary>
    /// Gets the selection clipboard text.
    /// </summary>
    public override string GetSelectionText() {
        return SelectionContent ?? string.Empty;
    }

    #endregion PTClipboardProvider


    private static bool TryOpenClipboard() {
        for (var attempt = 0; attempt < 10; attempt++) {
            if (NativeMethods.OpenClipboard(IntPtr.Zero)) {
                return true;
            }
            Thread.Sleep(10);
        }

        Debug.WriteLine($"[PTClipboard:Win32] OpenClipboard failed ({Marshal.GetLastPInvokeError()})");
        return false;
    }


    private static class NativeMethods {

        internal const uint CF_UNICODETEXT = 13;
        internal const uint GMEM_MOVEABLE = 0x0002;
        internal const uint GMEM_ZEROINIT = 0x0040;

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern Boolean CloseClipboard();

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern Boolean EmptyClipboard();

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr GetClipboardData(UInt32 uFormat);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetClipboardData(UInt32 uFormat, IntPtr hMem);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern Boolean IsClipboardFormatAvailable(uint format);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GlobalAlloc(UInt32 uFlags, nuint dwBytes);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GlobalFree(IntPtr hMem);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GlobalLock(IntPtr hMem);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern Boolean GlobalUnlock(IntPtr hMem);

    }

}
