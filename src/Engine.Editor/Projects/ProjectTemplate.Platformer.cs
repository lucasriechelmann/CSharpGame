using System.Text;

namespace Engine.Editor.Projects;

// The 2D platformer starter: files that replace or add to the empty project's. Level, tile set and atlas are made here so the project needs no
// binary files in the engine's source.
public static partial class ProjectTemplate
{
    private const int LevelColumns = 30;
    private const int LevelRows = 12;
    private const int LevelTileSize = 32;

    private static readonly (string Path, string Content)[] PlatformerFiles =
    [
        ("Game/__NAME__Game.cs", """
            using System.Numerics;
            using Engine.Core;
            using Engine.Core.Components;
            using Engine.Core.Ecs;
            using Engine.Core.Input;
            using Engine.Physics2D;
            using Engine.Rendering;
            using Engine.Runtime;
            using Engine.Tilemaps;
            using Silk.NET.OpenGL;

            namespace __NAME__;

            /// <summary>
            /// A small platformer: the hero (an entity with a <see cref="PlatformerIntent"/>) runs and jumps on a tile level. The level is data
            /// (<c>scenes/main.scene.json</c>, <c>levels/ground.layer.json</c>, <c>tilesets/world.tileset.json</c>); paint tiles in the editor's Tiles panel and the
            /// collision follows. This class only reads the keys, runs the systems and follows the hero with the camera.
            /// </summary>
            public sealed class __NAME__Game : Game
            {
                private const string MainScene = "scenes/main.scene.json";
                private const string GroundLayer = "levels/ground.layer.json";

                private readonly Camera2D _camera = new();
                private readonly EntityAction<PlatformerIntent> _applyInput;
                private SpriteBatch _batch = null!;
                private PhysicsSystem _physics = null!;
                private Entity _hero;
                private float _moveX;
                private bool _jumpHeld;
                private bool _jumpPressed;

                /// <summary>Creates the game and binds the keys; <paramref name="launch"/> says where its assets are.</summary>
                public __NAME__Game(GameLaunch? launch = null) : base("__NAME__", launch: launch)
                {
                    Actions
                        .BindAxis("MoveX", KeyCode.A, KeyCode.D)
                        .BindAxis("MoveX", KeyCode.Left, KeyCode.Right)
                        .Bind("Jump", KeyCode.Space)
                        .Bind("Jump", KeyCode.W)
                        .Bind("Jump", KeyCode.Up);
                    _applyInput = ApplyInput; // created once: a per-frame query must not allocate a delegate
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
                    _hero = World.RequireFirst<PlatformerIntent>();

                    _camera.ViewportSize = new Vector2(ViewportWidth, ViewportHeight);
                    ViewportResized += (width, height) => _camera.ViewportSize = new Vector2(width, height);
                    _camera.Position = HeroDrawPosition();

                    // Fixed step: remember where everything was (for smooth drawing), let the controller decide velocities, then move bodies.
                    _physics = new PhysicsSystem(BuildCollisionGrid());
                    FixedSystems.Add(new PreviousTransformSystem());
                    FixedSystems.Add(new PlatformerControllerSystem());
                    FixedSystems.Add(_physics);

                    RenderSystems.Add(new TileLayerRenderSystem(_batch, _camera, Assets, foreground: false));
                    RenderSystems.Add(new SpriteRenderSystem(_batch, _camera, Assets, RenderClock));
                }

                /// <inheritdoc />
                protected override void Unload() => _batch.Dispose(); // textures belong to Assets, which disposes them

                /// <inheritdoc />
                protected override void SceneReloaded() => _hero = World.RequireFirst<PlatformerIntent>();

                /// <inheritdoc />
                protected override void PlayStarted() => _physics.Grid = BuildCollisionGrid(); // the editor may have painted tiles

                /// <inheritdoc />
                protected override void Update(double dt)
                {
                    if (IsEditing) return; // the editor has the game frozen

                    _moveX = Actions.GetAxis("MoveX");
                    _jumpHeld = Actions.IsDown("Jump");
                    _jumpPressed = Actions.WasPressed("Jump");
                    World.Query(_applyInput);
                }

                /// <inheritdoc />
                protected override void Draw(double dt)
                {
                    // Follow the hero where it is drawn (between the last two fixed steps), in Draw so it is after the steps.
                    if (!IsEditing) _camera.Position = Vector2.Lerp(_camera.Position, HeroDrawPosition(), 1f - MathF.Exp(-6f * (float)dt));

                    _batch.ResetStats();
                    Gl.ClearColor(0.35f, 0.55f, 0.85f, 1f);
                    Gl.Clear(ClearBufferMask.ColorBufferBit);
                }

                /// <summary>The controller reads the intent in the fixed step; a jump press is latched until it does.</summary>
                private void ApplyInput(Entity entity, ref PlatformerIntent intent)
                {
                    intent.MoveX = _moveX;
                    intent.JumpHeld = _jumpHeld;
                    if (_jumpPressed) intent.JumpPressedPending = true;
                }

                private Vector2 HeroDrawPosition()
                {
                    var drawn = PreviousTransform.Interpolate(World.Get<Transform>(_hero), World.Get<PreviousTransform>(_hero), RenderClock.Alpha);
                    return new Vector2(drawn.Position.X, drawn.Position.Y);
                }

                /// <summary>Builds the physics grid from the ground layer's solid tiles, placed where the layer entity is.</summary>
                private TileGrid BuildCollisionGrid()
                {
                    var layerHandle = Assets.Load<TileLayer>(GroundLayer);
                    var origin = Vector2.Zero;
                    var tileSet = Engine.Core.Assets.AssetHandle<TileSet>.Invalid;
                    World.Query<Transform, TileLayerRenderer>((Entity _, ref Transform transform, ref TileLayerRenderer renderer) =>
                    {
                        if (renderer.Layer != layerHandle) return;

                        origin = new Vector2(transform.Position.X, transform.Position.Y);
                        tileSet = renderer.TileSet;
                    });

                    if (!tileSet.IsValid) throw new InvalidOperationException($"The scene has no tile layer entity for '{GroundLayer}'.");
                    return TileGridBuilder.FromLayer(Assets.Get(layerHandle), Assets.Get(tileSet), origin);
                }
            }
            """),

        ("Assets/tilesets/world.tileset.json", """
            {
              "texture": "tiles.png",
              "tileWidth": 16,
              "tileHeight": 16,
              "solid": [1, 2]
            }
            """),

        ("Assets/scenes/main.scene.json", """
            {
              "version": 1,
              "entities": [
                {
                  "components": {
                    "Name": { "value": "Ground layer" },
                    "Transform": {
                      "position": { "x": -480, "y": -192, "z": -5 },
                      "rotation": { "x": 0, "y": 0, "z": 0, "w": 1 },
                      "scale": { "x": 1, "y": 1, "z": 1 }
                    },
                    "TileLayerRenderer": {
                      "layer": "levels/ground.layer.json",
                      "tileSet": "tilesets/world.tileset.json",
                      "tint": { "r": 1, "g": 1, "b": 1, "a": 1 },
                      "foreground": false,
                      "castsShadows": false
                    }
                  }
                },
                {
                  "components": {
                    "Name": { "value": "Hero" },
                    "PreviousTransform": {},
                    "Transform": {
                      "position": { "x": -400, "y": -90, "z": 10 },
                      "rotation": { "x": 0, "y": 0, "z": 0, "w": 1 },
                      "scale": { "x": 1, "y": 1, "z": 1 }
                    },
                    "Sprite": {
                      "texture": "sprites/square.png",
                      "size": { "x": 28, "y": 30 },
                      "source": { "x": 0, "y": 0, "width": 32, "height": 32 },
                      "color": { "r": 1, "g": 0.75, "b": 0.3, "a": 1 },
                      "pivot": { "x": 0.5, "y": 0.5 },
                      "flip": "None"
                    },
                    "BoxCollider": { "size": { "x": 28, "y": 30 }, "offset": { "x": 0, "y": 0 } },
                    "PhysicsBody": { "velocity": { "x": 0, "y": 0 }, "gravityScale": 1, "contacts": "None" },
                    "PlatformerController": {
                      "tuning": {
                        "moveSpeed": 220,
                        "groundAcceleration": 2200,
                        "groundDeceleration": 2600,
                        "airAcceleration": 1500,
                        "airDeceleration": 900,
                        "jumpSpeed": 640,
                        "coyoteTime": 0.1,
                        "jumpBufferTime": 0.1,
                        "jumpCutMultiplier": 0.5,
                        "fallGravityMultiplier": 1.6
                      },
                      "coyoteTimer": 0,
                      "jumpBufferTimer": 0,
                      "isJumping": false
                    },
                    "PlatformerIntent": { "moveX": 0, "jumpHeld": false, "jumpPressedPending": false }
                  }
                }
              ]
            }
            """),
    ];

