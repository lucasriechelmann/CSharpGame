using Engine.Runtime;

namespace Scene3D;

/// <summary>The 3D sample's entry point for hosts: the shipped launcher and the editor both start the game through this.</summary>
public sealed class Scene3DModule : IGameModule
{
    /// <inheritdoc />
    public string Name => "Scene3D";

    /// <inheritdoc />
    public Game CreateGame(GameLaunch launch) => new Scene3DGame(launch);
}
