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

internal sealed partial class PTClipboardX11Provider : PTClipboardProvider, IDisposable {

    internal PTClipboardX11Provider()
        : base() {
        try {
            DisplayPtr = Native.XOpenDisplay(null);
            if (DisplayPtr == IntPtr.Zero) { throw new NotSupportedException("Failed to open display"); }
            Debug.WriteLine($"[PTClipboard:X11] Display: 0x{DisplayPtr:X2}");
        } catch (DllNotFoundException) {
            throw new NotSupportedException("Cannot load libX11");
        }

        RootWindowPtr = Native.XDefaultRootWindow(DisplayPtr);
        if (RootWindowPtr == IntPtr.Zero) { throw new NotSupportedException("Failed to open root window"); }
        Debug.WriteLine($"[PTClipboard:X11] RootWindow: 0x{RootWindowPtr:X2}");

        WindowPtr = Native.XCreateSimpleWindow(DisplayPtr, RootWindowPtr, -10, -10, 1, 1, 0, 0, 0);
        if (WindowPtr == IntPtr.Zero) { throw new NotSupportedException("Failed to open new window"); }
        Debug.WriteLine($"[PTClipboard:X11] Window: 0x{WindowPtr:X2}");

        TargetsAtom = Native.XInternAtom(DisplayPtr, "TARGETS", only_if_exists: false);
        if (TargetsAtom == IntPtr.Zero) { throw new NotSupportedException("Failed to open TARGETS atom"); }
        Debug.WriteLine($"[PTClipboard:X11] Atom[TARGETS]: 0x{TargetsAtom:X2}");

        ClipboardAtom = Native.XInternAtom(DisplayPtr, "CLIPBOARD", only_if_exists: false);
        if (ClipboardAtom == 0) { throw new NotSupportedException("Failed to open CLIPBOARD atom"); }
        Debug.WriteLine($"[PTClipboard:X11] Atom(CLIPBOARD): 0x{ClipboardAtom:X2}");

        SelectionAtom = Native.XInternAtom(DisplayPtr, "PRIMARY", only_if_exists: false);
        if (SelectionAtom == 0) { throw new NotSupportedException("Failed to open PRIMARY atom"); }
        Debug.WriteLine($"[PTClipboard:X11] Atom(PRIMARY): 0x{SelectionAtom:X2}");

        Utf8StringAtom = Native.XInternAtom(DisplayPtr, "UTF8_STRING", only_if_exists: false);
        if (Utf8StringAtom == 0) { throw new NotSupportedException("Failed to open UTF8_STRING atom"); }
        Debug.WriteLine($"[PTClipboard:X11] Atom[UTF8_STRING]: 0x{Utf8StringAtom:X2}");

        var metaSelectionAtomName = "MEDO_SELECTION_0x" + RandomNumberGenerator.GetHexString(16, lowercase: true);
        MetaSelectionAtom = Native.XInternAtom(DisplayPtr, metaSelectionAtomName, only_if_exists: false);
        if (MetaSelectionAtom == 0) { throw new NotSupportedException("Failed to open {metaSelectionAtomName} atom"); }
        Debug.WriteLine($"[PTClipboard:X11] Atom[{metaSelectionAtomName}]: 0x{MetaSelectionAtom:X2}");

        EventThread = new Thread(EventLoop) {  // last to initialize so we can use it as detection for successful init
            IsBackground = true,
            Name = "X11Clipboard",
        };
        EventThread.Start();
    }

    ~PTClipboardX11Provider() {
        Dispose();
    }

    private bool WasDisposed;
    public void Dispose() {
        if (WasDisposed) { return; } else { WasDisposed = true; }
        if (DisplayPtr != IntPtr.Zero) { Native.XCloseDisplay(DisplayPtr); }
        if (WindowPtr != IntPtr.Zero) { var _ = Native.XDestroyWindow(DisplayPtr, WindowPtr); }
        ClipboardBytesInLock.Dispose();
        SelectionBytesInLock.Dispose();
        GC.SuppressFinalize(this);
    }


    #region PTClipboardProvider

    /// <summary>
    /// Gets if clipboard service is available.
    /// </summary>
    public override bool IsClipboardAvailable {
        get { return (EventThread != null); }
    }

    /// <summary>
    /// Clears the clipboard.
    /// </summary>
    public override void ClearClipboard() {
        if (EventThread == null) { return; }   // something went wrong when initializing

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
        if (EventThread == null) { return; }   // something went wrong when initializing

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
        if (EventThread == null) { return string.Empty; }   // something went wrong when initializing

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
    public override bool IsSelectionAvailable {
        get { return (EventThread != null); }
    }

    /// <summary>
    /// Clears the clipboard.
    /// </summary>
    public override void ClearSelection() {
        if (EventThread == null) { return; }   // something went wrong when initializing

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
        if (EventThread == null) { return; }  // something went wrong when initializing

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
        if (EventThread == null) { return string.Empty; }  // something went wrong when initializing

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

    #endregion PTClipboardProvider


    private readonly IntPtr DisplayPtr;
    private readonly IntPtr RootWindowPtr;
    private readonly IntPtr WindowPtr;
    private readonly Int32 ClipboardAtom;
    private readonly Int32 SelectionAtom;
    private readonly Int32 TargetsAtom;
    private readonly Int32 Utf8StringAtom;
    private readonly Int32 MetaSelectionAtom;

    private readonly Thread? EventThread;

    private readonly Lock ClipboardBytesOutLock = new();  // locked when BytesOut is accessed
    private byte[] ClipboardBytesOut = [];
    private readonly AutoResetEvent ClipboardBytesInLock = new(false);  // signaled when BytesIn is set
    private byte[] ClipboardBytesIn = [];

    private readonly Lock SelectionBytesOutLock = new();  // locked when BytesOut is accessed
    private byte[] SelectionBytesOut = [];
    private readonly AutoResetEvent SelectionBytesInLock = new(false);  // signaled when BytesIn is set
    private byte[] SelectionBytesIn = [];

}