    /// <summary>The ground layer: a floor two tiles thick, walls at both edges and three floating platforms. Tile 1 is grass, 2 is dirt; rows run top to bottom.</summary>
    private static string PlatformerLevelJson()
    {
        var grid = new int[LevelRows, LevelColumns];
        for (int row = LevelRows - 2; row < LevelRows; row++)
            for (int column = 0; column < LevelColumns; column++) grid[row, column] = row == LevelRows - 2 ? 1 : 2;

        for (int row = 0; row < LevelRows - 2; row++) grid[row, 0] = grid[row, LevelColumns - 1] = 2;
        for (int column = 6; column <= 10; column++) grid[7, column] = 1;
        for (int column = 14; column <= 18; column++) grid[5, column] = 1;
        for (int column = 21; column <= 25; column++) grid[7, column] = 1;

        var json = new StringBuilder();
        json.Append("{\n  \"tileSize\": ").Append(LevelTileSize).Append(",\n  \"originX\": 0,\n  \"originY\": 0,\n  \"rows\": [\n");
        for (int row = 0; row < LevelRows; row++)
        {
            json.Append("    [");
            for (int column = 0; column < LevelColumns; column++) json.Append(column == 0 ? "" : ", ").Append(grid[row, column]);
            json.Append(row == LevelRows - 1 ? "]\n" : "],\n");
        }

        return json.Append("  ]\n}\n").ToString();
    }

    /// <summary>A 32x16 atlas of two 16x16 tiles: grass on dirt, and plain dirt.</summary>
    private static byte[] PlatformerTiles()
    {
        const int width = 32, height = 16;
        var pixels = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool grass = x < 16 && y < 4;
                bool speck = (x * 7 + y * 13) % 11 == 0;
                (byte r, byte g, byte b) = grass ? ((byte)90, (byte)185, (byte)70) : speck ? ((byte)120, (byte)85, (byte)55) : ((byte)150, (byte)105, (byte)70);
                int i = (y * width + x) * 4;
                pixels[i] = r;
                pixels[i + 1] = g;
                pixels[i + 2] = b;
                pixels[i + 3] = 255;
            }
        }

        return PngWriter.Encode(width, height, pixels);
    }
}
