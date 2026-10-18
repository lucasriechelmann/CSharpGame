using Engine.Runtime;
using Silk.NET.OpenGL;

namespace Engine.Editor;

/// <summary>
/// The "game" behind the project hub: it has nothing to simulate or draw, it only gives the hub a window, an ImGui context and a loop to run in.
/// The hub's interface is its editor shell (<see cref="HubShell"/>).
/// </summary>
internal sealed class HubGame() : Game("Game Editor", 1000, 640)
{
    /// <inheritdoc />
    protected override void Draw(double dt)
    {
        Gl.ClearColor(0.125f, 0.125f, 0.125f, 1f);
        Gl.Clear(ClearBufferMask.ColorBufferBit);
    }
}
