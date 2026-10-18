using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Engine.Editor;

/// <summary>
/// The editor's default window layout: the Scene in the middle, the Hierarchy on the left, the Inspector on the right, and the log, history,
/// profiler and other tools as tabs along the bottom. ImGui.NET does not wrap the dock-builder functions (they are part of ImGui's
/// internal API), but the native library exports them, so they are called directly. Windows are placed by title, so a window that does
/// not exist (a game's own debug panel) is simply docked when it first appears.
/// </summary>
internal static unsafe class DockLayout
{
    /// <summary>The id of the editor's dock space; the layout and the dock space call must agree on it.</summary>
    public const uint DockSpaceId = 0x0ED17000;

    private const int DirectionLeft = 0;
    private const int DirectionRight = 1;
    private const int DirectionDown = 3;
    private const int DockSpaceFlag = 1 << 10; // ImGuiDockNodeFlags_DockSpace, an internal flag

    /// <summary>Windows tabbed along the bottom, in tab order.</summary>
    private static readonly string[] BottomWindows = ["Assets", "Log", "History", "Profiler", "Engine", "Post effects", "Sample", "Hero feel", "3D scene"];

    /// <summary>Windows tabbed on the right next to the Inspector.</summary>
    private static readonly string[] RightWindows = ["Inspector", "Tiles"];

    /// <summary>Throws the current arrangement away and builds the default one for a dock space of the given size.</summary>
    /// <param name="size">The size of the area the dock space covers.</param>
    public static void ApplyDefault(Vector2 size)
    {
        igDockBuilderRemoveNode(DockSpaceId);
        igDockBuilderAddNode(DockSpaceId, DockSpaceFlag);
        igDockBuilderSetNodeSize(DockSpaceId, size);

        uint left, rest, right, centreAndBottom, bottom, centre;
        igDockBuilderSplitNode(DockSpaceId, DirectionLeft, 0.18f, &left, &rest);
        igDockBuilderSplitNode(rest, DirectionRight, 0.24f, &right, &centreAndBottom);
        igDockBuilderSplitNode(centreAndBottom, DirectionDown, 0.28f, &bottom, &centre);

        Dock("Hierarchy", left);
        Dock(SceneEditor.ScenePanelTitle, centre);
        foreach (string window in RightWindows) Dock(window, right);
        foreach (string window in BottomWindows) Dock(window, bottom);
        igDockBuilderFinish(DockSpaceId);
    }

    private static void Dock(string window, uint node)
    {
        byte[] name = Encoding.UTF8.GetBytes(window + "\0");
        fixed (byte* pointer = name) igDockBuilderDockWindow(pointer, node);
    }

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    private static extern void igDockBuilderRemoveNode(uint nodeId);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint igDockBuilderAddNode(uint nodeId, int flags);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    private static extern void igDockBuilderSetNodeSize(uint nodeId, Vector2 size);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint igDockBuilderSplitNode(uint nodeId, int direction, float sizeRatio, uint* outIdAtDirection, uint* outIdAtOppositeDirection);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    private static extern void igDockBuilderDockWindow(byte* windowName, uint nodeId);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    private static extern void igDockBuilderFinish(uint nodeId);
}
