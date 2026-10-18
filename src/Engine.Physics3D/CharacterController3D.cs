using System.Numerics;

namespace Engine.Physics3D;

/// <summary>
/// Makes a capsule <see cref="PhysicsBody3D"/> walk and jump like a game character. The game writes what the player wants into
/// <see cref="Move"/> and <see cref="JumpPressed"/> each frame; <see cref="CharacterControllerSystem3D"/> turns that into velocity before
/// physics runs. Plain data, so it saves in scenes and the inspector can tune the feel live.
/// </summary>
public struct CharacterController3D
{
    /// <summary>Top walking speed in metres per second at full input.</summary>
    public float MoveSpeed;

    /// <summary>How quickly horizontal speed changes towards the wanted speed on the ground, in metres per second squared. Also what stops the character when input is released, since ground friction does not apply to controlled bodies.</summary>
    public float Acceleration;

    /// <summary>The same in the air: lower than on the ground, so a jump commits the character to a direction.</summary>
    public float AirAcceleration;

    /// <summary>Upward speed given by a jump, in metres per second (about <c>sqrt(2 * 9.81 * height)</c> for a jump of that height).</summary>
    public float JumpSpeed;

    /// <summary>Seconds after walking off an edge in which a jump still works.</summary>
    public float CoyoteTime;

    /// <summary>Seconds a jump press is remembered while in the air, so one pressed just before landing still fires.</summary>
    public float JumpBufferTime;

    /// <summary>Intent: the wanted horizontal direction in world space, X along world X and Y along world Z, up to length 1 (longer is clamped). The game rotates input by the camera's yaw.</summary>
    public Vector2 Move;

    /// <summary>Intent: set when a jump was pressed; the system takes it and clears it. Latch it from per-frame input, never read the key in a fixed step.</summary>
    public bool JumpPressed;

    /// <summary>Runtime: time left in which a jump is still allowed after leaving the ground.</summary>
    public float CoyoteTimer;

    /// <summary>Runtime: time left in which a remembered jump press can still fire on landing.</summary>
    public float JumpBufferTimer;

    /// <summary>Creates a controller with a sensible feel: 5 m/s, quick ground response, a 1 m jump and forgiving timers.</summary>
    public static CharacterController3D Create() => new()
    {
        MoveSpeed = 5f,
        Acceleration = 40f,
        AirAcceleration = 12f,
        JumpSpeed = 5.5f,
        CoyoteTime = 0.1f,
        JumpBufferTime = 0.12f,
    };
}
