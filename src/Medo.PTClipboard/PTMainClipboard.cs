/* Josip Medved <jmedved@jmedved.com> * www.medo64.com * MIT License */

namespace Medo;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

/// <summary>
/// Main clipboard handling operations.
/// </summary>
public sealed class PTMainClipboard {

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    internal PTMainClipboard(PTClipboardProvider provider) {
        ArgumentNullException.ThrowIfNull(provider);
        Provider = provider;
    }


    private readonly PTClipboardProvider Provider;


    /// <summary>
    /// Returns true if clipboard service is available.
    /// </summary>
    public bool IsAvailable {
        get { return Provider.IsClipboardAvailable; }
    }

    /// <summary>
    /// Clears the clipboard.
    /// </summary>
    public void Clear() {
       Provider.ClearClipboard();
    }

    /// <summary>
    /// Sets the clipboard text.
    /// </summary>
    /// <param name="text">Text.</param>
    public void SetText(string text) {
        Provider.SetClipboardText(text);
    }

    /// <summary>
    /// Gets the clipboard text.
    /// </summary>
    public string GetText() {
        return Provider.GetClipboardText();
    }

}
