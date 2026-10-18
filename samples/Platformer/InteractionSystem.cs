using System.Numerics;
using Engine.Audio;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Physics2D;

namespace Platformer;

/// <summary>
/// Turns the hero touching things into game events: coins are collected, creatures are stomped (from above, while falling) or hurt
/// the hero (from the side or below), and the exit ends the run. It only <i>detects</i> overlaps and reports to the
/// <see cref="GameSession"/>, which owns the rules. Run it in the fixed group after <see cref="PhysicsSystem"/> so it sees the
/// positions the step settled on.
/// </summary>
/// <remarks>
/// Destroying entities mid-query is not allowed, so removals go through a <see cref="CommandBuffer"/> played back after the
/// queries. Pickups are rare events, so the buffer's small per-command allocation is accepted.
/// </remarks>
public sealed class InteractionSystem : ISystem
{
    /// <summary>Side of the square around a coin that counts as touching it, in world units (the art is smaller than its sprite).</summary>
    private const float CoinPickupSize = 24f;

    /// <summary>Side of the square around the exit that counts as touching it.</summary>
    private const float GoalSize = 28f;

    /// <summary>Upward speed the hero gets after stomping a creature.</summary>
    private const float StompBounce = 420f;

    /// <summary>Speed the hero is thrown away from a creature that hurt it (sideways, upwards).</summary>
    private static readonly Vector2 Knockback = new(240f, 380f);

    private readonly GameSession _session;
    private readonly IAudioDevice _audio;
    private readonly GameSounds _sounds;
    private readonly Func<Entity> _hero;
    private readonly CommandBuffer _buffer = new();
    private readonly EntityAction<Transform, Coin> _touchCoin;
    private readonly EntityAction<Transform, Patrol> _touchCreature;
    private readonly EntityAction<Transform, Goal> _touchGoal;
    private World _world = null!;
    private Entity _heroEntity;
    private Aabb _heroBox;

    /// <summary>Creates the system.</summary>
    /// <param name="session">Receives the events and decides their consequences.</param>
    /// <param name="audio">Device the event sounds play on.</param>
    /// <param name="sounds">The clips to play.</param>
    /// <param name="hero">Returns the current hero entity (it changes when the level is restarted or reloaded).</param>
    public InteractionSystem(GameSession session, IAudioDevice audio, GameSounds sounds, Func<Entity> hero)
    {
        _session = session;
        _audio = audio;
        _sounds = sounds;
        _hero = hero;
        _touchCoin = TouchCoin; // Created once so the per-step queries do not allocate delegates.
        _touchCreature = TouchCreature;
        _touchGoal = TouchGoal;
    }

    /// <inheritdoc />
    public void Update(World world, double dt)
    {
        _heroEntity = _hero();
        if (_session.Phase != GamePhase.Playing || !world.Has<BoxCollider>(_heroEntity) || !world.Has<Transform>(_heroEntity)) return;

        _world = world;
        _heroBox = world.Get<BoxCollider>(_heroEntity).GetBounds(world.Get<Transform>(_heroEntity).Position);

        world.Query(_touchCoin);
        world.Query(_touchCreature);
        world.Query(_touchGoal);
        _buffer.Playback(world);
    }

    private void TouchCoin(Entity entity, ref Transform transform, ref Coin coin)
    {
        var box = Aabb.FromCenter(new Vector2(transform.Position.X, transform.Position.Y), new Vector2(CoinPickupSize));
        if (!_heroBox.Overlaps(box)) return;

        _session.CollectCoin(coin.Value);
        _buffer.Destroy(entity);
        _audio.Play(_sounds.Coin, volume: 0.6f, pitch: 1f + 0.03f * Math.Min(_session.CoinsCollected, 10));
        if (_session.GoalUnlocked) _audio.Play(_sounds.Unlock, volume: 0.7f);
    }

    private void TouchCreature(Entity entity, ref Transform transform, ref Patrol patrol)
    {
        if (!_world.TryGet<BoxCollider>(entity, out var collider)) return;

        var box = collider.GetBounds(transform.Position);
        if (!_heroBox.Overlaps(box)) return;

        ref var body = ref _world.Get<PhysicsBody>(_heroEntity);

        // A stomp is landing on top: moving down, with the hero's feet above the creature's middle.
        if (body.Velocity.Y < 0f && _heroBox.Min.Y >= box.Center.Y)
        {
            _session.StompKill();
            _buffer.Destroy(entity);
            body.Velocity.Y = StompBounce;
            _audio.Play(_sounds.Stomp, volume: 0.7f);
            return;
        }

        if (!_session.TryHurt()) return;

        float away = _heroBox.Center.X >= box.Center.X ? 1f : -1f;
        body.Velocity = new Vector2(away * Knockback.X, Knockback.Y);
        _audio.Play(_session.Phase == GamePhase.Lost ? _sounds.Lose : _sounds.Hurt, volume: 0.8f);
    }

    private void TouchGoal(Entity entity, ref Transform transform, ref Goal goal)
    {
        var box = Aabb.FromCenter(new Vector2(transform.Position.X, transform.Position.Y), new Vector2(GoalSize));
        if (_heroBox.Overlaps(box) && _session.TryReachGoal())
            _audio.Play(_sounds.Win, volume: 0.8f);
    }
}
