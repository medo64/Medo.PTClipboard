/* Josip Medved <jmedved@jmedved.com> * www.medo64.com * MIT License */

namespace Medo;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

partial class PTClipboardX11Provider {

    private static class Native {  // https://www.x.org/releases/current/doc/libX11/libX11/libX11.html

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
