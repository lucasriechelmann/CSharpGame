using System.Numerics;
using Engine.Core.Components;
using Engine.Physics3D;

namespace Arena3D;

// The player's look: it turns to face where it walks and leans into the run. The capsule collider ignores rotation, so this is purely visual.
public sealed partial class Arena3DGame
{
    private const float TurnSpeed = 12f;       // how quickly the body swings round (1/s)
    private const float MaxLean = 0.2f;        // radians of forward lean at full speed
    private const float FullSpeed = 5f;        // metres per second that counts as a full run (the controller's move speed)

    private float _facingYaw;

    /// <summary>Faces <paramref name="wish"/> (the walk direction on the ground) and leans by how fast the player really moves.</summary>
    private void AnimatePlayer(Vector3 wish, double dt)
    {
        if (wish.LengthSquared() > 1e-4f)
        {
            float target = MathF.Atan2(-wish.X, -wish.Z); // the model's front is -Z
            float turn = target - _facingYaw;
            turn -= MathF.Tau * MathF.Round(turn / MathF.Tau); // the short way round
            _facingYaw += turn * (1f - MathF.Exp(-TurnSpeed * (float)dt));
        }

        var velocity = World.Get<PhysicsBody3D>(_player).Velocity;
        float speed = new Vector2(velocity.X, velocity.Z).Length();
        float lean = -MaxLean * Math.Clamp(speed / FullSpeed, 0f, 1f); // negative pitch tips the head toward the front (-Z)
        World.Get<Transform>(_player).Rotation = Quaternion.CreateFromYawPitchRoll(_facingYaw, lean, 0f);
    }
}
