/* Josip Medved <jmedved@jmedved.com> * www.medo64.com * MIT License */

namespace Medo;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

/// <summary>
/// Windows clipboard handling operations.
/// </summary>
internal sealed partial class PTClipboardWin32Provider : PTClipboardProvider {

    internal PTClipboardWin32Provider() {
        try {  // just test if user32 is available
            Native.OpenClipboard(IntPtr.Zero);
            Native.CloseClipboard();
        } catch (DllNotFoundException) {
            throw new NotSupportedException("Cannot load user32.dll");
        }
    }


    private string? SelectionContent;


    private static bool TryOpenClipboard() {
        for (var attempt = 0; attempt < 10; attempt++) {
            if (Native.OpenClipboard(IntPtr.Zero)) {
                return true;
            }
            Thread.Sleep(10);
        }

        Debug.WriteLine($"[PTClipboard:Win32] OpenClipboard failed ({Marshal.GetLastPInvokeError()})");
        return false;
    }


    protected override void Dispose(bool disposing) {
        // nothing to dispose
    }

}
