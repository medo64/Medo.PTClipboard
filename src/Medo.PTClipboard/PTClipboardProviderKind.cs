namespace Medo;

using System;

/// <summary>
/// Clipboard provider kind.
/// </summary>
public enum PTClipboardProviderKind {

    /// <summary>
    /// Auto-detect clipboard provider.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Force Wayland clipboard provider.
    /// </summary>
    ForceWayland = 7687,

    /// <summary>
    /// Force X11 clipboard provider.
    /// </summary>
    ForceX11 = 7688,

    /// <summary>
    /// Force Win32 clipboard provider.
    /// </summary>
    ForceWin32 = 8700,

    /// <summary>
    /// Use fallback clipboard provider.
    /// </summary>
    None = 2147483647,

}
