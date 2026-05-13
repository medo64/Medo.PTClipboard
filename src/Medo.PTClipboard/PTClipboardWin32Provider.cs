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
internal sealed partial class PTClipboardWin32Provider : PTClipboardProvider {

    internal PTClipboardWin32Provider() {
        try {  // just test if user32 is available
            Native.OpenClipboard(IntPtr.Zero);
            Native.CloseClipboard();
        } catch (DllNotFoundException) {
            throw new NotSupportedException("Cannot load user32.dll");
        }
    }


    private string? SelectionContent;


    #region PTClipboardProvider


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
            if (!Native.EmptyClipboard()) {
                Debug.WriteLine($"[PTClipboard:Win32] ClearClipboard(): EmptyClipboard failed ({Marshal.GetLastPInvokeError()})");
            }
        } finally {
            Native.CloseClipboard();
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
            if (!Native.EmptyClipboard()) {
                Debug.WriteLine($"[PTClipboard:Win32] SetClipboardText(): EmptyClipboard failed ({Marshal.GetLastPInvokeError()})");
                return;
            }

            var bytes = Encoding.Unicode.GetBytes(text + '\0');
            bufferHandle = Native.GlobalAlloc(Native.GMEM_MOVEABLE | Native.GMEM_ZEROINIT, (nuint)bytes.Length);
            if (bufferHandle == IntPtr.Zero) {
                Debug.WriteLine($"[PTClipboard:Win32] SetClipboardText(): GlobalAlloc failed ({Marshal.GetLastPInvokeError()})");
                return;
            }

            var bufferPtr = Native.GlobalLock(bufferHandle);
            if (bufferPtr == IntPtr.Zero) {
                Debug.WriteLine($"[PTClipboard:Win32] SetClipboardText(): GlobalLock failed ({Marshal.GetLastPInvokeError()})");
                return;
            }

            try {
                Marshal.Copy(bytes, 0, bufferPtr, bytes.Length);
            } finally {
                Native.GlobalUnlock(bufferHandle);
            }

            if (Native.SetClipboardData(Native.CF_UNICODETEXT, bufferHandle) == IntPtr.Zero) {
                Debug.WriteLine($"[PTClipboard:Win32] SetClipboardText(): SetClipboardData failed ({Marshal.GetLastPInvokeError()})");
                return;
            }

            bufferHandle = IntPtr.Zero;  // clipboard owns the memory after successful set
        } finally {
            if (bufferHandle != IntPtr.Zero) {
                Native.GlobalFree(bufferHandle);
            }
            Native.CloseClipboard();
        }
    }

    /// <summary>
    /// Gets the clipboard text.
    /// </summary>
    public override string GetClipboardText() {
        if (!TryOpenClipboard()) { return string.Empty; }

        try {
            if (!Native.IsClipboardFormatAvailable(Native.CF_UNICODETEXT)) {
                return string.Empty;
            }

            var handle = Native.GetClipboardData(Native.CF_UNICODETEXT);
            if (handle == IntPtr.Zero) {
                Debug.WriteLine($"[PTClipboard:Win32] GetClipboardText(): GetClipboardData failed ({Marshal.GetLastPInvokeError()})");
                return string.Empty;
            }

            var bufferPtr = Native.GlobalLock(handle);
            if (bufferPtr == IntPtr.Zero) {
                Debug.WriteLine($"[PTClipboard:Win32] GetClipboardText(): GlobalLock failed ({Marshal.GetLastPInvokeError()})");
                return string.Empty;
            }

            try {
                return Marshal.PtrToStringUni(bufferPtr) ?? string.Empty;
            } finally {
                Native.GlobalUnlock(handle);
            }
        } finally {
            Native.CloseClipboard();
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
            if (Native.OpenClipboard(IntPtr.Zero)) {
                return true;
            }
            Thread.Sleep(10);
        }

        Debug.WriteLine($"[PTClipboard:Win32] OpenClipboard failed ({Marshal.GetLastPInvokeError()})");
        return false;
    }

}
