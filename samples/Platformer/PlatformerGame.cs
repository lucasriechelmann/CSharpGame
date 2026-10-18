using System.Numerics;
using Engine.Core.Ecs;
using Engine.Runtime;
using ImGuiNET;
using Silk.NET.OpenGL;

namespace Platformer;

/// <summary>The sample game: for now a clear colour plus a moving probe entity to prove the ECS and fixed loop work.</summary>
public sealed class PlatformerGame : Game
{
    private Vector3 _clear = new(0.1f, 0.12f, 0.18f);
    private readonly Entity _probe;

    /// <summary>Registers systems and spawns the probe entity.</summary>
    public PlatformerGame() : base("Platformer")
    {
        FixedSystems.Add(new MovementSystem());

        _probe = World.CreateEntity();
        World.Add(_probe, new Position());
        World.Add(_probe, new Velocity { Value = new Vector3(1, 0.5f, 0) });
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
        ImGui.Text($"Probe position: {World.Get<Position>(_probe).Value}");
        if (ImGui.Button("Spawn 100 entities"))
            SpawnMovers(100);

        ImGui.ColorEdit3("Clear color", ref _clear);
        ImGui.End();
    }

    private void SpawnMovers(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var e = World.CreateEntity();
            World.Add(e, new Position());
            World.Add(e, new Velocity { Value = new Vector3(i, 0, 0) });
        }
    }
}
