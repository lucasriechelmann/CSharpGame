using System.Numerics;
using Engine.Animation;
using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Physics2D;
using Engine.Physics3D;
using Engine.Rendering;
using Engine.Rendering3D;
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

    private List<T> All<T>() where T : struct
    {
        var found = new List<T>();
        _world.Query((Entity entity, ref T component) => found.Add(component));
        return found;
    }

    [Fact]
    public void Every_engine_component_is_registered()
    {
        var registry = EngineComponents.RegisterAll(new ComponentRegistry());

        Assert.Equal(
            new[] { "AudioListener", "AudioSource", "BoxCollider", "BoxCollider3D", "CapsuleCollider3D", "CharacterController3D", "DirectionalLight", "Light2D", "MeshRenderer", "ModelAnimator", "ModelRenderer", "MovingPlatform", "Name", "PhysicsBody", "PhysicsBody3D", "PlatformerController", "PlatformerIntent", "PointLight3D", "PreviousTransform", "ShadowCaster2D", "Sprite", "SpriteAnimator", "StaticSolid", "StaticSolid3D", "TileLayerRenderer", "Transform" },
            registry.Names.OrderBy(n => n).ToArray());
    }

    [Fact]
    public void Capsule_and_character_components_round_trip()
    {
        var e = _world.CreateEntity();
        _world.Add(e, new CapsuleCollider3D { Radius = 0.4f, Height = 1.8f, Offset = new Vector3(0f, 0.1f, 0f) });
        var controller = CharacterController3D.Create();
        controller.MoveSpeed = 7f;
        _world.Add(e, controller);
        _world.Add(e, new PhysicsBody3D { GravityScale = 1f, Mass = 70f });
        _world.Add(e, new BoxCollider3D { Size = new Vector3(6f, 0.4f, 4f), Oriented = true });

        var loaded = RoundTrip();

        var capsule = _world.Get<CapsuleCollider3D>(loaded);
        Assert.Equal(0.4f, capsule.Radius);
        Assert.Equal(1.8f, capsule.Height);
        Assert.Equal(0.1f, capsule.Offset.Y);
        Assert.Equal(7f, _world.Get<CharacterController3D>(loaded).MoveSpeed);
        Assert.Equal(70f, _world.Get<PhysicsBody3D>(loaded).Mass);
        Assert.True(_world.Get<BoxCollider3D>(loaded).Oriented);
    }

    [Fact]
    public void Registering_the_engine_components_twice_is_rejected()
    {
        var registry = EngineComponents.RegisterAll(new ComponentRegistry());

        Assert.Throws<ArgumentException>(() => EngineComponents.RegisterAll(registry));
    }

    [Fact]
    public void Light_round_trips()
    {
        var e = _world.CreateEntity();
        _world.Add(e, Light2D.Create(180f, new Color(1f, 0.5f, 0.25f), 1.5f));

        var loaded = RoundTrip();

        var light = _world.Get<Light2D>(loaded);
        Assert.Equal(180f, light.Radius);
        Assert.Equal(1.5f, light.Intensity);
        Assert.Equal(new Color(1f, 0.5f, 0.25f), light.Color);
    }

    [Fact]
    public void Directional_and_point_lights_round_trip()
    {
        var sun = _world.CreateEntity();
        _world.Add(sun, new DirectionalLight { Color = new Color(1f, 0.5f, 0.25f), Intensity = 0.75f });
        var lamp = _world.CreateEntity();
        _world.Add(lamp, PointLight3D.Create(9f, new Color(0.25f, 0.5f, 1f), 2f));

        string json = _scenes.Save();
        _world.Clear();
        _scenes.Load(json);

        var loadedSun = Assert.Single(All<DirectionalLight>());
        Assert.Equal(new Color(1f, 0.5f, 0.25f), loadedSun.Color);
        Assert.Equal(0.75f, loadedSun.Intensity);
        var loadedLamp = Assert.Single(All<PointLight3D>());
        Assert.Equal(9f, loadedLamp.Radius);
        Assert.Equal(2f, loadedLamp.Intensity);
        Assert.Equal(new Color(0.25f, 0.5f, 1f), loadedLamp.Color);
    }

    [Fact]
    public void Mesh_renderer_without_assets_round_trips_with_invalid_handles()
    {
        var e = _world.CreateEntity();
        _world.Add(e, new MeshRenderer());

        var r = _world.Get<MeshRenderer>(RoundTrip());

        Assert.False(r.Mesh.IsValid);
        Assert.False(r.Material.IsValid);
    }

    [Fact]
    public void Empty_previous_transform_round_trips_without_history()
    {
        var e = _world.CreateEntity();
        _world.Add(e, new PreviousTransform());

        var loaded = RoundTrip();

        Assert.False(_world.Get<PreviousTransform>(loaded).HasHistory);
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
