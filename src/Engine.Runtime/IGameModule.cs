namespace Engine.Runtime;

/// <summary>
/// The single entry point a game library exposes, so a host (the shipped launcher, or the editor) can start any game without knowing
/// its types: it finds the library's one implementation, calls <see cref="CreateGame"/>, and runs what comes back. The returned
/// <see cref="Game"/> stays the composition root (it registers its own components, systems and loaders); the module only adds
/// "make one, with these folders".
/// </summary>
public interface IGameModule
{
    /// <summary>A short name for window titles and logs.</summary>
    string Name { get; }

    /// <summary>Creates the game (and its window, which opens on <see cref="Game.Run"/>).</summary>
    /// <param name="launch">Where the game finds its assets and writes its files.</param>
    Game CreateGame(GameLaunch launch);
}
