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

internal sealed class PTClipboardX11Provider : PTClipboardProvider, IDisposable {

    internal PTClipboardX11Provider()
        : base() {
        try {
            DisplayPtr = NativeMethods.XOpenDisplay(null);
            if (DisplayPtr == IntPtr.Zero) { throw new NotSupportedException("Failed to open display"); }
            Debug.WriteLine($"[PTClipboard:X11] Display: 0x{DisplayPtr:X2}");
        } catch (DllNotFoundException) {
            throw new NotSupportedException("Cannot load libX11");
        }

        RootWindowPtr = NativeMethods.XDefaultRootWindow(DisplayPtr);
        if (RootWindowPtr == IntPtr.Zero) { throw new NotSupportedException("Failed to open root window"); }
        Debug.WriteLine($"[PTClipboard:X11] RootWindow: 0x{RootWindowPtr:X2}");

        WindowPtr = NativeMethods.XCreateSimpleWindow(DisplayPtr, RootWindowPtr, -10, -10, 1, 1, 0, 0, 0);
        if (WindowPtr == IntPtr.Zero) { throw new NotSupportedException("Failed to open new window"); }
        Debug.WriteLine($"[PTClipboard:X11] Window: 0x{WindowPtr:X2}");

        TargetsAtom = NativeMethods.XInternAtom(DisplayPtr, "TARGETS", only_if_exists: false);
        if (TargetsAtom == IntPtr.Zero) { throw new NotSupportedException("Failed to open TARGETS atom"); }
        Debug.WriteLine($"[PTClipboard:X11] Atom[TARGETS]: 0x{TargetsAtom:X2}");

        ClipboardAtom = NativeMethods.XInternAtom(DisplayPtr, "CLIPBOARD", only_if_exists: false);
        if (ClipboardAtom == 0) { throw new NotSupportedException("Failed to open CLIPBOARD atom"); }
        Debug.WriteLine($"[PTClipboard:X11] Atom(CLIPBOARD): 0x{ClipboardAtom:X2}");

        SelectionAtom = NativeMethods.XInternAtom(DisplayPtr, "PRIMARY", only_if_exists: false);
        if (SelectionAtom == 0) { throw new NotSupportedException("Failed to open PRIMARY atom"); }
        Debug.WriteLine($"[PTClipboard:X11] Atom(PRIMARY): 0x{SelectionAtom:X2}");

        Utf8StringAtom = NativeMethods.XInternAtom(DisplayPtr, "UTF8_STRING", only_if_exists: false);
        if (Utf8StringAtom == 0) { throw new NotSupportedException("Failed to open UTF8_STRING atom"); }
        Debug.WriteLine($"[PTClipboard:X11] Atom[UTF8_STRING]: 0x{Utf8StringAtom:X2}");

        var metaSelectionAtomName = "MEDO_SELECTION_0x" + RandomNumberGenerator.GetHexString(16, lowercase: true);
        MetaSelectionAtom = NativeMethods.XInternAtom(DisplayPtr, metaSelectionAtomName, only_if_exists: false);
        if (MetaSelectionAtom == 0) { throw new NotSupportedException("Failed to open {metaSelectionAtomName} atom"); }
        Debug.WriteLine($"[PTClipboard:X11] Atom[{metaSelectionAtomName}]: 0x{MetaSelectionAtom:X2}");

        EventThread = new Thread(EventLoop) {  // last to initialize so we can use it as detection for successful init
            IsBackground = true,
            Name = "X11Clipboard",
        };
        EventThread.Start();
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
        NativeMethods.XSetSelectionOwner(DisplayPtr, ClipboardAtom, IntPtr.Zero, 0);
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
        NativeMethods.XSetSelectionOwner(DisplayPtr, ClipboardAtom, WindowPtr, 0);
        Debug.WriteLine($"[PTClipboard:X11] SetText(): Ownership set");
    }

