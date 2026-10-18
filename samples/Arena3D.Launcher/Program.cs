using Arena3D;
using Engine.Runtime;

// The shipped game: no editor, assets next to the executable (they flow in from the Arena3D game project).
using var game = new Arena3DModule().CreateGame(GameLaunch.Default);
game.Run();
