namespace Medo;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;

internal sealed partial class PTClipboardWaylandProvider : PTClipboardProvider {

    public PTClipboardWaylandProvider() {
        OwnPtr = GCHandle.ToIntPtr(GCHandle.Alloc(this));

        try {
            DisplayPtr = Native.wl_display_connect(IntPtr.Zero);
            if (DisplayPtr == IntPtr.Zero) { throw new NotSupportedException("Failed to connect to Wayland display"); }
            Debug.WriteLine($"[PTClipboard:WL] Display handle 0x{DisplayPtr:x2} connected");
        } catch (DllNotFoundException) {
            throw new NotSupportedException("Cannot load libwayland-client.so.0");
        }

        var registryArgBuf = new Native.wl_argument[1];
        var pinnedRegistryArgs = GCHandle.Alloc(registryArgBuf, GCHandleType.Pinned);
        try {
            RegistryPtr = Native.wl_proxy_marshal_array_constructor_versioned(
                DisplayPtr,
                Native.OPCODE_GET_REGISTRY,
                pinnedRegistryArgs.AddrOfPinnedObject(),
                RegistryInterface.DangerousGetHandle(),
                version: 1);
        } finally {
            pinnedRegistryArgs.Free();
        }
        if (RegistryPtr == IntPtr.Zero) { throw new NotSupportedException("Failed to bind to Wayland registry"); }

        RegistryListeners = new(
           (Native.RegistryGlobalDelegate)HandleRegistryGlobal,
           (Native.RegistryGlobalRemoveDelegate)HandleRegistryGlobalRemove
       );

        var addRegistryListenerRes = Native.wl_proxy_add_listener(
            RegistryPtr,
            RegistryListeners.DangerousGetHandle(),
            OwnPtr
        );
        if (addRegistryListenerRes != 0) { throw new NotSupportedException("Failed to connect Wayland listeners"); }

        var discoverGlobalsRes = Native.wl_display_roundtrip(DisplayPtr);
        if (discoverGlobalsRes < 0) { throw new NotSupportedException("Failed to execute Wayland display roundtrip"); }

        DeviceListeners = new(
            (Native.DataDeviceDataOfferDelegate)HandleDataDeviceDataOffer,
            (Native.DataDeviceSelectionDelegate)HandleDataDeviceSelection,
            (Native.DataDeviceFinishedDelegate)HandleDataDeviceFinished,
            (Native.DataDeviceSelectionDelegate)HandleDataDevicePrimarySelection
        );

        OfferListeners = new(
            (Native.DataOfferOfferDelegate)HandleDataOfferOffer
        );

        SourceListeners = new(
            (Native.DataSourceSendDelegate)HandleDataSourceSend,
            (Native.DataSourceCancelledDelegate)HandleDataSourceCancelled
        );

        var getDataDeviceArgs = new Native.wl_argument[2];
        getDataDeviceArgs[1].o = SeatPtr;
        var pinnedArgs = GCHandle.Alloc(getDataDeviceArgs, GCHandleType.Pinned);
        try {
            DevicePtr = Native.wl_proxy_marshal_array_constructor_versioned(
                ManagerPtr,
                Native.OPCODE_MANAGER_GET_DATA_DEVICE,
                pinnedArgs.AddrOfPinnedObject(),
                ExtDataControlDeviceInterface.DangerousGetHandle(),
                version: 1);
        } finally {
            pinnedArgs.Free();
        }
        if (DevicePtr == IntPtr.Zero) { throw new NotSupportedException("Failed to create Wayland data device"); }

        var addListenerRes = Native.wl_proxy_add_listener(
            DevicePtr,
            DeviceListeners.DangerousGetHandle(),
            OwnPtr);
        if (addListenerRes != 0) { throw new NotSupportedException("Failed to connect data device listeners"); }

        var initialSyncRes = Native.wl_display_roundtrip(DisplayPtr);
        if (initialSyncRes < 0) { throw new NotSupportedException("Failed to receive initial Wayland selection state"); }

        SourcePumpThread = new Thread(SourcePumpLoop) {
            IsBackground = true,
            Name = "WaylandClipboardSourcePump"
        };
        SourcePumpThread.Start();
    }

