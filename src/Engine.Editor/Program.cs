using Engine.Editor;
using Engine.Editor.Theme;
using Engine.Runtime;

// The editor program: opens a game library, shows its scene and lets you edit and play it.
//   Editor --game <path to the game library .dll> --project <folder that contains the game's Assets folder>
string? gamePath = null;
string? projectPath = null;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--game" && i + 1 < args.Length) gamePath = args[++i];
    else if (args[i] == "--project" && i + 1 < args.Length) projectPath = args[++i];
    else
    {
        Console.Error.WriteLine($"Unknown argument '{args[i]}'.");
        return Usage();
    }
}

if (gamePath == null || projectPath == null) return Usage();

try
{
    var module = GameLibrary.Load(gamePath);
    using var game = module.CreateGame(GameLaunch.ForProject(projectPath));
    game.ImGuiSetup = Win11Theme.Configure; // Windows 11 look: Segoe UI, Fluent colours, rounded controls
    game.DarkTitleBar = true;
    game.AttachEditor(context =>
    {
        var editor = new SceneEditor(context.World, context.Assets, context.Inspector, context.Log, context.Hooks, context.Game) { Pinned = true };
        editor.Toggle(); // the standalone editor is always on
        return editor;
    });
    game.Run();
    return 0;
}
catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or InvalidOperationException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static int Usage()
{
    Console.Error.WriteLine("Usage: Editor --game <game library .dll> --project <folder containing Assets>");
    Console.Error.WriteLine("Example: Editor --game samples/Platformer/bin/Debug/net10.0/Platformer.Game.dll --project samples/Platformer");
    return 2;
}