    /// <summary>
    /// Gets the clipboard text.
    /// </summary>
    public override string GetClipboardText() {
        if (EventThread == null) { return string.Empty; }   // something went wrong when initializing

        ClipboardBytesInLock.Reset();  // shouldn't be set but let's make sure
        NativeMethods.XConvertSelection(DisplayPtr,
                                        ClipboardAtom,
                                        Utf8StringAtom,
                                        MetaSelectionAtom,
                                        WindowPtr,
                                        IntPtr.Zero);
        NativeMethods.XFlush(DisplayPtr);
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
        NativeMethods.XSetSelectionOwner(DisplayPtr, SelectionAtom, IntPtr.Zero, 0);
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
        NativeMethods.XSetSelectionOwner(DisplayPtr, SelectionAtom, WindowPtr, 0);
        Debug.WriteLine($"[PTClipboard:X11] SetSelectionText(): Ownership set");
    }

    /// <summary>
    /// Gets the selection clipboard text.
    /// </summary>
    public override string GetSelectionText() {
        if (EventThread == null) { return string.Empty; }  // something went wrong when initializing

        SelectionBytesInLock.Reset();  // shouldn't be set but let's make sure
        NativeMethods.XConvertSelection(DisplayPtr,
                                        SelectionAtom,
                                        Utf8StringAtom,
                                        MetaSelectionAtom,
                                        WindowPtr,
                                        IntPtr.Zero);
        NativeMethods.XFlush(DisplayPtr);
        Debug.WriteLine($"[PTClipboard:X11] GetText(): Text requested");

        if (SelectionBytesInLock.WaitOne(100)) {  // don't wait long
            return Encoding.UTF8.GetString(SelectionBytesIn);
        } else {
            Debug.WriteLine($"[PTClipboard:X11] GetSelectionText(): Timeout reading text");
            return string.Empty;
        }
    }

    #endregion PTClipboardProvider


    private IntPtr DisplayPtr;
    private readonly IntPtr RootWindowPtr;
    private IntPtr WindowPtr;
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

