using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;
using Engine.Physics3D;

namespace Arena3D;

/// <summary>
/// Detects the player touching an orb: any <see cref="Orb"/> within its radius of the entity that has a <see cref="CharacterController3D"/> is removed and
/// reported to the game's rules through a callback. A fixed-step system that runs after physics, so it sees where the player really is.
/// </summary>
public sealed class OrbCollectSystem : ISystem
{
    private readonly Action _onCollected;
    private readonly CommandBuffer _commands = new();
    private readonly EntityAction<CharacterController3D, Transform> _findPlayer;
    private readonly EntityAction<Transform, Orb> _check;
    private World _world = null!;
    private Vector3 _player;
    private bool _hasPlayer;

    /// <summary>Creates the system.</summary>
    /// <param name="onCollected">Called once for every orb collected.</param>
    public OrbCollectSystem(Action onCollected)
    {
        _onCollected = onCollected;
        _findPlayer = FindPlayer;
        _check = Check;
    }

    /// <inheritdoc />
    public void Update(World world, double dt)
    {
        _world = world;
        _hasPlayer = false;
        world.Query(_findPlayer);
        if (!_hasPlayer) return;

        world.Query(_check);
        _commands.Playback(world); // removing entities while querying is not allowed, so they go through the command buffer
    }

    private void FindPlayer(Entity entity, ref CharacterController3D controller, ref Transform transform)
    {
        if (_hasPlayer) return;

        _player = TransformHierarchy.GetWorldPosition(_world, entity, transform);
        _hasPlayer = true;
    }

    private void Check(Entity entity, ref Transform transform, ref Orb orb)
    {
        var position = TransformHierarchy.GetWorldPosition(_world, entity, transform);
        if (Vector3.DistanceSquared(position, _player) > orb.Radius * orb.Radius) return;

        _commands.Destroy(entity);
        _onCollected();
    }
}
