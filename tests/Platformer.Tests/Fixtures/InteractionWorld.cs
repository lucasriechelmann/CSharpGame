using System.Numerics;
using Engine.Audio;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Physics2D;

namespace Platformer.Tests.Fixtures;

/// <summary>
/// A tiny world with a hero at the origin and helpers to drop coins, creatures and the exit next to it, wired to a real
/// <see cref="InteractionSystem"/> with a silent audio device.
/// </summary>
internal sealed class InteractionWorld
{
    private static readonly AudioClip Clip = new(8000, 1, new byte[4]);

    public InteractionWorld(int coinsTotal)
    {
        Session = new GameSession(coinsTotal);
        Session.Tick(GameSession.StartGrace); // skip the start-of-run grace period so hits count unless a test wants it
        Hero = World.CreateEntity();
        World.Add(Hero, Transform.At(0f, 0f));
        World.Add(Hero, BoxCollider.Create(new Vector2(24f, 30f)));
        World.Add(Hero, PhysicsBody.Create());
        System = new InteractionSystem(Session, new NullAudioDevice("test"), new GameSounds(Clip, Clip, Clip, Clip, Clip, Clip), () => Hero);
    }

    public World World { get; } = new();

    public GameSession Session { get; }

    public Entity Hero { get; }

    public InteractionSystem System { get; }

    public void Step() => System.Update(World, 1.0 / 60);

    public Entity AddCoin(float x, float y, int value = 50)
    {
        var coin = World.CreateEntity();
        World.Add(coin, Transform.At(x, y));
        World.Add(coin, new Coin { Value = value });
        return coin;
    }

    public Entity AddCreature(float x, float y)
    {
        var creature = World.CreateEntity();
        World.Add(creature, Transform.At(x, y));
        World.Add(creature, BoxCollider.Create(new Vector2(20f, 20f)));
        World.Add(creature, new Patrol { Speed = 50f, Direction = 1f });
        return creature;
    }

    public Entity AddGoal(float x, float y)
    {
        var goal = World.CreateEntity();
        World.Add(goal, Transform.At(x, y));
        World.Add(goal, new Goal());
        return goal;
    }

    public void SetHeroPosition(float x, float y) => World.Get<Transform>(Hero).Position = new Vector3(x, y, 0f);

    public void SetHeroVelocity(float x, float y) => World.Get<PhysicsBody>(Hero).Velocity = new Vector2(x, y);
}
