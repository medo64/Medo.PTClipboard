/* Josip Medved <jmedved@jmedved.com> * www.medo64.com * MIT License */

namespace Medo;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

/// <summary>
/// Main clipboard handling operations.
/// </summary>
public sealed class PTMainClipboard {

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    internal PTMainClipboard(PTClipboardProvider provider, Lock syncRoot) {
        ArgumentNullException.ThrowIfNull(provider);
        Provider = provider;
        SyncRoot = syncRoot;
    }


    private readonly PTClipboardProvider Provider;
    private readonly Lock SyncRoot;


    /// <summary>
    /// Returns true if clipboard service is available.
    /// </summary>
    public bool IsAvailable {
        get {
            lock (SyncRoot) {
                return Provider.IsClipboardAvailable;
            }
        }
    }

    /// <summary>
    /// Clears the clipboard.
    /// </summary>
    public void Clear() {
        lock (SyncRoot) {
            Provider.ClearClipboard();
        }
    }

    /// <summary>
    /// Sets the clipboard text.
    /// </summary>
    /// <param name="text">Text.</param>
    public void SetText(string text) {
        lock (SyncRoot) {
            Provider.SetClipboardText(text);
        }
    }

    /// <summary>
    /// Gets the clipboard text.
    /// </summary>
    public string GetText() {
        lock (SyncRoot) {
            return Provider.GetClipboardText();
        }
    }

}
