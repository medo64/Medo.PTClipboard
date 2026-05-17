/* Josip Medved <jmedved@jmedved.com> * www.medo64.com * MIT License */

namespace Medo;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

/// <summary>
/// Primary selection clipboard handling operations.
/// </summary>
public sealed class PTSelectionClipboard {

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    internal PTSelectionClipboard(PTClipboardProvider provider, Lock syncRoot) {
        ArgumentNullException.ThrowIfNull(provider);
        Provider = provider;
        SyncRoot = syncRoot;
    }


    private readonly PTClipboardProvider Provider;
    private readonly Lock SyncRoot;


    /// <summary>
    /// Returns true if selection clipboard service is available.
    /// </summary>
    public bool IsAvailable {
        get {
            lock (SyncRoot) {
                return Provider.IsSelectionAvailable;
            }
        }
    }

    /// <summary>
    /// Clears the selection clipboard.
    /// </summary>
    public void Clear() {
        lock (SyncRoot) {
            Provider.ClearSelection();
        }
    }

    /// <summary>
    /// Sets the selection clipboard text.
    /// </summary>
    public void SetText(string text) {
        lock (SyncRoot) {
            Provider.SetSelectionText(text);
        }
    }

    /// <summary>
    /// Gets the selection clipboard text.
    /// </summary>
    public string GetText() {
        lock (SyncRoot) {
            return Provider.GetSelectionText();
        }
    }

}
