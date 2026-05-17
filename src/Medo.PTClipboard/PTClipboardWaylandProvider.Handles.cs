namespace Medo;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

partial class PTClipboardWaylandProvider {

    private static InterfaceHandle RegistryInterface { get; } = new(
        "wl_registry",
        1,
        [new("bind", "usun")],
        [new("global", "usu"), new("global_remove", "u")]
    );

    private static InterfaceHandle SeatInterface { get; } = new(
        "wl_seat",
        9,
        [],
        [new("capabilities", "u"), new("name", "2s")]
    );

    private static InterfaceHandle ExtDataControlOfferInterface { get; } = new(
        "ext_data_control_offer_v1",
        1,
        [new("receive", "sh"), new("destroy", "")],
        [new("offer", "s")]
    );

    private static InterfaceHandle ExtDataControlSourceInterface { get; } = new(
        "ext_data_control_source_v1",
        1,
        [new("offer", "s"), new("destroy", "")],
        [new("send", "sh"), new("cancelled", "")]
    );

    private static InterfaceHandle ExtDataControlDeviceInterface { get; } = new(
        "ext_data_control_device_v1",
        1,
        [
            new("set_selection", "?o", [ExtDataControlSourceInterface]),
            new("destroy", ""),
            new("set_primary_selection", "?o", [ExtDataControlSourceInterface])
        ],
        [
            new("data_offer", "n", [ExtDataControlOfferInterface]),
            new("selection", "?o", [ExtDataControlOfferInterface]),
            new("finished", ""),
            new("primary_selection", "?o", [ExtDataControlOfferInterface])
        ]
    );

    private static InterfaceHandle ExtDataControlManagerInterface { get; } = new(
        "ext_data_control_manager_v1",
        1,
        [
            new("create_data_source", "n", [ExtDataControlSourceInterface]),
            new("get_data_device", "no", [ExtDataControlDeviceInterface, SeatInterface]),
            new("destroy", "")
        ],
        []
    );

    internal readonly record struct MessageDefinition(string Name, string Signature, InterfaceHandle?[]? Types = null);

    internal sealed class InterfaceHandle : SafeHandle {
        public InterfaceHandle(string name, int version, MessageDefinition[] methods, MessageDefinition[] events)
            : base(IntPtr.Zero, ownsHandle: true) {
            NameHandle = new Utf8StringHandle(name);
            var methodsHandle = new RegistryMessagesHandle(methods);
            var eventsHandle = new RegistryMessagesHandle(events);
            var @interface = new Native.wl_interface {
                name = NameHandle.DangerousGetHandle(),
                version = version,
                methodCount = methods.Length,
                methods = methodsHandle.DangerousGetHandle(),
                eventCount = events.Length,
                events = eventsHandle.DangerousGetHandle()
            };
            PinnedInterfaceGCHandle = GCHandle.Alloc(@interface, GCHandleType.Pinned);

            UsedHandles.Add(NameHandle);
            UsedHandles.Add(methodsHandle);
            UsedHandles.Add(eventsHandle);
            SetHandle(PinnedInterfaceGCHandle.AddrOfPinnedObject());
            Debug.WriteLine($"[PTClipboard:WL] Interface handle 0x{handle:x2} created");
        }

        public override bool IsInvalid => (handle == IntPtr.Zero);
        public Utf8StringHandle NameHandle { get; }
        private GCHandle PinnedInterfaceGCHandle;
        private readonly List<SafeHandle> UsedHandles = [];

        protected override bool ReleaseHandle() {
            Debug.WriteLine($"[PTClipboard:WL] Interface handle 0x{handle:x2} releasing");
            if (PinnedInterfaceGCHandle.IsAllocated) { PinnedInterfaceGCHandle.Free(); }
            foreach (var usedHandle in UsedHandles) {
                if (!usedHandle.IsInvalid) { usedHandle.Dispose(); }
            }
            Debug.WriteLine($"[PTClipboard:WL] Interface handle 0x{handle:x2} released");
            handle = IntPtr.Zero;
            return true;
        }
    }

