using System.Numerics;
using Engine.Animation;
using Engine.Audio;
using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Input;
using Engine.Physics2D;
using Engine.Rendering;
using Engine.Runtime;
using Engine.Tilemaps;
using Silk.NET.OpenGL;

namespace Platformer;

/// <summary>
/// The sample game: an animated hero you steer through a tilemap level with run, jump (coyote time, jump buffering, variable
/// height) and gravity, plus patrolling creatures. The level, hero, creatures, tiles and animations are all data files
/// loaded through the scene system; F5 and F9 quick-save and quick-load the whole world.
/// </summary>
/// <remarks>
/// Split by topic: this file (setup, loop hooks), <c>PlatformerGame.Scene.cs</c> (loading, saving, spawning) and
/// <c>PlatformerGame.Debug.cs</c>.
/// </remarks>
public sealed partial class PlatformerGame : Game
{
    private readonly Camera2D _camera = new();
    private readonly Random _random = new(1234);
    private readonly PhysicsSettings _physicsSettings = new();
    private Vector3 _clear = new(0.07f, 0.08f, 0.12f);
    private SpriteBatch _batch = null!;
    private TileGrid _grid = null!;
    private AudioClip _spawnSound = null!;
    private Entity _hero;

    /// <summary>Registers input bindings and the game's own components for scene files. Systems are added in <see cref="Load"/>.</summary>
    public PlatformerGame() : base("Platformer")
    {
        Actions
            .BindAxis(GameActions.MoveX, KeyCode.A, KeyCode.D)
            .BindAxis(GameActions.MoveX, KeyCode.Left, KeyCode.Right)
            .Bind(GameActions.Jump, KeyCode.Space)
            .Bind(GameActions.Jump, KeyCode.W)
            .Bind(GameActions.Jump, KeyCode.Up)
            .BindAxis(GameActions.Zoom, KeyCode.Q, KeyCode.E)
            .Bind(GameActions.Spawn, MouseButton.Left)
            .Bind(GameActions.ResetCamera, KeyCode.R)
            .Bind(GameActions.QuickSave, KeyCode.F5)
            .Bind(GameActions.QuickLoad, KeyCode.F9);

        // The engine's components are registered already; scenes need the game's own too.
        Components.Register<Patrol>("Patrol");
    }

    /// <inheritdoc />
    protected override void Load()
    {
        _batch = new SpriteBatch(Gl);

        Scenes.LoadFile(LevelScene);
        _hero = FindHero();
        _grid = BuildCollisionGrid();
        _creatureMachines = Enumerable.Range(0, CreatureTypeCount)
            .Select(type => Assets.Load<AnimationStateMachine>($"anim/creature{type}.anim.json"))
            .ToArray();

        _spawnSound = Assets.Get(Assets.Load<AudioClip>("sounds/spawn.wav"));
        Audio.Preload(_spawnSound);

        _camera.ViewportSize = new Vector2(Window.Width, Window.Height);
        Window.Resized += (w, h) => _camera.ViewportSize = new Vector2(w, h);
        SnapCameraToHero();

        // Per frame: input -> animation parameters -> animation, then facing.
        FrameSystems.Add(new PlayerInputSystem(Actions));
        FrameSystems.Add(new HeroAnimationSystem(Assets.Get(Assets.Load<AnimationStateMachine>(HeroAnimation))));
        FrameSystems.Add(new AnimationSystem(Assets));
        FrameSystems.Add(new FaceVelocitySystem());
        FrameSystems.Add(new HeroSoundSystem(
            Audio,
            Assets.Get(Assets.Load<AudioClip>("sounds/jump.wav")),
            Assets.Get(Assets.Load<AudioClip>("sounds/land.wav"))));

        // Fixed step: decide velocities (patrol, controller) first, then let physics move everything.
        FixedSystems.Add(new PatrolSystem());
        FixedSystems.Add(new PlatformerControllerSystem());
        FixedSystems.Add(new PhysicsSystem(_grid, _physicsSettings));

        // Render passes: background and ground tiles, then sprites, then foreground tiles in front.
        RenderSystems.Add(new TileLayerRenderSystem(_batch, _camera, Assets, foreground: false));
        RenderSystems.Add(new SpriteRenderSystem(_batch, _camera, Assets));
        RenderSystems.Add(new TileLayerRenderSystem(_batch, _camera, Assets, foreground: true));
    }

    /// <inheritdoc />
    protected override void Unload() => _batch.Dispose(); // textures belong to Assets, which disposes them

    /// <inheritdoc />
    protected override void Update(double dt)
    {
        UpdateCamera(dt);
        UpdateStatusMessage(dt);

        if (Actions.WasPressed(GameActions.QuickSave)) QuickSave();
        else if (Actions.WasPressed(GameActions.QuickLoad)) QuickLoad();

        if (Actions.WasPressed(GameActions.Spawn))
            SpawnCreaturesAt(_camera.ScreenToWorld(Input.MousePosition), 5);
    }

    /// <inheritdoc />
    protected override void Draw(double dt)
    {
        _batch.ResetStats(); // render systems add to the totals; the debug UI reads them after all passes
        Gl.ClearColor(_clear.X, _clear.Y, _clear.Z, 1f);
        Gl.Clear(ClearBufferMask.ColorBufferBit);
    }

    private void UpdateCamera(double dt)
    {
        // Follow the hero with a frame-rate independent ease.
        var heroPosition = World.Get<Transform>(_hero).Position;
        var target = new Vector2(heroPosition.X, heroPosition.Y);
        float follow = 1f - MathF.Exp(-6f * (float)dt);
        _camera.Position = Vector2.Lerp(_camera.Position, target, follow);

        float zoom = _camera.Zoom * (1f + Actions.GetAxis(GameActions.Zoom) * (float)dt);
        zoom *= MathF.Pow(1.1f, Input.ScrollDelta);
        _camera.Zoom = Math.Clamp(zoom, 0.1f, 8f);

        if (Actions.WasPressed(GameActions.ResetCamera))
        {
            _camera.Zoom = 1f;
            _camera.Position = target;
        }
    }

    private void SnapCameraToHero()
    {
        var position = World.Get<Transform>(_hero).Position;
        _camera.Position = new Vector2(position.X, position.Y);
    }
}
