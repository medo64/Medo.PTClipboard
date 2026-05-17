namespace Medo;

using System;
using System.Runtime.InteropServices;

partial class PTClipboardWaylandProvider {

    private static class Native {

        internal const Int32 OPCODE_GET_REGISTRY = 1;
        internal const Int32 OPCODE_REGISTRY_BIND = 0;
        internal const Int32 OPCODE_MANAGER_CREATE_DATA_SOURCE = 0;
        internal const Int32 OPCODE_MANAGER_GET_DATA_DEVICE = 1;
        internal const Int32 OPCODE_MANAGER_DESTROY = 2;
        internal const Int32 OPCODE_DEVICE_SET_SELECTION = 0;
        internal const Int32 OPCODE_DEVICE_DESTROY = 1;
        internal const Int32 OPCODE_DEVICE_SET_PRIMARY_SELECTION = 2;
        internal const Int32 OPCODE_SOURCE_OFFER = 0;
        internal const Int32 OPCODE_SOURCE_DESTROY = 1;
        internal const Int32 OPCODE_OFFER_RECEIVE = 0;
        internal const Int32 OPCODE_OFFER_DESTROY = 1;


        [StructLayout(LayoutKind.Explicit)]
        internal struct wl_argument {
            [FieldOffset(0)] public Int32 i;
            [FieldOffset(0)] public UInt32 u;
            [FieldOffset(0)] public Int32 h;
            [FieldOffset(0)] public IntPtr s;
            [FieldOffset(0)] public IntPtr o;
            [FieldOffset(0)] public IntPtr n;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct wl_interface {
            public IntPtr name;
            public Int32 version;
            public Int32 methodCount;
            public IntPtr methods;
            public Int32 eventCount;
            public IntPtr events;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct wl_message {
            public IntPtr name;
            public IntPtr signature;
            public IntPtr types;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Timespec {
            public IntPtr tv_sec;
            public IntPtr tv_nsec;
        }


#pragma warning disable CA5392

        [DllImport("libwayland-client.so.0", EntryPoint = "wl_display_connect")]
        internal static extern IntPtr wl_display_connect(IntPtr name);

        [DllImport("libwayland-client.so.0", EntryPoint = "wl_display_disconnect")]
        internal static extern void wl_display_disconnect(IntPtr display);

        [DllImport("libwayland-client.so.0", EntryPoint = "wl_proxy_marshal_array_constructor_versioned")]
        internal static extern IntPtr wl_proxy_marshal_array_constructor_versioned(
            IntPtr proxy,
            UInt32 opcode,
            IntPtr args,
            IntPtr @interface,
            UInt32 version
        );

        [DllImport("libwayland-client.so.0", EntryPoint = "wl_proxy_add_listener")]
        internal static extern Int32 wl_proxy_add_listener(
            IntPtr proxy,
            IntPtr implementation,
            IntPtr data
        );

        [DllImport("libwayland-client.so.0", EntryPoint = "wl_display_roundtrip")]
        internal static extern Int32 wl_display_roundtrip(IntPtr display);

        [DllImport("libwayland-client.so.0", EntryPoint = "wl_display_dispatch_pending")]
        internal static extern Int32 wl_display_dispatch_pending(IntPtr display);

        [DllImport("libwayland-client.so.0", EntryPoint = "wl_display_dispatch_timeout")]
        internal static extern Int32 wl_display_dispatch_timeout(IntPtr display, ref Timespec timeout);

        [DllImport("libwayland-client.so.0", EntryPoint = "wl_display_flush")]
        internal static extern Int32 wl_display_flush(IntPtr display);

        [DllImport("libwayland-client.so.0", EntryPoint = "wl_proxy_destroy")]
        internal static extern void wl_proxy_destroy(IntPtr proxy);

        [DllImport("libwayland-client.so.0", EntryPoint = "wl_proxy_marshal_array")]
        internal static extern void wl_proxy_marshal_array(
            IntPtr proxy,
            UInt32 opcode,
            IntPtr args
        );

#pragma warning restore CA5392

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void RegistryGlobalDelegate(
            IntPtr data,
            IntPtr registry,
            uint name,
            IntPtr interfaceName,
            uint version
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void RegistryGlobalRemoveDelegate(
            IntPtr data,
            IntPtr registry,
            uint name
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void DataDeviceDataOfferDelegate(
            IntPtr data,
            IntPtr device,
            IntPtr offer
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void DataDeviceSelectionDelegate(
            IntPtr data,
            IntPtr device,
            IntPtr offer
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void DataDeviceFinishedDelegate(
            IntPtr data,
            IntPtr device
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void DataOfferOfferDelegate(
            IntPtr data,
            IntPtr offer,
            IntPtr mimeType
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void DataSourceSendDelegate(
            IntPtr data,
            IntPtr source,
            IntPtr mimeType,
            Int32 fd
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void DataSourceCancelledDelegate(
            IntPtr data,
            IntPtr source
        );

    }

}