    internal sealed class Utf8StringHandle : SafeHandle {
        public Utf8StringHandle(string value)
            : base(IntPtr.Zero, ownsHandle: true) {
            SetHandle(Marshal.StringToCoTaskMemUTF8(value));
        }

        public override bool IsInvalid => (handle == IntPtr.Zero);

        protected override bool ReleaseHandle() {
            Marshal.FreeCoTaskMem(handle);
            return true;
        }
    }

    internal sealed class RegistryMessagesHandle : SafeHandle {

        public RegistryMessagesHandle(params MessageDefinition[] messages)
            : base(IntPtr.Zero, ownsHandle: true) {

            var size = Marshal.SizeOf<Native.wl_message>();
            var buffer = Marshal.AllocHGlobal(size * messages.Length);
            for (int i = 0; i < messages.Length; i++) {
                var nameHandle = new Utf8StringHandle(messages[i].Name);
                var signatureHandle = new Utf8StringHandle(messages[i].Signature);
                var typesHandle = new MessageTypesHandle(messages[i].Types);

                UsedHandles.Add(nameHandle);
                UsedHandles.Add(signatureHandle);
                UsedHandles.Add(typesHandle);

                var message = new Native.wl_message {
                    name = nameHandle.DangerousGetHandle(),
                    signature = signatureHandle.DangerousGetHandle(),
                    types = typesHandle.DangerousGetHandle()
                };

                Marshal.StructureToPtr(message, buffer + i * size, false);
            }
            SetHandle(buffer);
        }

        public override bool IsInvalid => (handle == IntPtr.Zero);
        private List<SafeHandle> UsedHandles = [];

        protected override bool ReleaseHandle() {
            Marshal.FreeHGlobal(handle);
            foreach (var usedHandle in UsedHandles) {
                usedHandle.Dispose();
            }
            return true;
        }
    }

    internal sealed class MessageTypesHandle : SafeHandle {
        public MessageTypesHandle(InterfaceHandle?[]? types)
            : base(IntPtr.Zero, ownsHandle: true) {
            if ((types == null) || (types.Length == 0)) {
                return;
            }

            var buffer = Marshal.AllocHGlobal(IntPtr.Size * types.Length);
            for (var i = 0; i < types.Length; i++) {
                Marshal.WriteIntPtr(buffer, i * IntPtr.Size, types[i]?.DangerousGetHandle() ?? IntPtr.Zero);
            }
            SetHandle(buffer);
        }

        public override bool IsInvalid => (handle == IntPtr.Zero);

        protected override bool ReleaseHandle() {
            if (handle != IntPtr.Zero) {
                Marshal.FreeHGlobal(handle);
                handle = IntPtr.Zero;
            }
            return true;
        }
    }

    internal sealed class DelegatesHandle : SafeHandle {
        public DelegatesHandle(params Delegate[] delegates)
            : base(IntPtr.Zero, ownsHandle: true) {
            // Keep the delegate objects alive: GetFunctionPointerForDelegate returns a raw
            // function pointer and does NOT root the delegate; if the delegate is collected,
            // the thunk is freed and the pointer becomes dangling.
            _delegates = new Delegate[delegates.Length];
            Array.Copy(delegates, _delegates, delegates.Length);

            var handle = Marshal.AllocHGlobal(IntPtr.Size * delegates.Length);
            for (var i = 0; i < delegates.Length; i++) {
                Marshal.WriteIntPtr(handle, i * IntPtr.Size, Marshal.GetFunctionPointerForDelegate<Delegate>(delegates[i]));
            }
            SetHandle(handle);
        }

        public override bool IsInvalid => (handle == IntPtr.Zero);
        private readonly Delegate[] _delegates;

        protected override bool ReleaseHandle() {
            Marshal.FreeHGlobal(handle);
            handle = IntPtr.Zero;
            return true;
        }
    }

}
