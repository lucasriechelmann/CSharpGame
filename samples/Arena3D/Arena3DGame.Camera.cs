using System.Numerics;
using Engine.Core.Components;
using Engine.Rendering3D;

namespace Arena3D;

// The third-person camera: an orbit camera around the player. The mouse turns it while the right button is held, the wheel zooms, and it eases after the player.
public sealed partial class Arena3DGame
{
    private const float LookSensitivity = 0.0035f; // radians per pixel of mouse movement
    private const float MinZoom = 3f;
    private const float MaxZoom = 14f;
    private const float StartDistance = 7.5f;

    /// <summary>Metres above the capsule's centre the camera looks at, so the player sits a little below the middle of the picture.</summary>
    private const float LookHeight = 0.9f;

    private readonly OrbitCamera _orbit = new() { Pitch = -0.5f, Distance = StartDistance };
    private Vector2 _lastMouse;
    private bool _wasLooking;

    /// <summary>Turns the camera while the right mouse button is held; the first frame only records the mouse, so there is no jump.</summary>
    private void UpdateLook()
    {
        bool looking = Actions.IsDown(GameActions.LookHeld);
        var mouse = Input.MousePosition;
        if (looking && _wasLooking)
        {
            var delta = mouse - _lastMouse;
            _orbit.Orbit(-delta.X * LookSensitivity, -delta.Y * LookSensitivity); // mouse right = turn right = negative yaw
        }

        _wasLooking = looking;
        _lastMouse = mouse;

        _orbit.Zoom(Input.ScrollDelta);
        _orbit.Distance = Math.Clamp(_orbit.Distance, MinZoom, MaxZoom);
    }

    /// <summary>Eases the camera's target toward where the player is drawn this frame (between the last two fixed steps), then places the camera.</summary>
    private void FollowPlayer(double dt)
    {
        if (!World.IsAlive(_player)) return;

        var drawn = PreviousTransform.Interpolate(World.Get<Transform>(_player), World.Get<PreviousTransform>(_player), RenderClock.Alpha);
        var target = drawn.Position + new Vector3(0f, LookHeight, 0f);
        float follow = 1f - MathF.Exp(-14f * (float)dt);
        _orbit.Target = Vector3.Lerp(_orbit.Target, target, follow);
        _orbit.ApplyTo(_camera);
    }

    /// <summary>Puts the camera straight behind the start position, with no easing.</summary>
    private void SnapCamera()
    {
        _orbit.Yaw = 0f;
        _orbit.Pitch = -0.5f;
        _orbit.Distance = StartDistance;
        _orbit.Target = _spawn + new Vector3(0f, LookHeight, 0f);
        _orbit.ApplyTo(_camera);
    }
}
