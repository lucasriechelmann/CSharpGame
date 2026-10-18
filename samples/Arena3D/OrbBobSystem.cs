using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;

namespace Arena3D;

/// <summary>Animates every <see cref="OrbBob"/>: rises and falls around its resting height and turns. A per-frame system; the orbs have no physics, so nothing needs interpolating.</summary>
public sealed class OrbBobSystem : ISystem
{
    private readonly EntityAction<Transform, OrbBob> _animate;
    private float _time;

    /// <summary>Creates the system.</summary>
    public OrbBobSystem() => _animate = Animate; // Created once so the per-frame query does not allocate a delegate.

    /// <inheritdoc />
    public void Update(World world, double dt)
    {
        _time += (float)dt;
        world.Query(_animate);
    }

    private void Animate(Entity entity, ref Transform transform, ref OrbBob bob)
    {
        if (!bob.Started)
        {
            bob.RestHeight = transform.Position.Y;
            bob.Started = true;
        }

        transform.Position = transform.Position with { Y = bob.RestHeight + MathF.Sin(_time * bob.Speed + bob.Phase) * bob.Amplitude };
        transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, _time * bob.Spin + bob.Phase);
    }
}
