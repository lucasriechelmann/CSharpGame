using Engine.Runtime;

namespace Arena3D;

/// <summary>The entry point the editor and the launcher use to start this game.</summary>
public sealed class Arena3DModule : IGameModule
{
    /// <inheritdoc />
    public string Name => "Arena3D";

    /// <inheritdoc />
    public Game CreateGame(GameLaunch launch) => new Arena3DGame(launch);
}
