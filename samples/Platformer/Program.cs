using System.Numerics;
using Engine.Core.Ecs;
using Engine.Runtime;
using ImGuiNET;
using Silk.NET.OpenGL;

using var game = new PlatformerGame();
game.Run();

// Components are plain data. Z stays in Vector3 so the same types serve a future 3D renderer.
struct Position { public Vector3 Value; }
struct Velocity { public Vector3 Value; }

sealed class MovementSystem : ISystem
{
    public void Update(World world, double dt) =>
        world.Query<Position, Velocity>((Entity _, ref Position p, ref Velocity v) =>
            p.Value += v.Value * (float)dt);
}

sealed class PlatformerGame : Game
{
    private Vector3 _clear = new(0.1f, 0.12f, 0.18f);
    private Entity _probe;

    public PlatformerGame() : base("Platformer")
    {
        FixedSystems.Add(new MovementSystem());

        _probe = World.CreateEntity();
        World.Add(_probe, new Position());
        World.Add(_probe, new Velocity { Value = new Vector3(1, 0.5f, 0) });
    }

    protected override void Draw(double dt)
    {
        Gl.ClearColor(_clear.X, _clear.Y, _clear.Z, 1f);
        Gl.Clear(ClearBufferMask.ColorBufferBit);
    }

    protected override void DebugGui()
    {
        ImGui.Begin("Sample");
        ImGui.Text($"Probe position: {World.Get<Position>(_probe).Value}");
        if (ImGui.Button("Spawn 100 entities"))
        {
            for (int i = 0; i < 100; i++)
            {
                var e = World.CreateEntity();
                World.Add(e, new Position());
                World.Add(e, new Velocity { Value = new Vector3(i, 0, 0) });
            }
        }

        ImGui.ColorEdit3("Clear color", ref _clear);
        ImGui.End();
    }
}
