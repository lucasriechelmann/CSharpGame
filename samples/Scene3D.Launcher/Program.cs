using Engine.Runtime;
using Scene3D;

// The shipped game: no editor, assets next to the executable (they flow in from the Scene3D game project).
using var game = new Scene3DModule().CreateGame(GameLaunch.Default);
game.Run();
