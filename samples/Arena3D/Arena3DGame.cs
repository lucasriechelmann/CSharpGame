using System.Numerics;
using Engine.Audio;
using Engine.Core;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Input;
using Engine.Physics3D;
using Engine.Rendering;
using Engine.Rendering3D;
using Engine.Runtime;
using Silk.NET.OpenGL;

namespace Arena3D;

/// <summary>
/// Orb Run, a small 3D game: collect every glowing orb in the arena before the clock runs out. Walk with W/A/S/D, jump with Space, move the mouse to turn the camera around the player (the pointer is captured; Escape pauses), scroll to zoom. Falling off the edge puts you back at the start and costs time.
/// </summary>
/// <remarks>
/// Almost everything is data (<c>Assets/scenes/arena.scene.json</c>, the prefab and the materials). The rules live in <see cref="OrbGameSession"/> (plain
/// code, unit-tested); <see cref="OrbCollectSystem"/> only detects touches and reports them. Split by topic: this file (setup and the loop),
/// <c>Arena3DGame.Camera.cs</c> and <c>Arena3DGame.Hud.cs</c>.
/// </remarks>
public sealed partial class Arena3DGame : Game
{
    private const string ArenaScene = "scenes/arena.scene.json";

    /// <summary>Below this height the player has fallen off the arena.</summary>
    private const float FallLevel = -6f;

    private readonly Camera3D _camera = new();
    private OrbGameSession _session = new(1);
    private MeshRenderSystem _meshes = null!;
    private PhysicsSystem3D _physics = null!;
    private Entity _player;
    private Vector3 _spawn;
    private AudioGroup _sfx = null!;
    private AudioClip _orbSound = null!;
    private AudioClip _jumpSound = null!;
    private AudioClip _fallSound = null!;
    private AudioClip _winSound = null!;
    private AudioClip _loseSound = null!;
    private OrbPhase _lastPhase = OrbPhase.Playing;

    /// <summary>Registers input bindings and the game's own components for scene files.</summary>
    public Arena3DGame(GameLaunch? launch = null) : base("Orb Run", launch: launch)
    {
        Actions
            .BindAxis(GameActions.MoveForward, KeyCode.S, KeyCode.W)
            .BindAxis(GameActions.MoveRight, KeyCode.A, KeyCode.D)
            .Bind(GameActions.Jump, KeyCode.Space)
            .Bind(GameActions.Restart, KeyCode.Enter)
            .Bind(GameActions.Pause, KeyCode.Escape)
            .Bind(GameActions.Pause, KeyCode.P)
            .Bind(GameActions.MenuUp, KeyCode.Up)
            .Bind(GameActions.MenuDown, KeyCode.Down)
            .Bind(GameActions.MenuConfirm, KeyCode.Enter);
        CaptureMouse = true;

        Components.Register<Orb>("Orb");
        Components.Register<OrbBob>("OrbBob");
    }

    /// <inheritdoc />
    protected override Camera3D? SceneCamera3D => _camera;

    /// <inheritdoc />
    protected override string? ScenePath => Path.Combine(Launch.AssetRoot, ArenaScene);

    /// <inheritdoc />
    protected override void Load()
    {
        // Procedural assets first: the scene and its materials refer to them by name.
        Assets.Add("meshes/cube", new Mesh(Gl, MeshBuilder.Cube(Vector3.One)));
        Assets.Add("meshes/sphere", new Mesh(Gl, MeshBuilder.Sphere(0.5f)));
        Assets.Add("meshes/capsule", new Mesh(Gl, MeshBuilder.Capsule(0.4f, 1.8f)));
        Assets.Add("meshes/floor", new Mesh(Gl, MeshBuilder.Plane(24f, 24f, uvRepeat: 12f)));
        Assets.Add("checker", ProceduralTextures.Checker(Gl));

        LoadSounds();
        Scenes.LoadFile(ArenaScene);
        BindWorld();

        _camera.ViewportSize = new Vector2(ViewportWidth, ViewportHeight);
        ViewportResized += (width, height) => _camera.ViewportSize = new Vector2(width, height);
        SnapCamera();

        // Per frame the orbs bob; per fixed step: record positions, walk the player, let physics move everything, then look for pick-ups.
        FrameSystems.Add(new OrbBobSystem());
        FrameSystems.Add(new AudioSourceSystem(Audio, Assets)); // the lamp's positional hum
        FixedSystems.Add(new PreviousTransformSystem());
        FixedSystems.Add(new CharacterControllerSystem3D()); // before physics: it sets the velocity physics then applies
        _physics = new PhysicsSystem3D();
        FixedSystems.Add(_physics);
        FixedSystems.Add(new OrbCollectSystem(CollectOrb));
        _meshes = new MeshRenderSystem(Gl, Assets, _camera, RenderClock);
        RenderSystems.Add(_meshes);
        Log.Info("Game", $"Arena loaded: {World.EntityCount} entities, {_session.OrbsTotal} orbs");
    }

    /// <inheritdoc />
    protected override void Unload() => _meshes.Dispose();

    /// <inheritdoc />
    protected override void SceneReloaded() => BindWorld();

    /// <inheritdoc />
    protected override void PlayStarted()
    {
        BindWorld(); // a fresh run from the scene as edited
        SnapCamera();
    }

