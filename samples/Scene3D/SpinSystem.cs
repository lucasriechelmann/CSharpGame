using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;

namespace Scene3D;

/// <summary>Rotates every entity that has a <see cref="Spin"/>. A frame system, so it stops while the game is paused or the editor is open.</summary>
public sealed class SpinSystem : ISystem
{
    private readonly EntityAction<Transform, Spin> _apply;
    private float _dt;

    /// <summary>Creates the system.</summary>
    public SpinSystem() => _apply = Apply; // created once so the per-frame query does not allocate a delegate

    /// <inheritdoc />
    public void Update(World world, double dt)
    {
        _dt = (float)dt;
        world.Query(_apply);
    }

    private void Apply(Entity entity, ref Transform transform, ref Spin spin)
    {
        if (spin.Axis.LengthSquared() < 1e-12f) return;

        var turn = Quaternion.CreateFromAxisAngle(Vector3.Normalize(spin.Axis), spin.Speed * _dt);
        transform.Rotation = Quaternion.Normalize(turn * transform.Rotation); // turn applied after the current orientation = spin about a world axis
    }
}
