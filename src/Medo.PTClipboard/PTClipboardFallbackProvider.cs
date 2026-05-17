/* Josip Medved <jmedved@jmedved.com> * www.medo64.com * MIT License */

namespace Medo;

using System;

/// <summary>
/// Fallback clipboard implementation.
/// </summary>
internal sealed class PTClipboardFallbackProvider : PTClipboardProvider {

    public PTClipboardFallbackProvider() {
    }


    private string? ClipboardContent;
    private string? SelectionContent;


    /// <summary>
    /// Returns true if clipboard service is available.
    /// </summary>
    public override bool IsClipboardAvailable => true;

    /// <summary>
    /// Clears the clipboard.
    /// </summary>
    public override void ClearClipboard() {
        ClipboardContent = null;
    }

    /// <summary>
    /// Sets the clipboard text.
    /// </summary>
    /// <param name="text">Text.</param>
    public override void SetClipboardText(string text) {
        ClipboardContent = text;
    }

    /// <summary>
    /// Gets the clipboard text.
    /// </summary>
    public override string GetClipboardText() {
        return ClipboardContent ?? string.Empty;
    }


    /// <summary>
    /// Returns true if selection clipboard service is available.
    /// </summary>
    public override bool IsSelectionAvailable => true;

    /// <summary>
    /// Clears the selection clipboard.
    /// </summary>
    public override void ClearSelection() {
        SelectionContent = null;
    }

    /// <summary>
    /// Sets the selection clipboard text.
    /// </summary>
    public override void SetSelectionText(string text) {
        SelectionContent = text;
    }

    /// <summary>
    /// Gets the selection clipboard text.
    /// </summary>
    public override string GetSelectionText() {
        return SelectionContent ?? string.Empty;
    }


    protected override void Dispose(bool disposing) {
        // nothing to dispose
    }

}
