namespace Medo;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

partial class PTClipboardWaylandProvider {

    private sealed class OfferState {
        public OfferState(IntPtr pointer) {
            Pointer = pointer;
        }

        public IntPtr Pointer { get; }
        public HashSet<string> MimeTypes { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class SourceState {
        public SourceState(IntPtr pointer, string text) {
            Pointer = pointer;
            Text = text;
        }

        public IntPtr Pointer { get; }
        public string Text { get; }
    }

    private void HandleRegistryGlobal(IntPtr data, IntPtr registry, UInt32 name, IntPtr interfaceName, UInt32 version) {
        var me = (PTClipboardWaylandProvider)GCHandle.FromIntPtr(data).Target!;

        var interfaceString = Marshal.PtrToStringUTF8(interfaceName);
        if (string.IsNullOrWhiteSpace(interfaceString)) { return; }

        if (me.SeatPtr == IntPtr.Zero && interfaceString == "wl_seat") {
            var seatBindVersion = Math.Min(version, 9u);
            // bind signature is "usun": uint, string, uint, new_id — 4 slots required;
            // libwayland writes the new proxy into args[3].o.
            var seatBindArgs = new Native.wl_argument[4];
            seatBindArgs[0].u = name;
            seatBindArgs[1].s = SeatInterface.NameHandle.DangerousGetHandle();
            seatBindArgs[2].u = seatBindVersion;
            var pinnedSeatBindArgs = GCHandle.Alloc(seatBindArgs, GCHandleType.Pinned);
            try {
                me.SeatPtr = Native.wl_proxy_marshal_array_constructor_versioned(
                    registry,
                    Native.OPCODE_REGISTRY_BIND,
                    pinnedSeatBindArgs.AddrOfPinnedObject(),
                    SeatInterface.DangerousGetHandle(),
                    version: seatBindVersion);
            } finally {
                pinnedSeatBindArgs.Free();
            }
            return;
        }

        if (me.ManagerPtr != IntPtr.Zero) { return; }

        if (interfaceString == "ext_data_control_manager_v1") {
            var boundVersion = Math.Min(version, 1u);
            // bind signature is "usun": 4 slots required; libwayland writes new proxy into args[3].o.
            var bindArgs = new Native.wl_argument[4];
            bindArgs[0].u = name;
            bindArgs[1].s = ExtDataControlManagerInterface.NameHandle.DangerousGetHandle();
            bindArgs[2].u = boundVersion;
            var pinnedBindArgs = GCHandle.Alloc(bindArgs, GCHandleType.Pinned);
            try {
                me.ManagerPtr = Native.wl_proxy_marshal_array_constructor_versioned(
                    registry,
                    Native.OPCODE_REGISTRY_BIND,
                    pinnedBindArgs.AddrOfPinnedObject(),
                    ExtDataControlManagerInterface.DangerousGetHandle(),
                    version: boundVersion);
            } finally {
                pinnedBindArgs.Free();
            }
            return;
        }
    }

    private void HandleRegistryGlobalRemove(IntPtr data, IntPtr registry, uint name) {
    }

    private void HandleDataDeviceDataOffer(IntPtr data, IntPtr device, IntPtr offer) {
        var me = (PTClipboardWaylandProvider)GCHandle.FromIntPtr(data).Target!;
        if (offer == IntPtr.Zero) { return; }
        if (me.Offers.ContainsKey(offer)) { return; }

        var addListenerRes = Native.wl_proxy_add_listener(
            offer,
            me.OfferListeners.DangerousGetHandle(),
            me.OwnPtr);
        if (addListenerRes != 0) {
            Debug.WriteLine("[PTClipboard:WL] Failed to connect data offer listeners");
            return;
        }

        me.Offers[offer] = new OfferState(offer);
    }

    private void HandleDataDeviceSelection(IntPtr data, IntPtr device, IntPtr offer) {
        var me = (PTClipboardWaylandProvider)GCHandle.FromIntPtr(data).Target!;
        var previous = me.ClipboardOfferPtr;
        me.ClipboardOfferPtr = offer;
        me.ReleaseOfferIfUnused(previous);
    }

    private void HandleDataDevicePrimarySelection(IntPtr data, IntPtr device, IntPtr offer) {
        var me = (PTClipboardWaylandProvider)GCHandle.FromIntPtr(data).Target!;
        var previous = me.PrimaryOfferPtr;
        me.PrimaryOfferPtr = offer;
        me.ReleaseOfferIfUnused(previous);
    }

    private void HandleDataDeviceFinished(IntPtr data, IntPtr device) {
        var me = (PTClipboardWaylandProvider)GCHandle.FromIntPtr(data).Target!;
        me.ReleaseAllOffers();
    }

    private void HandleDataOfferOffer(IntPtr data, IntPtr offer, IntPtr mimeType) {
        var me = (PTClipboardWaylandProvider)GCHandle.FromIntPtr(data).Target!;
        if (!me.Offers.TryGetValue(offer, out var state)) { return; }

        var mimeTypeString = Marshal.PtrToStringUTF8(mimeType);
        if (!string.IsNullOrWhiteSpace(mimeTypeString)) {
            state.MimeTypes.Add(mimeTypeString);
        }
    }

    private void HandleDataSourceSend(IntPtr data, IntPtr source, IntPtr mimeType, int fd) {
        var me = (PTClipboardWaylandProvider)GCHandle.FromIntPtr(data).Target!;
        if ((fd < 0) || !me.Sources.TryGetValue(source, out var state)) { return; }

        try {
            var mimeTypeString = Marshal.PtrToStringUTF8(mimeType);
            if (!string.IsNullOrWhiteSpace(mimeTypeString)
                    && !mimeTypeString.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(mimeTypeString, "UTF8_STRING", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(mimeTypeString, "STRING", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(mimeTypeString, "TEXT", StringComparison.OrdinalIgnoreCase)) {
                return;
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(state.Text);
            using var handle = new SafeFileHandle((IntPtr)fd, ownsHandle: true);
            using var stream = new FileStream(handle, FileAccess.Write);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        } catch (IOException) {
        }
    }

    private void HandleDataSourceCancelled(IntPtr data, IntPtr source) {
        var me = (PTClipboardWaylandProvider)GCHandle.FromIntPtr(data).Target!;
        me.ReleaseSource(source);
    }

}
