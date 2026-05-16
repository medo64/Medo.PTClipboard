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
        if (WindowPtr != IntPtr.Zero) { _ = Native.XDestroyWindow(DisplayPtr, WindowPtr); }
        ClipboardBytesInLock.Dispose();
        SelectionBytesInLock.Dispose();
        GC.SuppressFinalize(this);
    }


    private readonly IntPtr DisplayPtr;
    private readonly IntPtr RootWindowPtr;
    private readonly IntPtr WindowPtr;
    private readonly Int32 ClipboardAtom;
    private readonly Int32 SelectionAtom;
    private readonly Int32 TargetsAtom;
    private readonly Int32 Utf8StringAtom;
    private readonly Int32 MetaSelectionAtom;

    private readonly Thread EventThread;

    private readonly Lock ClipboardBytesOutLock = new();  // locked when BytesOut is accessed
    private byte[] ClipboardBytesOut = [];
    private readonly AutoResetEvent ClipboardBytesInLock = new(false);  // signaled when BytesIn is set
    private byte[] ClipboardBytesIn = [];

    private readonly Lock SelectionBytesOutLock = new();  // locked when BytesOut is accessed
    private byte[] SelectionBytesOut = [];
    private readonly AutoResetEvent SelectionBytesInLock = new(false);  // signaled when BytesIn is set
    private byte[] SelectionBytesIn = [];

}