    /// <inheritdoc />
    protected override void Update(double dt)
    {
        if (IsEditing) return; // the editor owns the camera and the mouse

        if (Actions.WasPressed(GameActions.Pause)) TogglePause();
        if (IsPaused)
        {
            UpdatePauseMenu();
            return;
        }

        UpdateLook();
        AnnounceEnd(); // a run that ended in the last step plays its jingle now
        if (_session.Phase == OrbPhase.Playing)
        {
            _session.Tick(dt);
            DrivePlayer(dt);
            CheckFall();
        }
        else
        {
            StopPlayer();
            AnimatePlayer(Vector3.Zero, dt);
            if (Actions.WasPressed(GameActions.Restart)) Restart();
        }
    }

    /// <inheritdoc />
    protected override void Draw(double dt)
    {
        if (!IsEditing) FollowPlayer(dt);
        Gl.ClearColor(0.45f, 0.62f, 0.85f, 1f);
        Gl.Clear(ClearBufferMask.ColorBufferBit);
    }

    /// <summary>Finds the player and counts the orbs after the world was loaded or replaced, and starts a fresh run.</summary>
    private void BindWorld()
    {
        _player = World.RequireFirst<CharacterController3D>(); // the scene's walking entity
        _spawn = World.Get<Transform>(_player).Position;
        _session.Reset(Math.Max(World.CountWith<Orb>(), 1));
        _lastPhase = OrbPhase.Playing;
    }

    /// <summary>Starts the run over: the arena is loaded afresh so every orb is back (<see cref="SceneReloaded"/> rebinds the world).</summary>
    private void Restart()
    {
        ReloadScene(ArenaScene);
        SnapCamera();
    }

    private void CollectOrb()
    {
        _session.CollectOrb();
        if (_session.Phase == OrbPhase.Playing) Audio.PlayIn(_orbSound, _sfx, 0.8f);
    }

    /// <summary>Loads the sound effects and puts them in a mixer group the player could turn down.</summary>
    private void LoadSounds()
    {
        _sfx = Audio.Mixer.CreateGroup("Sfx");
        var music = Audio.Mixer.CreateGroup("Music");
        Audio.Mixer.CreateGroup("Ambient"); // the lamp's AudioSource names it in the scene
        _orbSound = Assets.Get(Assets.Load<AudioClip>("sounds/orb.wav"));
        _jumpSound = Assets.Get(Assets.Load<AudioClip>("sounds/jump.wav"));
        _fallSound = Assets.Get(Assets.Load<AudioClip>("sounds/fall.wav"));
        _winSound = Assets.Get(Assets.Load<AudioClip>("sounds/win.wav"));
        _loseSound = Assets.Get(Assets.Load<AudioClip>("sounds/lose.wav"));
        foreach (var clip in new[] { _orbSound, _jumpSound, _fallSound, _winSound, _loseSound }) Audio.Preload(clip);

        // The music is long, so it is streamed from the file instead of being decoded whole; the device owns the stream from here.
        Audio.PlayStream(WavStream.Open(Path.Combine(Launch.AssetRoot, "sounds", "music.wav")), music, 0.3f, loop: true);
    }

    /// <summary>Plays the win or lose jingle once, on the frame the run ends.</summary>
    private void AnnounceEnd()
    {
        if (_session.Phase == _lastPhase) return;

        _lastPhase = _session.Phase;
        if (_lastPhase == OrbPhase.Won) Audio.PlayIn(_winSound, _sfx);
        else if (_lastPhase == OrbPhase.Lost) Audio.PlayIn(_loseSound, _sfx);
    }

    /// <summary>
    /// Writes the walk keys into the player's <see cref="CharacterController3D"/> as a direction relative to where the camera looks (W walks away from
    /// the camera along the ground) and latches a jump press; the fixed step turns that into velocity.
    /// </summary>
    private void DrivePlayer(double dt)
    {
        var forward = _orbit.Forward with { Y = 0f };
        forward = forward.LengthSquared() > 1e-6f ? Vector3.Normalize(forward) : -Vector3.UnitZ;
        var right = Vector3.Cross(forward, Vector3.UnitY);
        var wish = forward * Actions.GetAxis(GameActions.MoveForward) + right * Actions.GetAxis(GameActions.MoveRight);

        ref var controller = ref World.Get<CharacterController3D>(_player);
        controller.Move = new Vector2(wish.X, wish.Z);
        AnimatePlayer(wish, dt);
        if (Actions.WasPressed(GameActions.Jump))
        {
            controller.JumpPressed = true;
            if (World.Get<PhysicsBody3D>(_player).IsGrounded) Audio.PlayIn(_jumpSound, _sfx, 0.7f);
        }
    }

    /// <summary>After the run ends the player stands still.</summary>
    private void StopPlayer()
    {
        ref var controller = ref World.Get<CharacterController3D>(_player);
        controller.Move = Vector2.Zero;
    }

    /// <summary>A player that dropped below the arena goes back to the start and the clock pays for it.</summary>
    private void CheckFall()
    {
        ref var transform = ref World.Get<Transform>(_player);
        if (transform.Position.Y >= FallLevel) return;

        transform.Position = _spawn;
        World.Get<PreviousTransform>(_player) = default; // so the camera and the picture do not streak from the pit to the start
        World.Get<PhysicsBody3D>(_player).Velocity = Vector3.Zero;
        _session.PlayerFell();
        Audio.PlayIn(_fallSound, _sfx, 0.7f);
        Log.Info("Game", $"Fell off the arena (time left {_session.TimeLeft:F0} s)");
    }
}
