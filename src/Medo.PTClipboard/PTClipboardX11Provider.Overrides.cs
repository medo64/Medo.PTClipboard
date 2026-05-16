/* Josip Medved <jmedved@jmedved.com> * www.medo64.com * MIT License */

namespace Medo;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

/// <summary>
/// X11 clipboard handling operations.
/// </summary>

partial class PTClipboardX11Provider {

    /// <summary>
    /// Gets if clipboard service is available.
    /// </summary>
    public override bool IsClipboardAvailable => true;

    /// <summary>
    /// Clears the clipboard.
    /// </summary>
    public override void ClearClipboard() {
        ObjectDisposedException.ThrowIf(WasDisposed, this);

        lock (ClipboardBytesOutLock) {
            ClipboardBytesOut = [];
        }
        Native.XSetSelectionOwner(DisplayPtr, ClipboardAtom, IntPtr.Zero, 0);
        Debug.WriteLine($"[PTClipboard:X11] ClearClipboard(): Ownership cleared");
    }

    /// <summary>
    /// Sets the clipboard text.
    /// </summary>
    /// <param name="text">Text.</param>
    public override void SetClipboardText(string text) {
        ObjectDisposedException.ThrowIf(WasDisposed, this);

        lock (ClipboardBytesOutLock) {
            ClipboardBytesOut = Encoding.UTF8.GetBytes(text);
        }
        Native.XSetSelectionOwner(DisplayPtr, ClipboardAtom, WindowPtr, 0);
        Debug.WriteLine($"[PTClipboard:X11] SetText(): Ownership set");
    }

    /// <summary>
    /// Gets the clipboard text.
    /// </summary>
    public override string GetClipboardText() {
        ObjectDisposedException.ThrowIf(WasDisposed, this);

        ClipboardBytesInLock.Reset();  // shouldn't be set but let's make sure
        Native.XConvertSelection(DisplayPtr,
                                        ClipboardAtom,
                                        Utf8StringAtom,
                                        MetaSelectionAtom,
                                        WindowPtr,
                                        IntPtr.Zero);
        Native.XFlush(DisplayPtr);
        Debug.WriteLine($"[PTClipboard:X11] GetText(): Text requested");

        if (ClipboardBytesInLock.WaitOne(100)) {  // don't wait long
            return Encoding.UTF8.GetString(ClipboardBytesIn);
        } else {
            Debug.WriteLine($"[PTClipboard:X11] GetText(): Timeout reading text");
            return string.Empty;
        }
    }


    /// <summary>
    /// Returns true if selection clipboard service is available.
    /// </summary>
    public override bool IsSelectionAvailable => true;

    /// <summary>
    /// Clears the clipboard.
    /// </summary>
    public override void ClearSelection() {
        ObjectDisposedException.ThrowIf(WasDisposed, this);

        lock (SelectionBytesOutLock) {
            SelectionBytesOut = [];
        }
        Native.XSetSelectionOwner(DisplayPtr, SelectionAtom, IntPtr.Zero, 0);
        Debug.WriteLine($"[PTClipboard:X11] ClearSelection(): Ownership cleared");
    }

    /// <summary>
    /// Sets the selection clipboard text.
    /// </summary>
    /// <param name="text">Text.</param>
    public override void SetSelectionText(string text) {
        ObjectDisposedException.ThrowIf(WasDisposed, this);

        lock (SelectionBytesOutLock) {
            SelectionBytesOut = Encoding.UTF8.GetBytes(text);
        }
        Native.XSetSelectionOwner(DisplayPtr, SelectionAtom, WindowPtr, 0);
        Debug.WriteLine($"[PTClipboard:X11] SetSelectionText(): Ownership set");
    }

    /// <summary>
    /// Gets the selection clipboard text.
    /// </summary>
    public override string GetSelectionText() {
        ObjectDisposedException.ThrowIf(WasDisposed, this);

        SelectionBytesInLock.Reset();  // shouldn't be set but let's make sure
        Native.XConvertSelection(DisplayPtr,
                                        SelectionAtom,
                                        Utf8StringAtom,
                                        MetaSelectionAtom,
                                        WindowPtr,
                                        IntPtr.Zero);
        Native.XFlush(DisplayPtr);
        Debug.WriteLine($"[PTClipboard:X11] GetText(): Text requested");

        if (SelectionBytesInLock.WaitOne(100)) {  // don't wait long
            return Encoding.UTF8.GetString(SelectionBytesIn);
        } else {
            Debug.WriteLine($"[PTClipboard:X11] GetSelectionText(): Timeout reading text");
            return string.Empty;
        }
    }

}
