namespace Engine.Editor.Projects;

// The 3D starter: files that replace or add to the empty project's. Meshes are made in code (the engine's MeshBuilder); the scene refers to them by name.
public static partial class ProjectTemplate
{
    private static readonly (string Path, string Content)[] Scene3DFiles =
    [
        ("Game/__NAME__Game.cs", """
            using System.Numerics;
            using Engine.Core.Input;
            using Engine.Rendering;
            using Engine.Rendering3D;
            using Engine.Runtime;
            using Silk.NET.OpenGL;

            namespace __NAME__;

            /// <summary>
            /// A 3D scene viewed with a fly camera: W/A/S/D move, Q/E go down and up, hold the right mouse button and move the mouse to look. The world is
            /// data (<c>scenes/main.scene.json</c> and the materials); the meshes it names are made in <see cref="Load"/>.
            /// </summary>
            public sealed class __NAME__Game : Game
            {
                private const string MainScene = "scenes/main.scene.json";
                private const float MoveSpeed = 6f;
                private const float LookSensitivity = 0.0035f;

                private readonly Camera3D _camera = new();
                private readonly FlyCamera _fly = new() { Position = new Vector3(0f, 3f, 9f), Pitch = -0.25f };
                private MeshRenderSystem _meshes = null!;
                private Vector2 _lastMouse;
                private bool _wasLooking;

                /// <summary>Creates the game and binds the keys; <paramref name="launch"/> says where its assets are.</summary>
                public __NAME__Game(GameLaunch? launch = null) : base("__NAME__", launch: launch)
                {
                    Actions
                        .BindAxis("MoveForward", KeyCode.S, KeyCode.W)
                        .BindAxis("MoveRight", KeyCode.A, KeyCode.D)
                        .BindAxis("MoveUp", KeyCode.Q, KeyCode.E)
                        .Bind("Look", MouseButton.Right);
                }

                /// <inheritdoc />
                protected override Camera3D? SceneCamera3D => _camera;

                /// <inheritdoc />
                protected override string? ScenePath => Path.Combine(Launch.AssetRoot, MainScene);

                /// <inheritdoc />
                protected override void Load()
                {
                    // Procedural assets first: the scene and its materials refer to them by name.
                    Assets.Add("meshes/cube", new Mesh(Gl, MeshBuilder.Cube(Vector3.One)));
                    Assets.Add("meshes/sphere", new Mesh(Gl, MeshBuilder.Sphere(0.5f)));
                    Assets.Add("meshes/floor", new Mesh(Gl, MeshBuilder.Plane(40f, 40f, uvRepeat: 20f)));
                    Assets.Add("checker", ProceduralTextures.Checker(Gl));
                    Scenes.LoadFile(MainScene);

                    _camera.ViewportSize = new Vector2(ViewportWidth, ViewportHeight);
                    ViewportResized += (width, height) => _camera.ViewportSize = new Vector2(width, height);
                    _fly.ApplyTo(_camera);

                    _meshes = new MeshRenderSystem(Gl, Assets, _camera, RenderClock);
                    _meshes.Lighting.DrawSky = true; // a gradient sky with the sun where the light points
                    RenderSystems.Add(_meshes);
                }

                /// <inheritdoc />
                protected override void Unload() => _meshes.Dispose();

                /// <inheritdoc />
                protected override void Update(double dt)
                {
                    if (IsEditing) return; // the editor owns the camera and the mouse

                    bool looking = Actions.IsDown("Look");
                    var mouse = Input.MousePosition;
                    if (looking && _wasLooking)
                    {
                        var delta = mouse - _lastMouse;
                        _fly.Look(-delta.X * LookSensitivity, -delta.Y * LookSensitivity); // mouse right = turn right = negative yaw
                    }

                    _wasLooking = looking;
                    _lastMouse = mouse;

                    float step = MoveSpeed * (float)dt;
                    _fly.Move(Actions.GetAxis("MoveForward"), Actions.GetAxis("MoveRight"), Actions.GetAxis("MoveUp"), step);
                }

                /// <inheritdoc />
                protected override void Draw(double dt)
                {
                    // While the editor is open it owns the camera, so follow it: flying on afterwards starts where it ended.
                    if (IsEditing) _fly.CopyFrom(_camera);
                    else _fly.ApplyTo(_camera);

                    Gl.ClearColor(0.45f, 0.62f, 0.85f, 1f);
                    Gl.Clear(ClearBufferMask.ColorBufferBit);
                }
            }
            """),

        ("Assets/materials/floor.material.json", """{ "baseColor": [0.55, 0.75, 0.55], "texture": "checker", "metallic": 0.0, "roughness": 0.8 }"""),
        ("Assets/materials/red.material.json", """{ "baseColor": [0.9, 0.25, 0.2], "metallic": 0.0, "roughness": 0.45 }"""),
        ("Assets/materials/gold.material.json", """{ "baseColor": [1.0, 0.78, 0.3], "metallic": 1.0, "roughness": 0.3 }"""),
        ("Assets/materials/blue.material.json", """{ "baseColor": [0.15, 0.4, 0.95], "metallic": 0.0, "roughness": 0.3 }"""),

        ("Assets/scenes/main.scene.json", """
            {
              "version": 1,
              "entities": [
                {
                  "components": {
                    "Name": { "value": "Floor" },
                    "Transform": { "position": { "x": 0, "y": 0, "z": 0 }, "rotation": { "x": 0, "y": 0, "z": 0, "w": 1 }, "scale": { "x": 1, "y": 1, "z": 1 } },
                    "MeshRenderer": { "mesh": "meshes/floor", "material": "materials/floor.material.json" }
                  }
                },
                {
                  "components": {
                    "Name": { "value": "Sun" },
                    "Transform": { "position": { "x": 0, "y": 10, "z": 0 }, "rotation": { "x": -0.4215, "y": 0.2228, "z": 0.1076, "w": 0.8724 }, "scale": { "x": 1, "y": 1, "z": 1 } },
                    "DirectionalLight": { "color": { "r": 1, "g": 0.96, "b": 0.88, "a": 1 }, "intensity": 1.0 }
                  }
                },
                {
                  "components": {
                    "Name": { "value": "Red box" },
                    "Transform": { "position": { "x": -2.5, "y": 0.75, "z": 0 }, "rotation": { "x": 0, "y": 0.2588, "z": 0, "w": 0.9659 }, "scale": { "x": 1.5, "y": 1.5, "z": 1.5 } },
                    "MeshRenderer": { "mesh": "meshes/cube", "material": "materials/red.material.json" }
                  }
                },
                {
                  "components": {
                    "Name": { "value": "Gold sphere" },
                    "Transform": { "position": { "x": 0, "y": 0.8, "z": -1 }, "rotation": { "x": 0, "y": 0, "z": 0, "w": 1 }, "scale": { "x": 1.6, "y": 1.6, "z": 1.6 } },
                    "MeshRenderer": { "mesh": "meshes/sphere", "material": "materials/gold.material.json" }
                  }
                },
                {
                  "components": {
                    "Name": { "value": "Blue box" },
                    "Transform": { "position": { "x": 2.5, "y": 1, "z": 0.5 }, "rotation": { "x": 0, "y": 0, "z": 0, "w": 1 }, "scale": { "x": 1.2, "y": 2, "z": 1.2 } },
                    "MeshRenderer": { "mesh": "meshes/cube", "material": "materials/blue.material.json" }
                  }
                }
              ]
            }
            """),
    ];
}
