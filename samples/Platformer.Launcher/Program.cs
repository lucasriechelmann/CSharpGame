using Engine.Runtime;
using Platformer;

// The shipped game: no editor, assets next to the executable (they flow in from the Platformer game project).
using var game = new PlatformerModule().CreateGame(GameLaunch.Default);
game.Run();
