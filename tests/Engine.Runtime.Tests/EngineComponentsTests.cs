using System.Numerics;
using Engine.Animation;
using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Physics2D;
using Engine.Rendering;
using Engine.Runtime;
using Engine.Scenes;
using Engine.Tilemaps;

namespace Engine.Runtime.Tests;

/// <summary>Checks that every engine component is registered and survives a JSON save and load unchanged.</summary>
public sealed class EngineComponentsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "csharpgame-runtime-" + Guid.NewGuid().ToString("N"));
    private readonly World _world = new();
    private readonly AssetManager _assets;
    private readonly SceneSerializer _scenes;

    public EngineComponentsTests()
    {
        Directory.CreateDirectory(_root);
        _assets = new AssetManager(_root);
        _scenes = new SceneSerializer(_world, _assets, EngineComponents.RegisterAll(new ComponentRegistry()));
    }

    public void Dispose()
    {
        _assets.Dispose();
        Directory.Delete(_root, recursive: true);
    }

    /// <summary>Saves the world, clears it, loads it back, and returns the single entity.</summary>
    private Entity RoundTrip()
    {
        string json = _scenes.Save();
        _world.Clear();
        return Assert.Single(_scenes.Load(json));
    }

    [Fact]
    public void Every_engine_component_is_registered()
    {
        var registry = EngineComponents.RegisterAll(new ComponentRegistry());

        Assert.Equal(
            new[] { "BoxCollider", "Name", "PhysicsBody", "PlatformerController", "PlatformerIntent", "Sprite", "SpriteAnimator", "StaticSolid", "TileLayerRenderer", "Transform" },
            registry.Names.OrderBy(n => n).ToArray());
    }

    [Fact]
    public void Registering_the_engine_components_twice_is_rejected()
    {
        var registry = EngineComponents.RegisterAll(new ComponentRegistry());

        Assert.Throws<ArgumentException>(() => EngineComponents.RegisterAll(registry));
    }

    [Fact]
    public void Transform_and_name_round_trip()
    {
        var e = _world.CreateEntity();
        var transform = Transform.At(10f, -20f, 5f);
        transform.Scale = new Vector3(2f, 3f, 1f);
        transform.SetRotationZ(1.25f);
        _world.Add(e, transform);
        _world.Add(e, Name.Of("Hero"));

        var loaded = RoundTrip();

        var t = _world.Get<Transform>(loaded);
        Assert.Equal(new Vector3(10f, -20f, 5f), t.Position);
        Assert.Equal(new Vector3(2f, 3f, 1f), t.Scale);
        Assert.Equal(1.25f, t.RotationZ, 4);
        Assert.Equal("Hero", _world.Get<Name>(loaded).Value);
    }

    [Fact]
    public void Sprite_round_trips_all_its_visual_settings()
    {
        var e = _world.CreateEntity();
        _world.Add(e, new Sprite
        {
            Size = new Vector2(40f, 24f),
            Source = new Rect(16f, 32f, 16f, 16f),
            Color = new Color(0.25f, 0.5f, 0.75f, 0.5f),
            Pivot = new Vector2(0.5f, 1f),
            Flip = SpriteFlip.Horizontal | SpriteFlip.Vertical,
        });

        var s = _world.Get<Sprite>(RoundTrip());

        Assert.Equal(new Vector2(40f, 24f), s.Size);
        Assert.Equal(new Rect(16f, 32f, 16f, 16f), s.Source);
        Assert.Equal(new Color(0.25f, 0.5f, 0.75f, 0.5f), s.Color);
        Assert.Equal(new Vector2(0.5f, 1f), s.Pivot);
        Assert.Equal(SpriteFlip.Horizontal | SpriteFlip.Vertical, s.Flip);
        Assert.False(s.Texture.IsValid);
    }

    [Fact]
    public void Physics_components_round_trip()
    {
        var e = _world.CreateEntity();
        _world.Add(e, new PhysicsBody { Velocity = new Vector2(3f, -4f), GravityScale = 1.5f, Contacts = CollisionFlags.Below | CollisionFlags.Left });
        _world.Add(e, new BoxCollider { Size = new Vector2(24f, 30f), Offset = new Vector2(0f, -2.5f) });
        _world.Add(e, new StaticSolid());

        var loaded = RoundTrip();

        var body = _world.Get<PhysicsBody>(loaded);
        Assert.Equal(new Vector2(3f, -4f), body.Velocity);
        Assert.Equal(1.5f, body.GravityScale);
        Assert.Equal(CollisionFlags.Below | CollisionFlags.Left, body.Contacts);
        Assert.Equal(new Vector2(24f, 30f), _world.Get<BoxCollider>(loaded).Size);
        Assert.Equal(new Vector2(0f, -2.5f), _world.Get<BoxCollider>(loaded).Offset);
        Assert.True(_world.Has<StaticSolid>(loaded)); // an empty tag component still round-trips
    }

    [Fact]
    public void Platformer_controller_and_intent_round_trip()
    {
        var e = _world.CreateEntity();
        var controller = PlatformerController.Create(PlatformerTuning.Default);
        controller.Tuning.JumpSpeed = 777f;
        controller.CoyoteTimer = 0.05f;
        controller.JumpBufferTimer = 0.07f;
        controller.IsJumping = true;
        _world.Add(e, controller);
        _world.Add(e, new PlatformerIntent { MoveX = -1f, JumpHeld = true, JumpPressedPending = true });

        var loaded = RoundTrip();

        var c = _world.Get<PlatformerController>(loaded);
        Assert.Equal(777f, c.Tuning.JumpSpeed);
        Assert.Equal(PlatformerTuning.Default.MoveSpeed, c.Tuning.MoveSpeed);
        Assert.Equal(0.05f, c.CoyoteTimer);
        Assert.Equal(0.07f, c.JumpBufferTimer);
        Assert.True(c.IsJumping);
        var i = _world.Get<PlatformerIntent>(loaded);
        Assert.Equal(-1f, i.MoveX);
        Assert.True(i.JumpHeld);
        Assert.True(i.JumpPressedPending);
    }

    [Fact]
    public void Animator_round_trips_its_state_and_parameters()
    {
        var e = _world.CreateEntity();
        _world.Add(e, new SpriteAnimator
        {
            CurrentState = 2,
            StateTime = 0.5f,
            Speed = 1.5f,
            Finished = true,
            Parameters = [1f, 2f, 3f],
        });

        var a = _world.Get<SpriteAnimator>(RoundTrip());

        Assert.Equal(2, a.CurrentState);
        Assert.Equal(0.5f, a.StateTime);
        Assert.Equal(1.5f, a.Speed);
        Assert.True(a.Finished);
        Assert.Equal(new[] { 1f, 2f, 3f }, a.Parameters);
    }

    [Fact]
    public void Tile_layer_renderer_round_trips()
    {
        var e = _world.CreateEntity();
        _world.Add(e, new TileLayerRenderer { Tint = new Color(1f, 0.5f, 0.25f, 1f), Foreground = true });

        var r = _world.Get<TileLayerRenderer>(RoundTrip());

        Assert.Equal(new Color(1f, 0.5f, 0.25f, 1f), r.Tint);
        Assert.True(r.Foreground);
    }

    [Fact]
    public void A_fully_loaded_entity_round_trips_every_component_together()
    {
        var e = _world.CreateEntity();
        _world.Add(e, Name.Of("Everything"));
        _world.Add(e, Transform.At(1f, 2f, 3f));
        _world.Add(e, new Sprite { Size = Vector2.One, Color = Color.White });
        _world.Add(e, PhysicsBody.Create());
        _world.Add(e, new BoxCollider { Size = Vector2.One });
        _world.Add(e, PlatformerController.Create(PlatformerTuning.Default));
        _world.Add(e, new PlatformerIntent());

        var loaded = RoundTrip();

        Assert.True(_world.Has<Name>(loaded));
        Assert.True(_world.Has<Transform>(loaded));
        Assert.True(_world.Has<Sprite>(loaded));
        Assert.True(_world.Has<PhysicsBody>(loaded));
        Assert.True(_world.Has<BoxCollider>(loaded));
        Assert.True(_world.Has<PlatformerController>(loaded));
        Assert.True(_world.Has<PlatformerIntent>(loaded));
    }
}
