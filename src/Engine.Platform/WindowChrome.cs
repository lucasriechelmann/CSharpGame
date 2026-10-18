using System.Runtime.InteropServices;
using Silk.NET.Windowing;

namespace Engine.Platform;

/// <summary>Small Windows-only touches on the window frame that Silk.NET has no setting for. Every call quietly does nothing on other systems or older Windows versions.</summary>
internal static class WindowChrome
{
    private const int DwmUseImmersiveDarkMode = 20; // DWMWA_USE_IMMERSIVE_DARK_MODE

    /// <summary>Switches the window's title bar to its dark variant.</summary>
    /// <param name="window">The window to change; it must already exist.</param>
    public static void UseDarkTitleBar(IWindow window)
    {
        if (!OperatingSystem.IsWindows()) return;

        var win32 = window.Native?.Win32;
        if (win32 is null) return;

        int on = 1;
        try
        {
            _ = DwmSetWindowAttribute(win32.Value.Hwnd, DwmUseImmersiveDarkMode, ref on, sizeof(int));
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            // An old Windows without this attribute: the title bar simply stays light.
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);
}
