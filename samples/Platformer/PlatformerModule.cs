using Engine.Runtime;

namespace Platformer;

/// <summary>The Platformer's entry point for hosts: the shipped launcher and the editor both start the game through this.</summary>
public sealed class PlatformerModule : IGameModule
{
    /// <inheritdoc />
    public string Name => "Platformer";

    /// <inheritdoc />
    public Game CreateGame(GameLaunch launch) => new PlatformerGame(launch);
}