    private void EventLoop() {
        Debug.WriteLine($"[PTClipboard:X11] Ready for events");

        while (true) {  // this is background thread thus it will stop when app stops
            try {
                NativeMethods.XEvent @event = new();
                NativeMethods.XNextEvent(DisplayPtr, ref @event);
                Debug.WriteLine($"[PTClipboard:X11] NextEvent: {@event.type}");

                switch (@event.type) {
                    case NativeMethods.XEventType.SelectionRequest: {
                            var requestEvent = @event.xselectionrequest;
                            if (NativeMethods.XGetSelectionOwner(DisplayPtr, requestEvent.selection) != WindowPtr) { continue; }  // not for us
                            if (requestEvent.property == IntPtr.Zero) { continue; }  // we ignore empty propertty
                            if ((requestEvent.selection != ClipboardAtom) && (requestEvent.selection != SelectionAtom)) { continue; }  // we ignore anything not clipboard

                            if (requestEvent.target == TargetsAtom) {  // asking for formats
                                Debug.WriteLine($"[PTClipboard:X11]   Query for {NativeMethods.XGetAtomName(DisplayPtr, requestEvent.property.ToInt32())}");

                                NativeMethods.XChangeProperty(requestEvent.display,
                                                              requestEvent.requestor,
                                                              requestEvent.property,
                                                              4,   // XA_ATOM
                                                              32,  // 32-bit data
                                                              0,   // Replace
                                                              [Utf8StringAtom],
                                                              1);

                                var sendEvent = GetNewSelectionEventFromSelectionRequestEvent(@event, NativeMethods.XEventType.SelectionNotify, sendEvent: true);
                                var resSend = NativeMethods.XSendEvent(DisplayPtr,
                                                                       requestEvent.requestor,
                                                                       propagate: false,
                                                                       eventMask: IntPtr.Zero,
                                                                       ref sendEvent);
                                if (resSend == 0) { Debug.WriteLine($"[PTClipboard:X11]   Failed to send event"); }

                            } else if (requestEvent.target == Utf8StringAtom) {
                                Debug.WriteLine($"[PTClipboard:X11]   Request for {NativeMethods.XGetAtomName(DisplayPtr, requestEvent.property.ToInt32())}");

                                var bufferPtr = IntPtr.Zero;
                                int bufferLength;
                                try {
                                    if (requestEvent.selection == SelectionAtom) {
                                        lock (SelectionBytesOutLock) {
                                            bufferPtr = Marshal.AllocHGlobal(SelectionBytesOut.Length);
                                            bufferLength = SelectionBytesOut.Length;
                                            Marshal.Copy(SelectionBytesOut, 0, bufferPtr, SelectionBytesOut.Length);
                                        }
                                    } else {
                                        lock (ClipboardBytesOutLock) {
                                            bufferPtr = Marshal.AllocHGlobal(ClipboardBytesOut.Length);
                                            bufferLength = ClipboardBytesOut.Length;
                                            Marshal.Copy(ClipboardBytesOut, 0, bufferPtr, ClipboardBytesOut.Length);
                                        }
                                    }

                                    NativeMethods.XChangeProperty(DisplayPtr,
                                                                requestEvent.requestor,
                                                                requestEvent.property,
                                                                requestEvent.target,
                                                                8,  // 8-bit data
                                                                0,  // Replace
                                                                bufferPtr,
                                                                bufferLength);
                                } finally {
                                    if (bufferPtr != IntPtr.Zero) { Marshal.FreeHGlobal(bufferPtr); }
                                }

                                var sendEvent = GetNewSelectionEventFromSelectionRequestEvent(@event, NativeMethods.XEventType.SelectionNotify, sendEvent: true);
                                var resSend = NativeMethods.XSendEvent(DisplayPtr,
                                                                       requestEvent.requestor,
                                                                       propagate: false,
                                                                       eventMask: IntPtr.Zero,
                                                                       ref sendEvent);
                                if (resSend == 0) { Debug.WriteLine($"[PTClipboard:X11]   Failed to send event"); }
                            }
                        }
                        break;

                    case NativeMethods.XEventType.SelectionNotify: {
                            var selectionEvent = @event.xselection;
                            if (selectionEvent.target != Utf8StringAtom) { continue; }  // we ignore anything not clipboard

                            if (selectionEvent.property == 0) {
                                Debug.WriteLine($"[PTClipboard:X11]   Notification for empty");
                                if (selectionEvent.selection == SelectionAtom) {
                                    SelectionBytesIn = [];
                                    SelectionBytesInLock.Set();
                                } else {
                                    ClipboardBytesIn = [];
                                    ClipboardBytesInLock.Set();
                                }
                                continue;
                            }

                            Debug.WriteLine($"[PTClipboard:X11]   Notification for {NativeMethods.XGetAtomName(DisplayPtr, selectionEvent.property.ToInt32())}");

                            var data = IntPtr.Zero;
                            NativeMethods.XGetWindowProperty(DisplayPtr,
                                                             selectionEvent.requestor,
                                                             selectionEvent.property,
                                                             long_offset: 0,
                                                             long_length: int.MaxValue,
                                                             delete: false,
                                                             0,  // AnyPropertyType
                                                             out var type,
                                                             out var format,
                                                             out var nitems,
                                                             out var bytes_after,
                                                             ref data);
                            if (selectionEvent.selection == SelectionAtom) {
                                if (data != IntPtr.Zero) {
                                    SelectionBytesIn = new byte[nitems.ToInt32()];
                                    Marshal.Copy(data, SelectionBytesIn, 0, SelectionBytesIn.Length);
                                    SelectionBytesInLock.Set();
                                    NativeMethods.XFree(data);
                                } else {
                                    Debug.WriteLine($"[PTClipboard:X11]   Cannot retrieve data");
                                    SelectionBytesIn = [];
                                    SelectionBytesInLock.Set();
                                }
                            } else {
                                if (data != IntPtr.Zero) {
                                    ClipboardBytesIn = new byte[nitems.ToInt32()];
                                    Marshal.Copy(data, ClipboardBytesIn, 0, ClipboardBytesIn.Length);
                                    ClipboardBytesInLock.Set();
                                    NativeMethods.XFree(data);
                                } else {
                                    Debug.WriteLine($"[PTClipboard:X11]   Cannot retrieve data");
                                    ClipboardBytesIn = [];
                                    ClipboardBytesInLock.Set();
                                }
                            }
                        }
                        break;

                    default: break;
                }
#pragma warning disable CA1031
            } catch (Exception ex) {
#pragma warning restore CA1031
                Debug.WriteLine($"[PTClipboard:X11] Error: {ex.Message}");
            }
        }
    }

