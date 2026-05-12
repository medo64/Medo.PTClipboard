/* Josip Medved <jmedved@jmedved.com> * www.medo64.com * MIT License */

namespace Medo;

using System;

/// <summary>
/// Base clipboard provider.
/// </summary>
internal abstract class PTClipboardProvider {

    /// <summary>
    /// Returns true if provider is available.
    /// </summary>
    public abstract bool IsAvailable { get; }


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

}
