using Engine.Audio;
using Engine.Core.Ecs;
using Engine.Physics2D;
using Engine.Rendering;

namespace Platformer;

// Game flow: sounds, the run's session, freezing the hero when the run ends, the exit door, blinking while invulnerable, restart.
public sealed partial class PlatformerGame
{
    /// <summary>Sprite-sheet rectangle (pixels) of the open exit door in <c>items.png</c>; the locked door is the prefab's default.</summary>
    private static readonly Rect OpenDoorSource = new(80f, 0f, 16f, 16f);

    private static readonly Rect LockedDoorSource = new(64f, 0f, 16f, 16f);

    /// <summary>Sprite-sheet rectangles of a checkpoint flag that has not been reached (down) and has (raised).</summary>
    private static readonly Rect FlagDownSource = new(96f, 0f, 16f, 16f);

    private static readonly Rect FlagRaisedSource = new(112f, 0f, 16f, 16f);

    /// <summary>Loads the gameplay sound effects and uploads them to the sound card so the first play does not hitch.</summary>
    private GameSounds LoadGameSounds()
    {
        AudioClip Load(string name)
        {
            var clip = Assets.Get(Assets.Load<AudioClip>($"sounds/{name}.wav"));
            Audio.Preload(clip);
            return clip;
        }

        return new GameSounds(Load("coin"), Load("hurt"), Load("stomp"), Load("win"), Load("lose"), Load("unlock"));
    }

    /// <summary>Counts the coin entities in the world (one-off, so the capturing closure is fine).</summary>
    private int CountCoins()
    {
        int count = 0;
        World.Query<Coin>((Entity _, ref Coin _) => count++);
        return count;
    }

    private void UpdateFlow(double dt)
    {
        _session.Tick(dt);
        RecordFinishedRun();

        if (Actions.WasPressed(GameActions.Restart)) RestartRun();

        // The input system has already written this frame's intent; once the run is over the hero stops obeying it.
        if (_session.Phase != GamePhase.Playing) World.Get<PlatformerIntent>(_hero) = default;

        if (_session.GoalUnlocked != _doorOpen) SetDoorOpen(_session.GoalUnlocked);
        ShowCheckpointFlags();

        // Blink while invulnerable so the player can see the grace period.
        bool blinkOff = _session.IsInvulnerable && ((int)(_session.Elapsed * 12) & 1) == 0;
        World.Get<Sprite>(_hero).Color = new Color(1f, 1f, 1f, blinkOff ? 0.3f : 1f);
    }

    /// <summary>The editor's Play: the edited world becomes a fresh run (new session, clock and score), with collision rebuilt from the level data.</summary>
    protected override void PlayStarted() => StartFreshRun();

    /// <summary>The editor's Stop: the world is back to its edited state, so discard the run that was played.</summary>
    protected override void PlayStopped() => StartFreshRun();

    private void StartFreshRun()
    {
        IsPaused = false;
        RebuildCollisionGrid(); // the editor may have painted tiles
        _session.Reset(CountCoins());
        _session.SetStart(HeroPosition());
        ResetRunRecord();
        SetDoorOpen(false);
        SnapCameraToHero();
    }

    /// <summary>Reloads the level scene (fresh coins and creatures) and starts a new session.</summary>
    private void RestartRun()
    {
        World.Clear();
        Scenes.LoadFile(LevelScene);
        _hero = FindHero();
        _session.Reset(CountCoins());
        _session.SetStart(HeroPosition());
        ResetRunRecord();
        SetDoorOpen(false);
        SnapCameraToHero();
        ShowStatus("New run");
        Log.Info("Game", $"New run: {_session.CoinsTotal} coins to collect");
    }

    /// <summary>Shows the raised-flag art on every checkpoint the hero has reached (the flag state lives in the saved component).</summary>
    private void ShowCheckpointFlags() =>
        World.Query<Sprite, Checkpoint>((Entity _, ref Sprite sprite, ref Checkpoint flag) => sprite.Source = flag.Reached ? FlagRaisedSource : FlagDownSource);

    /// <summary>Switches the exit door's art and brightness between locked and open.</summary>
    private void SetDoorOpen(bool open)
    {
        _doorOpen = open;
        World.Query<Sprite, Goal>((Entity _, ref Sprite sprite, ref Goal _) => sprite.Source = open ? OpenDoorSource : LockedDoorSource);
    }
}