    ~PTClipboardWaylandProvider() {
        Dispose(disposing: false);
    }

    protected override void Dispose(bool disposing) {
        SourcePumpStop.Set();
        SourcePumpWake.Set();
        if (disposing) {
            SourcePumpThread?.Join();
        }

        ReleaseAllOffers();
        ReleaseSource(ClipboardSourcePtr);
        ReleaseSource(PrimarySourcePtr);
        if (DevicePtr != IntPtr.Zero) { Native.wl_proxy_destroy(DevicePtr); }
        DestroyProxy(ref ManagerPtr, Native.OPCODE_MANAGER_DESTROY);
        if (SeatPtr != IntPtr.Zero) { Native.wl_proxy_destroy(SeatPtr); }
        if (RegistryPtr != IntPtr.Zero) { Native.wl_proxy_destroy(RegistryPtr); }
        if (DisplayPtr != IntPtr.Zero) { Native.wl_display_disconnect(DisplayPtr); }

        GCHandle.FromIntPtr(OwnPtr).Free();

        if (disposing) {
            RegistryListeners.Dispose();
            DeviceListeners.Dispose();
            OfferListeners.Dispose();
            SourceListeners.Dispose();

            SourcePumpWake.Dispose();
            SourcePumpStop.Dispose();
        }
    }


    private readonly IntPtr OwnPtr;
    private readonly IntPtr DisplayPtr;
    private readonly IntPtr RegistryPtr;
    private readonly IntPtr DevicePtr;
    private DelegatesHandle RegistryListeners;
    private IntPtr SeatPtr;
    private IntPtr ManagerPtr;
    private IntPtr ClipboardOfferPtr;
    private IntPtr PrimaryOfferPtr;
    private IntPtr ClipboardSourcePtr;
    private IntPtr PrimarySourcePtr;
    private DelegatesHandle DeviceListeners;
    private DelegatesHandle OfferListeners;
    private DelegatesHandle SourceListeners;
    private readonly Thread SourcePumpThread;
    private readonly AutoResetEvent SourcePumpWake = new(false);
    private readonly ManualResetEventSlim SourcePumpStop = new(false);
    private Dictionary<IntPtr, OfferState> Offers { get; } = [];
    private Dictionary<IntPtr, SourceState> Sources { get; } = [];
    private static string[] SupportedTextMimeTypes { get; } = ["text/plain;charset=utf-8", "UTF8_STRING", "text/plain", "STRING", "TEXT"];


    private void RefreshOffers() {
        if (DevicePtr == IntPtr.Zero) { return; }
        if (Native.wl_display_roundtrip(DisplayPtr) < 0) {
            Debug.WriteLine("[PTClipboard:WL] Failed to refresh Wayland selection state");
            return;
        }
    }

    private string ReadTextFromOffer(IntPtr offerPtr) {
        if ((offerPtr == IntPtr.Zero) || !Offers.TryGetValue(offerPtr, out var offer)) {
            return string.Empty;
        }

        var mimeType = GetPreferredTextMimeType(offer);
        if (mimeType == null) {
            return string.Empty;
        }

        using var receivePipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.None);
        using var mimeTypeHandle = new Utf8StringHandle(mimeType);
        var receiveArgs = new Native.wl_argument[2];
        receiveArgs[0].s = mimeTypeHandle.DangerousGetHandle();
        receiveArgs[1].h = receivePipe.ClientSafePipeHandle.DangerousGetHandle().ToInt32();
        var pinnedArgs = GCHandle.Alloc(receiveArgs, GCHandleType.Pinned);
        try {
            Native.wl_proxy_marshal_array(
                offerPtr,
                Native.OPCODE_OFFER_RECEIVE,
                pinnedArgs.AddrOfPinnedObject());
        } finally {
            pinnedArgs.Free();
        }

