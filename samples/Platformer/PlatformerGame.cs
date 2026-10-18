using System.Numerics;
using Engine.Core.Components;
using Engine.Rendering;
using Engine.Runtime;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;

namespace Platformer;

/// <summary>The sample game: a field of bouncing sprites that exercises the ECS, fixed loop, sprite batch and camera.</summary>
public sealed class PlatformerGame : Game
{
    private const float ArenaHalfWidth = 560f;
    private const float ArenaHalfHeight = 300f;

    private readonly Camera2D _camera = new();
    private readonly Random _random = new(1234);
    private Vector3 _clear = new(0.1f, 0.12f, 0.18f);
    private SpriteBatch _batch = null!;
    private Texture2D _white = null!;
    private Texture2D _checker = null!;

    /// <summary>Registers the fixed-step systems.</summary>
    public PlatformerGame() : base("Platformer")
    {
        FixedSystems.Add(new MovementSystem());
        FixedSystems.Add(new BoundsBounceSystem(ArenaHalfWidth, ArenaHalfHeight));
    }

    /// <inheritdoc />
    protected override void Load()
    {
        _batch = new SpriteBatch(Gl);
        _white = Texture2D.CreateWhite(Gl);
        _checker = TestTextures.CreateChecker(Gl);

        _camera.ViewportSize = new Vector2(Window.Width, Window.Height);
        Window.Resized += (w, h) => _camera.ViewportSize = new Vector2(w, h);
        RenderSystems.Add(new SpriteRenderSystem(_batch, _camera));

        SpawnArenaBackdrop();
        SpawnMovers(300);
    }

    /// <inheritdoc />
    protected override void Unload()
    {
        _checker.Dispose();
        _white.Dispose();
        _batch.Dispose();
    }

    /// <inheritdoc />
    protected override void Update(double dt)
    {
        var keyboard = Window.Input.Keyboards[0];
        var pan = Vector2.Zero;
        if (keyboard.IsKeyPressed(Key.W)) pan.Y += 1;
        if (keyboard.IsKeyPressed(Key.S)) pan.Y -= 1;
        if (keyboard.IsKeyPressed(Key.D)) pan.X += 1;
        if (keyboard.IsKeyPressed(Key.A)) pan.X -= 1;
        _camera.Position += pan * (float)(400.0 / _camera.Zoom * dt);

        if (keyboard.IsKeyPressed(Key.E)) _camera.Zoom *= 1f + (float)dt;
        if (keyboard.IsKeyPressed(Key.Q)) _camera.Zoom /= 1f + (float)dt;
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
        ImGui.Text("WASD: pan   Q/E: zoom");
        ImGui.Text($"Sprites: {_batch.Stats.Sprites}   Draw calls: {_batch.Stats.DrawCalls}");

        var position = _camera.Position;
        if (ImGui.DragFloat2("Camera", ref position)) _camera.Position = position;

        float zoom = _camera.Zoom;
        if (ImGui.SliderFloat("Zoom", ref zoom, 0.1f, 8f)) _camera.Zoom = zoom;

        if (ImGui.Button("Spawn 1000")) SpawnMovers(1000);
        ImGui.SameLine();
        if (ImGui.Button("Spawn 10000")) SpawnMovers(10000);

        ImGui.ColorEdit3("Clear color", ref _clear);
        ImGui.End();
    }

    private void SpawnArenaBackdrop()
    {
        var backdrop = World.CreateEntity();
        var transform = Transform.At(0, 0, -10);
        World.Add(backdrop, transform);

        var sprite = Sprite.Create(_white, new Vector2(ArenaHalfWidth * 2, ArenaHalfHeight * 2));
        sprite.Color = new Color(0.16f, 0.18f, 0.26f);
        World.Add(backdrop, sprite);
    }

    private void SpawnMovers(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var entity = World.CreateEntity();

            var transform = Transform.At(
                Range(-ArenaHalfWidth, ArenaHalfWidth),
                Range(-ArenaHalfHeight, ArenaHalfHeight),
                Range(0, 5));
            transform.SetRotationZ(Range(0, MathF.Tau));
            World.Add(entity, transform);

            var sprite = Sprite.Create(_checker, new Vector2(24, 24));
            sprite.Color = new Color(Range(0.4f, 1f), Range(0.4f, 1f), Range(0.4f, 1f));
            World.Add(entity, sprite);

            World.Add(entity, new Velocity { Value = new Vector3(Range(-120, 120), Range(-120, 120), 0) });
        }
    }

    private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);
}
