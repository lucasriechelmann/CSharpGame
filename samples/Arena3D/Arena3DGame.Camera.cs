using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Rendering3D;

namespace Arena3D;

// The third-person camera: an orbit camera around the player. The captured mouse turns it, the wheel zooms, it eases after the player and it
// moves closer instead of going through walls.
public sealed partial class Arena3DGame
{
    private const float LookSensitivity = 0.0035f; // radians per pixel of mouse movement
    private const float MinZoom = 3f;
    private const float MaxZoom = 14f;
    private const float StartDistance = 7.5f;

    /// <summary>Metres above the capsule's centre the camera looks at, so the player sits a little below the middle of the picture.</summary>
    private const float LookHeight = 0.9f;

    /// <summary>How far in front of a wall the camera stops, so the near plane does not poke through it.</summary>
    private const float WallMargin = 0.3f;

    /// <summary>The ray starts this far behind the look target, just outside the player's own box (radius 0.4), so the player never blocks it.</summary>
    private const float RayStart = 0.6f;

    private readonly OrbitCamera _orbit = new() { Pitch = -0.5f, Distance = StartDistance };
    private float _zoom = StartDistance; // where the player wants the camera; the orbit's Distance is this, pulled in by walls
    private Vector2 _lastMouse;
    private bool _hasMouse;

    /// <summary>Turns the camera with the mouse; the first frame after a pause only records the mouse, so there is no jump.</summary>
    private void UpdateLook()
    {
        var mouse = Input.MousePosition;
        if (_hasMouse)
        {
            var delta = mouse - _lastMouse;
            _orbit.Orbit(-delta.X * LookSensitivity, -delta.Y * LookSensitivity); // mouse right = turn right = negative yaw
        }

        _hasMouse = true;
        _lastMouse = mouse;

        _zoom = Math.Clamp(_zoom / MathF.Pow(OrbitCamera.ZoomStep, Input.ScrollDelta), MinZoom, MaxZoom);
    }

    /// <summary>Eases the camera's target toward where the player is drawn this frame (between the last two fixed steps), then places the camera.</summary>
    private void FollowPlayer(double dt)
    {
        if (!World.IsAlive(_player)) return;

        var drawn = PreviousTransform.Interpolate(World.Get<Transform>(_player), World.Get<PreviousTransform>(_player), RenderClock.Alpha);
        var target = drawn.Position + new Vector3(0f, LookHeight, 0f);
        float follow = 1f - MathF.Exp(-14f * (float)dt);
        _orbit.Target = Vector3.Lerp(_orbit.Target, target, follow);
        _orbit.Distance = ClearDistance(_zoom);
        _orbit.ApplyTo(_camera);
    }

    /// <summary>
    /// How far behind the target the camera may sit: the wanted distance, or less when a wall is in the way. Casts a ray from just outside the player
    /// toward where the camera wants to be (the physics gathered solids in the last fixed step).
    /// </summary>
    private float ClearDistance(float wanted)
    {
        var back = -_orbit.Forward;
        var ray = new Ray(_orbit.Target + back * RayStart, back);
        float reach = wanted - RayStart + WallMargin;
        if (reach <= 0f || !_physics.Raycast(ray, reach, out var hit, out _)) return wanted;

        return Math.Max(RayStart + hit.Distance - WallMargin, OrbitCamera.MinDistance);
    }

    /// <summary>Puts the camera straight behind the start position, with no easing.</summary>
    private void SnapCamera()
    {
        _orbit.Yaw = 0f;
        _orbit.Pitch = -0.5f;
        _zoom = StartDistance;
        _orbit.Distance = StartDistance;
        _orbit.Target = _spawn + new Vector3(0f, LookHeight, 0f);
        _orbit.ApplyTo(_camera);
    }
}
