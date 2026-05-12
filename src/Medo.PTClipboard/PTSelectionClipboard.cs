/* Josip Medved <jmedved@jmedved.com> * www.medo64.com * MIT License */

namespace Medo;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

/// <summary>
/// Primary selection clipboard handling operations.
/// </summary>
public sealed class PTSelectionClipboard {

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    internal PTSelectionClipboard(PTClipboardProvider provider) {
        ArgumentNullException.ThrowIfNull(provider);
        Provider = provider;
    }


    private readonly PTClipboardProvider Provider;


    /// <summary>
    /// Returns true if selection clipboard service is available.
    /// </summary>
    public bool IsAvailable {
        get { return Provider.IsSelectionAvailable; }
    }

    /// <summary>
    /// Clears the selection clipboard.
    /// </summary>
    public void Clear() {
        Provider.ClearSelection();
    }

    /// <summary>
    /// Sets the selection clipboard text.
    /// </summary>
    public void SetText(string text) {
        Provider.SetSelectionText(text);
    }

    /// <summary>
    /// Gets the selection clipboard text.
    /// </summary>
    public string GetText() {
        return Provider.GetSelectionText();
    }

}