        if (Native.wl_display_flush(DisplayPtr) < 0) { return string.Empty; }  // failed to flush Wayland clipboard receive request

        receivePipe.DisposeLocalCopyOfClientHandle();

        using var buffer = new MemoryStream();
        receivePipe.CopyTo(buffer);
        return System.Text.Encoding.UTF8.GetString(buffer.ToArray()).TrimEnd('\0');
    }

    private static string? GetPreferredTextMimeType(OfferState offer) {
        foreach (var mimeType in new[] { "text/plain;charset=utf-8", "text/plain;charset=UTF-8", "UTF8_STRING", "text/plain", "STRING", "TEXT" }) {
            if (offer.MimeTypes.Contains(mimeType)) {
                return mimeType;
            }
        }

        foreach (var mimeType in offer.MimeTypes) {
            if (mimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)) {
                return mimeType;
            }
        }

        return null;
    }

    private void ReleaseAllOffers() {
        var offerPtrs = new IntPtr[Offers.Count];
        Offers.Keys.CopyTo(offerPtrs, 0);
        foreach (var offerPtr in offerPtrs) {
            ReleaseOffer(offerPtr);
        }
        ClipboardOfferPtr = IntPtr.Zero;
        PrimaryOfferPtr = IntPtr.Zero;
    }

    private void ReleaseOfferIfUnused(IntPtr offerPtr) {
        if ((offerPtr == IntPtr.Zero) || (offerPtr == ClipboardOfferPtr) || (offerPtr == PrimaryOfferPtr)) { return; }
        ReleaseOffer(offerPtr);
    }

    private void ReleaseOffer(IntPtr offerPtr) {
        if ((offerPtr == IntPtr.Zero) || !Offers.Remove(offerPtr)) { return; }
        DestroyProxy(ref offerPtr, Native.OPCODE_OFFER_DESTROY);
    }

    private void SetSelectionText(string text, bool isPrimarySelection) {
        ObjectDisposedException.ThrowIf(WasDisposed, this);
        if (DevicePtr == IntPtr.Zero) {
            throw new NotSupportedException("Wayland data device is not available");
        }

        var sourcePtr = CreateTextSource(text ?? string.Empty);
        try {
            SetSelectionSource(sourcePtr, isPrimarySelection);
        } catch {
            ReleaseSource(sourcePtr);
            throw;
        }
    }

    private void SetSelectionSource(IntPtr sourcePtr, bool isPrimarySelection) {
        var setArgs = new Native.wl_argument[1];
        setArgs[0].o = sourcePtr;
        var pinnedArgs = GCHandle.Alloc(setArgs, GCHandleType.Pinned);
        try {
            Native.wl_proxy_marshal_array(
                DevicePtr,
                (uint)(isPrimarySelection ? Native.OPCODE_DEVICE_SET_PRIMARY_SELECTION : Native.OPCODE_DEVICE_SET_SELECTION),
                pinnedArgs.AddrOfPinnedObject());
        } finally {
            pinnedArgs.Free();
        }

        if (Native.wl_display_flush(DisplayPtr) < 0) {
            Debug.WriteLine("[PTClipboard:WL] Failed to flush Wayland selection update");
            return;
        }

        if (isPrimarySelection) {
            var previous = PrimarySourcePtr;
            PrimarySourcePtr = sourcePtr;
            ReleaseSourceIfUnused(previous);
        } else {
            var previous = ClipboardSourcePtr;
            ClipboardSourcePtr = sourcePtr;
            ReleaseSourceIfUnused(previous);
        }

        SourcePumpWake.Set();
    }

    private IntPtr CreateTextSource(string text) {
        if (ManagerPtr == IntPtr.Zero) {
            Debug.WriteLine("[PTClipboard:WL] Wayland data control manager is not available");
            return IntPtr.Zero;
        }

        var createArgs = new Native.wl_argument[1];
        var pinnedCreateArgs = GCHandle.Alloc(createArgs, GCHandleType.Pinned);
        IntPtr sourcePtr;
        try {
            sourcePtr = Native.wl_proxy_marshal_array_constructor_versioned(
                ManagerPtr,
                Native.OPCODE_MANAGER_CREATE_DATA_SOURCE,
                pinnedCreateArgs.AddrOfPinnedObject(),
                ExtDataControlSourceInterface.DangerousGetHandle(),
                version: 1);
        } finally {
            pinnedCreateArgs.Free();
        }
        if (sourcePtr == IntPtr.Zero) {
            throw new IOException("Failed to create Wayland data source");
        }

        try {
            var addListenerRes = Native.wl_proxy_add_listener(
                sourcePtr,
                SourceListeners.DangerousGetHandle(),
                OwnPtr);
            if (addListenerRes != 0) { throw new NotSupportedException("Failed to connect data source listeners"); }

            foreach (var mimeType in SupportedTextMimeTypes) {
                using var mimeTypeHandle = new Utf8StringHandle(mimeType);
                var offerArgs = new Native.wl_argument[1];
                offerArgs[0].s = mimeTypeHandle.DangerousGetHandle();
                var pinnedArgs = GCHandle.Alloc(offerArgs, GCHandleType.Pinned);
                try {
                    Native.wl_proxy_marshal_array(sourcePtr, Native.OPCODE_SOURCE_OFFER, pinnedArgs.AddrOfPinnedObject());
                } finally {
                    pinnedArgs.Free();
                }
            }

            Sources[sourcePtr] = new SourceState(sourcePtr, text);
            return sourcePtr;
        } catch {
            DestroyProxy(ref sourcePtr, Native.OPCODE_SOURCE_DESTROY);
            throw;
        }
    }

    private void ReleaseSourceIfUnused(IntPtr sourcePtr) {
        if ((sourcePtr == IntPtr.Zero) || (sourcePtr == ClipboardSourcePtr) || (sourcePtr == PrimarySourcePtr)) { return; }
        ReleaseSource(sourcePtr);
    }

    private void ReleaseSource(IntPtr sourcePtr) {
        if ((sourcePtr == IntPtr.Zero) || !Sources.Remove(sourcePtr)) { return; }

        if (sourcePtr == ClipboardSourcePtr) {
            ClipboardSourcePtr = IntPtr.Zero;
        }
        if (sourcePtr == PrimarySourcePtr) {
            PrimarySourcePtr = IntPtr.Zero;
        }

        DestroyProxy(ref sourcePtr, Native.OPCODE_SOURCE_DESTROY);

        if (Sources.Count == 0) {
            SourcePumpWake.Set();
        }
    }

    private const int SourcePumpWaitMilliseconds = 50;

    private void SourcePumpLoop() {
        var timeout = new Native.Timespec {
            tv_sec = IntPtr.Zero,
            tv_nsec = (IntPtr)(SourcePumpWaitMilliseconds * 1_000_000)
        };

        try {
            while (!SourcePumpStop.IsSet) {
                if (Sources.Count == 0) {
                    WaitHandle.WaitAny([SourcePumpWake, SourcePumpStop.WaitHandle], SourcePumpWaitMilliseconds);
                    continue;
                }

                var dispatchPendingResult = Native.wl_display_dispatch_pending(DisplayPtr);
                if (dispatchPendingResult < 0) { break; }

                var dispatchResult = Native.wl_display_dispatch_timeout(DisplayPtr, ref timeout);
                if (dispatchResult < 0) { break; }
            }
        } catch (ObjectDisposedException) {
        }
    }

    private static void DestroyProxy(ref IntPtr proxyPtr, int opcode) {
        if (proxyPtr == IntPtr.Zero) { return; }
        Native.wl_proxy_marshal_array(proxyPtr, (uint)opcode, IntPtr.Zero);
        Native.wl_proxy_destroy(proxyPtr);
        proxyPtr = IntPtr.Zero;
    }

}