    private static NativeMethods.XEvent GetNewSelectionEventFromSelectionRequestEvent(NativeMethods.XEvent @event, NativeMethods.XEventType? type = null, bool? sendEvent = null) {
        var newEvent = new NativeMethods.XEvent();
        newEvent.xselection.type = (type != null) ? type.Value : @event.xselectionrequest.type;
        newEvent.xselection.serial = @event.xselectionrequest.serial;
        newEvent.xselection.send_event = (sendEvent != null) ? sendEvent.Value : @event.xselectionrequest.send_event;
        newEvent.xselection.display = @event.xselectionrequest.display;
        newEvent.xselection.requestor = @event.xselectionrequest.requestor;
        newEvent.xselection.selection = @event.xselectionrequest.selection;
        newEvent.xselection.target = @event.xselectionrequest.target;
        newEvent.xselection.property = @event.xselectionrequest.property;
        newEvent.xselection.time = @event.xselectionrequest.time;
        return newEvent;
    }

    ~PTClipboardX11Provider() {
        Dispose();
    }

    public void Dispose() {
        if (WindowPtr != IntPtr.Zero) {
            var _ = NativeMethods.XDestroyWindow(DisplayPtr, WindowPtr);
            WindowPtr = IntPtr.Zero;
        }
        if (DisplayPtr != IntPtr.Zero) {
            NativeMethods.XCloseDisplay(DisplayPtr);
            DisplayPtr = IntPtr.Zero;
        }
        ClipboardBytesInLock.Dispose();
        SelectionBytesInLock.Dispose();
        GC.SuppressFinalize(this);
    }

    private static class NativeMethods {  //https://www.x.org/releases/current/doc/libX11/libX11/libX11.html

