using System.Numerics;
using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Input;
using Engine.Physics2D;
using Engine.Rendering;
using Engine.Runtime;
using Silk.NET.OpenGL;

namespace Platformer;

/// <summary>
/// The sample game: a hero you steer through a tile level with run, jump (coyote time, jump buffering, variable height)
/// and gravity, plus patrolling creatures. Click to spawn more, scroll or Q/E to zoom. It exercises the ECS, fixed loop,
/// input actions, asset handles, sprite sheets, tile and box collision, and the camera.
/// </summary>
/// <remarks>
/// Split by topic: this file (setup, loop hooks), <c>PlatformerGame.Spawning.cs</c> and <c>PlatformerGame.Debug.cs</c>.
/// </remarks>
public sealed partial class PlatformerGame : Game
{
    private const float CellSize = 16f;
    private const int SheetColumns = 4;
    private const int SheetRows = 2;

    private readonly Camera2D _camera = new();
    private readonly Random _random = new(1234);
    private readonly TileGrid _grid = LevelLayout.CreateGrid();
    private readonly PhysicsSettings _physicsSettings = new();
    private Vector3 _clear = new(0.07f, 0.08f, 0.12f);
    private SpriteBatch _batch = null!;
    private Entity _hero;
    private AssetHandle<Texture2D> _white;
    private AssetHandle<Texture2D> _sheet;

    /// <summary>Registers input bindings and systems.</summary>
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
            .Bind(GameActions.ResetCamera, KeyCode.R);

        // Per frame: read input into the hero's intent, face the way we move.
        FrameSystems.Add(new PlayerInputSystem(Actions));
        FrameSystems.Add(new FaceVelocitySystem());

        // Fixed step: decide velocities (patrol, controller) first, then let physics move everything.
        FixedSystems.Add(new PatrolSystem());
        FixedSystems.Add(new PlatformerControllerSystem());
        FixedSystems.Add(new PhysicsSystem(_grid, _physicsSettings));
    }

    /// <inheritdoc />
    protected override void Load()
    {
        _batch = new SpriteBatch(Gl);
        _white = Assets.Add("builtin/white", Texture2D.CreateWhite(Gl));
        _sheet = Assets.Load<Texture2D>("creatures.png");

        _camera.ViewportSize = new Vector2(Window.Width, Window.Height);
        Window.Resized += (w, h) => _camera.ViewportSize = new Vector2(w, h);
        RenderSystems.Add(new SpriteRenderSystem(_batch, _camera, Assets));

        SpawnBackdrop();
        SpawnTiles();
        _hero = SpawnHero(column: 3, row: 1);
        SpawnInitialCreatures();
    }

    /// <inheritdoc />
    protected override void Unload() => _batch.Dispose(); // textures belong to Assets, which disposes them

    /// <inheritdoc />
    protected override void Update(double dt)
    {
        UpdateCamera(dt);

        if (Actions.WasPressed(GameActions.Spawn))
            SpawnCreaturesAt(_camera.ScreenToWorld(Input.MousePosition), 5);
    }

    /// <inheritdoc />
    protected override void Draw(double dt)
    {
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
}
