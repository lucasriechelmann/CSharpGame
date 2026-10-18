using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Physics2D;

namespace Engine.Physics2D.Tests.Fixtures;

/// <summary>
/// A tiny simulated level for controller tests: a 40x20 grid of 16-unit tiles with a solid floor row (top at y = 16),
/// and one hero (16x24 collider) driven by the controller and physics systems at a power-of-two timestep so the
/// arithmetic is exact. The hero starts at the given height and the sim runs two settling steps.
/// </summary>
internal sealed class PlatformerSim
{
    /// <summary>Fixed timestep (1/64 s).</summary>
    public const float Dt = 1f / 64f;

    /// <summary>Hero collider height.</summary>
    public const float HeroHeight = 24f;

    /// <summary>Hero centre Y when standing on the floor (floor top 16 + half height 12).</summary>
    public const float GroundY = 28f;

    private readonly PlatformerControllerSystem _controller = new();
    private readonly PhysicsSystem _physics;

    public PlatformerSim(float heroY = GroundY, PlatformerTuning? tuning = null, Action<TileGrid>? customiseLevel = null)
    {
        Grid = new TileGrid(40, 20, 16f, Vector2.Zero);
        for (int x = 0; x < Grid.Width; x++) Grid.SetSolid(x, 0, true);
        customiseLevel?.Invoke(Grid);

        _physics = new PhysicsSystem(Grid);

        World = new World();
        Hero = World.CreateEntity();
        World.Add(Hero, Transform.At(100f, heroY));
        World.Add(Hero, PhysicsBody.Create());
        World.Add(Hero, BoxCollider.Create(new Vector2(16f, HeroHeight)));
        World.Add(Hero, PlatformerController.Create(tuning ?? DefaultTuning()));
        World.Add(Hero, new PlatformerIntent());

        Step(2); // settle: lets contacts register so "grounded" is known
    }

    public World World { get; }

    public TileGrid Grid { get; }

    public Entity Hero { get; }

    public ref Transform Transform => ref World.Get<Transform>(Hero);

    public ref PhysicsBody Body => ref World.Get<PhysicsBody>(Hero);

    public ref PlatformerController Controller => ref World.Get<PlatformerController>(Hero);

    public ref PlatformerIntent Intent => ref World.Get<PlatformerIntent>(Hero);

    /// <summary>Height of the hero's feet above the floor top.</summary>
    public float HeightAboveFloor => Transform.Position.Y - HeroHeight / 2f - 16f;

    /// <summary>Tuning with exact binary timer values (0.125 s coyote and buffer) so step counts are predictable.</summary>
    public static PlatformerTuning DefaultTuning()
    {
        var tuning = PlatformerTuning.Default;
        tuning.CoyoteTime = 0.125f;
        tuning.JumpBufferTime = 0.125f;
        return tuning;
    }

    /// <summary>Runs controller then physics for <paramref name="count"/> fixed steps.</summary>
    public void Step(int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            _controller.Update(World, Dt);
            _physics.Update(World, Dt);
        }
    }

    /// <summary>Simulates pressing the jump button: sets the one-shot pending flag and holds the button.</summary>
    public void PressJump()
    {
        Intent.JumpPressedPending = true;
        Intent.JumpHeld = true;
    }
}
