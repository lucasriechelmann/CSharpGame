using System.Numerics;
using Engine.Rendering;

namespace Platformer;

// Display modes: pixel-perfect rendering, HDR with tone mapping, and multisampling. All of them live in the post-process chain.
public sealed partial class PlatformerGame
{
    /// <summary>The game image size in pixel-perfect mode: sprites are drawn at 1 world unit = 1 pixel of this, then scaled up by a whole number.</summary>
    private static readonly Vector2 PixelPerfectSize = new(480f, 270f);

    private static readonly int[] MsaaSteps = [1, 2, 4, 8];

    private ToneMapEffect? _toneMap;
    private LightingEffect _lighting = null!;

    /// <summary>The mouse position in the camera's own pixels: the window position, or the position inside the scaled game image in pixel-perfect mode.</summary>
    private Vector2 MouseOnCamera() =>
        PostProcess.IsPixelPerfect ? PostProcess.PixelPerfectLayout.WindowToGame(Input.MousePosition) : Input.MousePosition;

    private void HandleDisplayKeys()
    {
        if (Actions.WasPressed(GameActions.TogglePixelPerfect)) SetPixelPerfect(!PostProcess.IsPixelPerfect);
        if (Actions.WasPressed(GameActions.ToggleHdr)) SetHdr(!PostProcess.Hdr);
        if (Actions.WasPressed(GameActions.ToggleShadows))
        {
            _lighting.CastShadows = !_lighting.CastShadows;
            Log.Info("Game", _lighting.CastShadows ? "Light shadows on" : "Light shadows off");
        }
        if (Actions.WasPressed(GameActions.CycleMsaa))
        {
            int next = MsaaSteps[(Array.IndexOf(MsaaSteps, PostProcess.Samples) + 1) % MsaaSteps.Length];
            PostProcess.Samples = next;
            Log.Info("Game", next == 1 ? "Multisampling off" : $"Multisampling {next}x");
        }
    }

    private void SetPixelPerfect(bool on)
    {
        if (on)
        {
            PostProcess.SetPixelPerfect((int)PixelPerfectSize.X, (int)PixelPerfectSize.Y);
            _camera.ViewportSize = PixelPerfectSize;
            _camera.Zoom = 1f;
        }
        else
        {
            PostProcess.ClearPixelPerfect();
            _camera.ViewportSize = new Vector2(ViewportWidth, ViewportHeight);
        }

        _camera.PixelSnap = on;
        Log.Info("Game", on ? $"Pixel-perfect {PixelPerfectSize.X}x{PixelPerfectSize.Y}" : "Pixel-perfect off");
    }

    private void SetHdr(bool on)
    {
        PostProcess.Hdr = on;
        _toneMap!.Enabled = on;
        Log.Info("Game", on ? "HDR on (tone mapped)" : "HDR off");
    }
}
