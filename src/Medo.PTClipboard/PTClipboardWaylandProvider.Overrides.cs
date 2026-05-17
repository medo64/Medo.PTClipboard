namespace Medo;

using System;

partial class PTClipboardWaylandProvider {

    /// <summary>
    /// Returns true if clipboard service is available.
    /// </summary>
    public override bool IsClipboardAvailable => true;

    /// <summary>
    /// Clears the clipboard.
    /// </summary>
    public override void ClearClipboard() {
        ObjectDisposedException.ThrowIf(WasDisposed, this);
        SetSelectionSource(IntPtr.Zero, isPrimarySelection: false);
    }

    /// <summary>
    /// Sets the clipboard text.
    /// </summary>
    /// <param name="text">Text.</param>
    public override void SetClipboardText(string text) {
        ObjectDisposedException.ThrowIf(WasDisposed, this);
        SetSelectionText(text, isPrimarySelection: false);
    }

    /// <summary>
    /// Gets the clipboard text.
    /// </summary>
    public override string GetClipboardText() {
        ObjectDisposedException.ThrowIf(WasDisposed, this);
        if ((ClipboardSourcePtr != IntPtr.Zero) && Sources.TryGetValue(ClipboardSourcePtr, out var clipboardSource)) {
            return clipboardSource.Text;
        }
        RefreshOffers();
        return ReadTextFromOffer(ClipboardOfferPtr);
    }


    /// <summary>
    /// Returns true if selection clipboard service is available.
    /// </summary>
    public override bool IsSelectionAvailable => true;

    /// <summary>
    /// Clears the selection clipboard.
    /// </summary>
    public override void ClearSelection() {
        ObjectDisposedException.ThrowIf(WasDisposed, this);
        SetSelectionSource(IntPtr.Zero, isPrimarySelection: true);
    }

    /// <summary>
    /// Sets the selection clipboard text.
    /// </summary>
    /// <param name="text">Text.</param>
    public override void SetSelectionText(string text) {
        ObjectDisposedException.ThrowIf(WasDisposed, this);
        SetSelectionText(text, isPrimarySelection: true);
    }

    /// <summary>
    /// Gets the selection clipboard text.
    /// </summary>
    public override string GetSelectionText() {
        ObjectDisposedException.ThrowIf(WasDisposed, this);
        if ((PrimarySourcePtr != IntPtr.Zero) && Sources.TryGetValue(PrimarySourcePtr, out var primarySource)) {
            return primarySource.Text;
        }
        RefreshOffers();
        return ReadTextFromOffer(PrimaryOfferPtr);
    }

}
