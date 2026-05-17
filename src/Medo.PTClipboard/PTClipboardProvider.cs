/* Josip Medved <jmedved@jmedved.com> * www.medo64.com * MIT License */

namespace Medo;

using System;
using System.Threading;

/// <summary>
/// Base clipboard provider.
/// </summary>
internal abstract class PTClipboardProvider : IDisposable {

    /// <summary>
    /// Returns true if clipboard service is available.
    /// </summary>
    public abstract bool IsClipboardAvailable { get; }

    /// <summary>
    /// Clears the clipboard.
    /// </summary>
    public abstract void ClearClipboard();

    /// <summary>
    /// Sets the clipboard text.
    /// </summary>
    /// <param name="text">Text.</param>
    public abstract void SetClipboardText(string text);

    /// <summary>
    /// Gets the clipboard text.
    /// </summary>
    public abstract string GetClipboardText();


    /// <summary>
    /// Returns true if selection clipboard service is available.
    /// </summary>
    public abstract bool IsSelectionAvailable { get; }

    /// <summary>
    /// Clears the selection clipboard.
    /// </summary>
    public abstract void ClearSelection();

    /// <summary>
    /// Sets the selection clipboard text.
    /// </summary>
    /// <param name="text">Text.</param>
    public abstract void SetSelectionText(string text);

    /// <summary>
    /// Gets the selection clipboard text.
    /// </summary>
    public abstract string GetSelectionText();


    #region IDisposable

    private int _wasDisposed;

    /// <summary>
    /// Gets if dispose already happened.
    /// </summary>
    protected bool WasDisposed => Volatile.Read(ref _wasDisposed) != 0;

    /// <summary>
    /// Dispose all resources used by the clipboard provider.
    /// </summary>
    public void Dispose() {
        if (Interlocked.CompareExchange(ref _wasDisposed, 1, 0) != 0) { return; }
        GC.SuppressFinalize(this);
        Dispose(disposing: true);
    }

    /// <summary>
    /// Dispose implementation.
    /// </summary>
    /// <param name="disposing">True if called from Dispose method.</param>
    protected abstract void Dispose(bool disposing);

    #endregion IDisposable

}
