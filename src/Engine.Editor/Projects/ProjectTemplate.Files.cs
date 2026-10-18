namespace Engine.Editor.Projects;

// The text files of a new project. __NAME__ and __ENGINEDIR__ are replaced when the project is created.
public static partial class ProjectTemplate
{
    private static readonly (string Path, string Content)[] Files =
    [
        ("Directory.Build.props", """
            <Project>

              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <LangVersion>latest</LangVersion>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <!-- Where the engine assemblies are. The editor passes this when it builds; the value below is for building from the command line or an IDE. -->
                <EngineDir Condition="'$(EngineDir)' == ''">__ENGINEDIR__</EngineDir>
              </PropertyGroup>

              <ItemGroup>
                <EngineAssembly Include="$(EngineDir)Engine.*.dll;$(EngineDir)Silk.NET.*.dll;$(EngineDir)ImGui.NET.dll;$(EngineDir)StbImageSharp.dll" />
              </ItemGroup>

            </Project>
            """),

        (".gitignore", """
            bin/
            obj/
            Build/
            .local/
            imgui.ini
            """),

        ("README.md", """
            # __NAME__

            A game made with the engine's editor.

            - `__NAME__.csgame.json`: the project file. Open it from the editor's hub (File > Open project).
            - `Game/`: your game code (a library). `__NAME__Game.cs` is the game; add components and systems next to it.
            - `Launcher/`: the small program that runs the game without the editor. This is what you ship.
            - `Assets/`: scenes, sprites, sounds, prefabs. Everything is loaded by its path relative to this folder.

            Build from the editor (Build menu) or with `dotnet build Game/__NAME__.Game.csproj`.
            Publish a self-contained copy with the editor's Build > Publish, or `dotnet publish Launcher/__NAME__.csproj -c Release -r win-x64 --self-contained`.
            """),

        ("Game/__NAME__.Game.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <OutputType>Library</OutputType>
                <AssemblyName>__NAME__.Game</AssemblyName>
                <RootNamespace>__NAME__</RootNamespace>
              </PropertyGroup>

              <ItemGroup>
                <!-- Compile against the engine; the editor and the launcher provide the assemblies at run time. -->
                <Reference Include="@(EngineAssembly)" Private="false" />
              </ItemGroup>

              <!-- The assets travel with the game library, so the launcher ships them. -->
              <ItemGroup>
                <Content Include="..\Assets\**\*" Link="Assets\%(RecursiveDir)%(Filename)%(Extension)" CopyToOutputDirectory="PreserveNewest" />
              </ItemGroup>

            </Project>
            """),

        ("Game/__NAME__Game.cs", """
            using System.Numerics;
            using Engine.Rendering;
            using Engine.Runtime;
            using Silk.NET.OpenGL;

            namespace __NAME__;

            /// <summary>
            /// The game. The engine calls <see cref="Load"/> once, then <see cref="Update"/> every frame and <see cref="Draw"/> to prepare the frame; sprites
            /// in the scene are drawn by the render systems added in <see cref="Load"/>. The editor shows this same game inside its Scene panel.
            /// </summary>
            public sealed class __NAME__Game : Game
            {
                private const string MainScene = "scenes/main.scene.json";

                private readonly Camera2D _camera = new();
                private SpriteBatch _batch = null!;

                /// <summary>Creates the game; <paramref name="launch"/> says where its assets are (the launcher and the editor each pass their own).</summary>
                public __NAME__Game(GameLaunch? launch = null) : base("__NAME__", launch: launch)
                {
                    // Register your own components here so scenes and prefabs can use them:
                    // Components.Register<MyComponent>("MyComponent");
                }

                /// <inheritdoc />
                protected override Camera2D? SceneCamera => _camera;

                /// <inheritdoc />
                protected override string? ScenePath => Path.Combine(Launch.AssetRoot, MainScene);

                /// <inheritdoc />
                protected override void Load()
                {
                    _batch = new SpriteBatch(Gl);
                    Scenes.LoadFile(MainScene);

                    // The camera follows the size of the area the game is drawn into (the window, or the editor's Scene panel).
                    _camera.ViewportSize = new Vector2(ViewportWidth, ViewportHeight);
                    ViewportResized += (width, height) => _camera.ViewportSize = new Vector2(width, height);

                    RenderSystems.Add(new SpriteRenderSystem(_batch, _camera, Assets, RenderClock));
                }

                /// <inheritdoc />
                protected override void Unload() => _batch.Dispose(); // textures belong to Assets, which disposes them

                /// <inheritdoc />
                protected override void Update(double dt)
                {
                    if (IsEditing) return; // the editor has the game frozen; do not run game logic then

                    // Your per-frame game logic goes here.
                }

                /// <inheritdoc />
                protected override void Draw(double dt)
                {
                    _batch.ResetStats();
                    Gl.ClearColor(0.07f, 0.08f, 0.12f, 1f);
                    Gl.Clear(ClearBufferMask.ColorBufferBit);
                }
            }
            """),

        ("Game/__NAME__Module.cs", """
            using Engine.Runtime;

            namespace __NAME__;

            /// <summary>The entry point the editor and the launcher use to start this game.</summary>
            public sealed class __NAME__Module : IGameModule
            {
                /// <inheritdoc />
                public string Name => "__NAME__";

                /// <inheritdoc />
                public Game CreateGame(GameLaunch launch) => new __NAME__Game(launch);
            }
            """),

        ("Launcher/__NAME__.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <AssemblyName>__NAME__</AssemblyName>
                <RootNamespace>__NAME__.Launcher</RootNamespace>
              </PropertyGroup>

              <ItemGroup>
                <ProjectReference Include="..\Game\__NAME__.Game.csproj" />
                <!-- The engine assemblies are copied next to the program. -->
                <Reference Include="@(EngineAssembly)" />
              </ItemGroup>

              <!-- Native libraries (window, ImGui, audio) and two small helpers live beside the engine assemblies. -->
              <ItemGroup>
                <None Include="$(EngineDir)runtimes\**\*" Link="runtimes\%(RecursiveDir)%(Filename)%(Extension)" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />
                <None Include="$(EngineDir)Microsoft.DotNet.PlatformAbstractions.dll;$(EngineDir)Microsoft.Extensions.DependencyModel.dll" Link="%(Filename)%(Extension)" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />
              </ItemGroup>

            </Project>
            """),

        ("Launcher/Program.cs", """
            using Engine.Runtime;
            using __NAME__;

            // The shipped game: no editor, assets next to the program.
            using var game = new __NAME__Module().CreateGame(GameLaunch.Default);
            game.Run();
            """),

        ("Assets/scenes/main.scene.json", """
            {
              "version": 1,
              "entities": [
                {
                  "components": {
                    "Name": { "value": "Ground" },
                    "Transform": {
                      "position": { "x": 0, "y": -140, "z": 0 },
                      "rotation": { "x": 0, "y": 0, "z": 0, "w": 1 },
                      "scale": { "x": 1, "y": 1, "z": 1 }
                    },
                    "Sprite": {
                      "texture": "sprites/square.png",
                      "size": { "x": 520, "y": 40 },
                      "source": { "x": 0, "y": 0, "width": 32, "height": 32 },
                      "color": { "r": 0.35, "g": 0.55, "b": 0.3, "a": 1 },
                      "pivot": { "x": 0.5, "y": 0.5 },
                      "flip": "None"
                    }
                  }
                },
                {
                  "components": {
                    "Name": { "value": "Player" },
                    "Transform": {
                      "position": { "x": 0, "y": -60, "z": 1 },
                      "rotation": { "x": 0, "y": 0, "z": 0, "w": 1 },
                      "scale": { "x": 1, "y": 1, "z": 1 }
                    },
                    "Sprite": {
                      "texture": "sprites/square.png",
                      "size": { "x": 48, "y": 48 },
                      "source": { "x": 0, "y": 0, "width": 32, "height": 32 },
                      "color": { "r": 0.38, "g": 0.8, "b": 1, "a": 1 },
                      "pivot": { "x": 0.5, "y": 0.5 },
                      "flip": "None"
                    }
                  }
                }
              ]
            }
            """),
    ];
}
