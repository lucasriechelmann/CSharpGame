using System.Numerics;
using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Input;
using Engine.Rendering;
using Engine.Runtime;
using ImGuiNET;
using Silk.NET.OpenGL;

namespace Platformer;

/// <summary>
/// The sample game: a hero you steer with WASD/arrows among bouncing creatures. Click to spawn more, scroll or Q/E to
/// zoom. It exercises the ECS, fixed loop, input actions, asset handles, sprite sheets, the sprite batch and the camera.
/// </summary>
public sealed class PlatformerGame : Game
{
    private const float ArenaHalfWidth = 560f;
    private const float ArenaHalfHeight = 300f;
    private const float CellSize = 16f;
    private const int SheetColumns = 4;
    private const int SheetRows = 2;
    private const float CreatureSize = 32f;
    private const float HeroSpeed = 220f;

    private readonly Camera2D _camera = new();
    private readonly Random _random = new(1234);
    private Vector3 _clear = new(0.1f, 0.12f, 0.18f);
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
            .BindAxis(GameActions.MoveY, KeyCode.S, KeyCode.W)
            .BindAxis(GameActions.MoveY, KeyCode.Down, KeyCode.Up)
            .BindAxis(GameActions.Zoom, KeyCode.Q, KeyCode.E)
            .Bind(GameActions.Spawn, MouseButton.Left)
            .Bind(GameActions.ResetCamera, KeyCode.R);

        // Per-frame: read input and set facing. Fixed: move things, bounce off the walls.
        FrameSystems.Add(new PlayerInputSystem(Actions));
        FrameSystems.Add(new FaceVelocitySystem());
        FixedSystems.Add(new MovementSystem());
        FixedSystems.Add(new BoundsBounceSystem(ArenaHalfWidth, ArenaHalfHeight));
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

        SpawnArenaBackdrop();
        _hero = SpawnHero();
        SpawnCreatures(150, Vector2.Zero, ArenaHalfWidth, ArenaHalfHeight);
    }

    /// <inheritdoc />
    protected override void Unload() => _batch.Dispose(); // textures belong to Assets, which disposes them

    /// <inheritdoc />
    protected override void Update(double dt)
    {
        UpdateCamera(dt);

        if (Actions.WasPressed(GameActions.Spawn))
            SpawnCreatures(10, _camera.ScreenToWorld(Input.MousePosition), 40f, 40f);
    }

    /// <inheritdoc />
    protected override void Draw(double dt)
    {
        Gl.ClearColor(_clear.X, _clear.Y, _clear.Z, 1f);
        Gl.Clear(ClearBufferMask.ColorBufferBit);
    }

    /// <inheritdoc />
    protected override void DebugGui()
    {
        ImGui.Begin("Sample");
        ImGui.Text("WASD/arrows: move   Q/E or scroll: zoom");
        ImGui.Text("Left click: spawn 10   R: reset camera");
        ImGui.Text($"Sprites: {_batch.Stats.Sprites}   Draw calls: {_batch.Stats.DrawCalls}");

        var mouseWorld = _camera.ScreenToWorld(Input.MousePosition);
        ImGui.Text($"Mouse: {Input.MousePosition.X:F0},{Input.MousePosition.Y:F0}  World: {mouseWorld.X:F0},{mouseWorld.Y:F0}");

        float zoom = _camera.Zoom;
        if (ImGui.SliderFloat("Zoom", ref zoom, 0.1f, 8f)) _camera.Zoom = zoom;

        if (ImGui.Button("Spawn 1000")) SpawnCreatures(1000, Vector2.Zero, ArenaHalfWidth, ArenaHalfHeight);
        ImGui.SameLine();
        if (ImGui.Button("Spawn 10000")) SpawnCreatures(10000, Vector2.Zero, ArenaHalfWidth, ArenaHalfHeight);

        ImGui.ColorEdit3("Clear color", ref _clear);
        ImGui.End();
    }

    private void UpdateCamera(double dt)
    {
        // Follow the hero with a frame-rate independent ease.
        var target = new Vector2(World.Get<Transform>(_hero).Position.X, World.Get<Transform>(_hero).Position.Y);
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

    private void SpawnArenaBackdrop()
    {
        var backdrop = World.CreateEntity();
        World.Add(backdrop, Transform.At(0, 0, -10));

        var sprite = Sprite.Create(_white, new Vector2(ArenaHalfWidth * 2, ArenaHalfHeight * 2));
        sprite.Color = new Color(0.16f, 0.18f, 0.26f);
        World.Add(backdrop, sprite);
    }

    private Entity SpawnHero()
    {
        var hero = World.CreateEntity();
        World.Add(hero, Transform.At(0, 0, 10));
        World.Add(hero, CreatureSprite(cell: 0, scale: 1.5f));
        World.Add(hero, new Velocity());
        World.Add(hero, new PlayerControlled { Speed = HeroSpeed });
        return hero;
    }

    private void SpawnCreatures(int count, Vector2 center, float halfWidth, float halfHeight)
    {
        for (int i = 0; i < count; i++)
        {
            var creature = World.CreateEntity();
            World.Add(creature, Transform.At(
                center.X + Range(-halfWidth, halfWidth),
                center.Y + Range(-halfHeight, halfHeight),
                Range(0, 5)));
            World.Add(creature, CreatureSprite(cell: _random.Next(1, SheetColumns * SheetRows), scale: 1f));
            World.Add(creature, new Velocity { Value = new Vector3(Range(-120, 120), Range(-120, 120), 0) });
            World.Add(creature, new Bouncer());
        }
    }

    /// <summary>Builds a sprite showing one 16x16 cell of the creature sheet (cells run left to right, top to bottom).</summary>
    private Sprite CreatureSprite(int cell, float scale)
    {
        int column = cell % SheetColumns;
        int row = cell / SheetColumns;

        var sprite = Sprite.Create(_sheet, new Vector2(CreatureSize * scale, CreatureSize * scale));
        sprite.Source = new Rect(column * CellSize, row * CellSize, CellSize, CellSize);
        return sprite;
    }

    private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);
}
