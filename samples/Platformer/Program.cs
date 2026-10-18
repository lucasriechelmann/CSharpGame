using Engine.Runtime;
using ImGuiNET;
using Silk.NET.OpenGL;

using var game = new PlatformerGame();
game.Run();

sealed class PlatformerGame() : Game("Platformer")
{
    private System.Numerics.Vector3 _clear = new(0.1f, 0.12f, 0.18f);
    private int _ticks;

    protected override void FixedUpdate(double dt) => _ticks++;

    protected override void Draw(double dt)
    {
        Gl.ClearColor(_clear.X, _clear.Y, _clear.Z, 1f);
        Gl.Clear(ClearBufferMask.ColorBufferBit);
    }

    protected override void DebugGui()
    {
        ImGui.Begin("Sample");
        ImGui.Text($"Game ticks: {_ticks}");
        ImGui.ColorEdit3("Clear color", ref _clear);
        ImGui.End();
    }
}
