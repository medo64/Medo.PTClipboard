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

    private void EventLoop() {
        Debug.WriteLine($"[PTClipboard:X11] Ready for events");

        while (true) {  // this is background thread thus it will stop when app stops
            try {
                Native.XEvent @event = new();
                Native.XNextEvent(DisplayPtr, ref @event);
                Debug.WriteLine($"[PTClipboard:X11] NextEvent: {@event.type}");

                switch (@event.type) {
                    case Native.XEventType.SelectionRequest: {
                            var requestEvent = @event.xselectionrequest;
                            if (Native.XGetSelectionOwner(DisplayPtr, requestEvent.selection) != WindowPtr) { continue; }  // not for us
                            if (requestEvent.property == IntPtr.Zero) { continue; }  // we ignore empty propertty
                            if ((requestEvent.selection != ClipboardAtom) && (requestEvent.selection != SelectionAtom)) { continue; }  // we ignore anything not clipboard

                            if (requestEvent.target == TargetsAtom) {  // asking for formats
                                Debug.WriteLine($"[PTClipboard:X11]   Query for {Native.XGetAtomName(DisplayPtr, requestEvent.property.ToInt32())}");

                                Native.XChangeProperty(requestEvent.display,
                                                              requestEvent.requestor,
                                                              requestEvent.property,
                                                              4,   // XA_ATOM
                                                              32,  // 32-bit data
                                                              0,   // Replace
                                                              [Utf8StringAtom],
                                                              1);

                                var sendEvent = GetNewSelectionEventFromSelectionRequestEvent(@event, Native.XEventType.SelectionNotify, sendEvent: true);
                                var resSend = Native.XSendEvent(DisplayPtr,
                                                                       requestEvent.requestor,
                                                                       propagate: false,
                                                                       eventMask: IntPtr.Zero,
                                                                       ref sendEvent);
                                if (resSend == 0) { Debug.WriteLine($"[PTClipboard:X11]   Failed to send event"); }

                            } else if (requestEvent.target == Utf8StringAtom) {
                                Debug.WriteLine($"[PTClipboard:X11]   Request for {Native.XGetAtomName(DisplayPtr, requestEvent.property.ToInt32())}");

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

                                    Native.XChangeProperty(DisplayPtr,
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

                                var sendEvent = GetNewSelectionEventFromSelectionRequestEvent(@event, Native.XEventType.SelectionNotify, sendEvent: true);
                                var resSend = Native.XSendEvent(DisplayPtr,
                                                                       requestEvent.requestor,
                                                                       propagate: false,
                                                                       eventMask: IntPtr.Zero,
                                                                       ref sendEvent);
                                if (resSend == 0) { Debug.WriteLine($"[PTClipboard:X11]   Failed to send event"); }
                            }
                        }
                        break;

                    case Native.XEventType.SelectionNotify: {
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

                            Debug.WriteLine($"[PTClipboard:X11]   Notification for {Native.XGetAtomName(DisplayPtr, selectionEvent.property.ToInt32())}");

                            var data = IntPtr.Zero;
                            Native.XGetWindowProperty(DisplayPtr,
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
                                    Native.XFree(data);
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
                                    Native.XFree(data);
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

    private static Native.XEvent GetNewSelectionEventFromSelectionRequestEvent(Native.XEvent @event, Native.XEventType? type = null, bool? sendEvent = null) {
        var newEvent = new Native.XEvent();
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

}
