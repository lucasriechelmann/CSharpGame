using System.Numerics;
using Engine.Rendering;
using Engine.Runtime.Views;
using ImGuiNET;

namespace Platformer;

// A radar in the corner: a second, zoomed-out picture of the level around the hero, drawn by a RenderView with effects of its own (washed out and
// sepia-tinted) while the main picture keeps its bloom and lights. M turns it on and off.
public sealed partial class PlatformerGame
{
    private const int MinimapWidth = 288;
    private const int MinimapHeight = 162;
    private const float MinimapZoom = 0.32f;

    private RenderView _minimap = null!;
    private Vector2 _savedPosition;
    private float _savedZoom;
    private Vector2 _savedViewport;
    private bool _savedSnap;

    /// <summary>Creates the radar view (call from <c>Load</c>, after the render systems exist).</summary>
    private void CreateMinimap()
    {
        _minimap = new RenderView(Gl, MinimapWidth, MinimapHeight, ApplyMinimapCamera, RestoreMainCamera) { ClearColor = new Vector4(0.02f, 0.02f, 0.05f, 1f) };
        _minimap.Chain.Add(new ColorGradeEffect(Gl) { Saturation = 0.25f, Contrast = 1.15f, Tint = new Vector3(1f, 0.9f, 0.7f) });
        _minimap.Chain.Add(new VignetteEffect(Gl));
        Views.Add(_minimap);
    }

    /// <summary>Points the shared camera at the hero from far away, remembering where the main picture had it.</summary>
    private void ApplyMinimapCamera()
    {
        _savedPosition = _camera.Position;
        _savedZoom = _camera.Zoom;
        _savedViewport = _camera.ViewportSize;
        _savedSnap = _camera.PixelSnap;

        _camera.Position = _viewingDemoMap ? _camera.Position : HeroDrawPosition();
        _camera.Zoom = MinimapZoom;
        _camera.ViewportSize = new Vector2(_minimap.Width, _minimap.Height);
        _camera.PixelSnap = false;
    }

    private void RestoreMainCamera()
    {
        _camera.Position = _savedPosition;
        _camera.Zoom = _savedZoom;
        _camera.ViewportSize = _savedViewport;
        _camera.PixelSnap = _savedSnap;
    }

    /// <summary>Draws the radar picture in the bottom-right corner of the game view.</summary>
    private void DrawMinimap()
    {
        if (!_minimap.Enabled || _minimap.Texture == null) return;

        ImGui.SetNextWindowPos(ViewportOrigin + new Vector2(ViewportWidth - 12f, ViewportHeight - 12f), ImGuiCond.Always, new Vector2(1f, 1f));
        ImGui.Begin("##minimap", HudFlags | ImGuiWindowFlags.NoBackground);
        ImGui.Image((IntPtr)_minimap.Texture.Handle, new Vector2(MinimapWidth, MinimapHeight), new Vector2(0f, 1f), new Vector2(1f, 0f), Vector4.One, new Vector4(1f, 0.85f, 0.5f, 1f));
        ImGui.TextDisabled("Radar (M)");
        ImGui.End();
    }
}