        internal enum XEventType {
            KeyPress = 2,
            KeyRelease = 3,
            ButtonPress = 4,
            ButtonRelease = 5,
            MotionNotify = 6,
            EnterNotify = 7,
            LeaveNotify = 8,
            FocusIn = 9,
            FocusOut = 10,
            KeymapNotify = 11,
            Expose = 12,
            GraphicsExpose = 13,
            NoExpose = 14,
            VisibilityNotify = 15,
            CreateNotify = 16,
            DestroyNotify = 17,
            UnmapNotify = 18,
            MapNotify = 19,
            MapRequest = 20,
            ReparentNotify = 21,
            ConfigureNotify = 22,
            ConfigureRequest = 23,
            GravityNotify = 24,
            ResizeRequest = 25,
            CirculateNotify = 26,
            CirculateRequest = 27,
            PropertyNotify = 28,
            SelectionClear = 29,
            SelectionRequest = 30,
            SelectionNotify = 31,
            ColormapNotify = 32,
            ClientMessage = 33,
            MappingNotify = 34,
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct XSelectionClearEvent {
            internal XEventType type;
            internal IntPtr serial;
            internal bool send_event;
            internal IntPtr display;
            internal IntPtr window;
            internal IntPtr selection;
            internal IntPtr time;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct XSelectionEvent {
            internal XEventType type;
            internal IntPtr serial;
            internal bool send_event;
            internal IntPtr display;
            internal IntPtr requestor;
            internal IntPtr selection;
            internal IntPtr target;
            internal IntPtr property;
            internal IntPtr time;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct XSelectionRequestEvent {
            internal XEventType type;
            internal IntPtr serial;
            internal bool send_event;
            internal IntPtr display;
            internal IntPtr owner;
            internal IntPtr requestor;
            internal IntPtr selection;
            internal IntPtr target;
            internal IntPtr property;
            internal IntPtr time;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct XEventPad {
            internal IntPtr pad00;
            internal IntPtr pad01;
            internal IntPtr pad02;
            internal IntPtr pad03;
            internal IntPtr pad04;
            internal IntPtr pad05;
            internal IntPtr pad06;
            internal IntPtr pad07;
            internal IntPtr pad08;
            internal IntPtr pad09;
            internal IntPtr pad10;
            internal IntPtr pad11;
            internal IntPtr pad12;
            internal IntPtr pad13;
            internal IntPtr pad14;
            internal IntPtr pad15;
            internal IntPtr pad16;
            internal IntPtr pad17;
            internal IntPtr pad18;
            internal IntPtr pad19;
            internal IntPtr pad20;
            internal IntPtr pad21;
            internal IntPtr pad22;
            internal IntPtr pad23;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct XEvent {
            [FieldOffset(0)] internal XEventType type;
            [FieldOffset(0)] internal XSelectionClearEvent xselectionclear;
            [FieldOffset(0)] internal XSelectionRequestEvent xselectionrequest;
            [FieldOffset(0)] internal XSelectionEvent xselection;
            [FieldOffset(0)] internal XEventPad pad;
        }

#pragma warning disable CA5392,SYSLIB1054

        [DllImport("libX11")]  // actually returns Int32 but we don't care
        internal extern static void XChangeProperty(IntPtr display, IntPtr w, IntPtr property, IntPtr type, Int32 format, Int32 mode, IntPtr data, int nelements);

        [DllImport("libX11")]  // actually returns Int32 but we don't care
        internal extern static void XChangeProperty(IntPtr display, IntPtr w, IntPtr property, UInt32 type, Int32 format, Int32 mode, Int32[] data, int nelements);

        [DllImport("libX11")]
        internal extern static void XCloseDisplay(IntPtr display);

        [DllImport("libX11")]
        internal extern static void XConvertSelection(IntPtr display, IntPtr selection, IntPtr target, IntPtr property, IntPtr requestor, IntPtr time);

        [DllImport("libX11")]
        internal extern static IntPtr XCreateSimpleWindow(IntPtr display, IntPtr parent, Int32 x, Int32 y, UInt32 width, UInt32 height, UInt32 border_width, nuint border, nuint background);

        [DllImport("libX11")]
        internal extern static IntPtr XDefaultRootWindow(IntPtr display);

        [DllImport("libX11")]
        public static extern int XDestroyWindow(IntPtr display, IntPtr window);

        [DllImport("libX11")]  // actually returns Int32 but we don't care
        internal extern static void XFlush(IntPtr display);

        [DllImport("libX11")]
        internal extern static void XFree(IntPtr data);

        [DllImport("libX11", BestFitMapping = false)]
        [return: MarshalAs(UnmanagedType.LPUTF8Str)]
        internal extern static String XGetAtomName(IntPtr display, Int32 atom);

        [DllImport("libX11")]
        internal extern static IntPtr XGetSelectionOwner(IntPtr display, IntPtr selection);

        [DllImport("libX11")]  // actually returns Int32 but we don't care
        internal extern static void XGetWindowProperty(IntPtr display, IntPtr w, IntPtr property, IntPtr long_offset, IntPtr long_length, bool delete, IntPtr req_type, out IntPtr actual_type_return, out Int32 actual_format_return, out IntPtr nitems_return, out IntPtr bytes_after_return, ref IntPtr prop_return);

        [DllImport("libX11", BestFitMapping = false)]
        internal extern static Int32 XInternAtom(IntPtr display, [MarshalAs(UnmanagedType.LPUTF8Str)] String atom_name, bool only_if_exists);

        [DllImport("libX11")]
        internal extern static void XNextEvent(IntPtr display, ref XEvent event_return);

        [DllImport("libX11", BestFitMapping = false)]
        internal extern static IntPtr XOpenDisplay([MarshalAs(UnmanagedType.LPUTF8Str)] String? display_name);

        [DllImport("libX11")]
        internal extern static Int32 XSendEvent(IntPtr display, IntPtr window, bool propagate, IntPtr eventMask, ref XEvent sendEvent);

        [DllImport("libX11")]
        internal extern static void XSetSelectionOwner(IntPtr display, IntPtr selection, IntPtr owner, UInt32 time);

#pragma warning restore CA5392,SYSLIB1054

    }
}
