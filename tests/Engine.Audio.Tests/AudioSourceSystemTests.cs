using System.Numerics;
using Engine.Audio.Tests.Fixtures;
using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;

namespace Engine.Audio.Tests;

public sealed class AudioSourceSystemTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "audio-system-" + Guid.NewGuid().ToString("N"));
    private readonly World _world = new();
    private readonly RecordingAudioDevice _device = new();
    private readonly AssetManager _assets;
    private readonly AudioSourceSystem _system;
    private readonly AssetHandle<AudioClip> _clip;

    public AudioSourceSystemTests()
    {
        Directory.CreateDirectory(_root);
        _assets = new AssetManager(_root);
        _clip = _assets.Add("sounds/beep", new AudioClip(8000, 1, new byte[8]));
        _system = new AudioSourceSystem(_device, _assets);
    }

    public void Dispose()
    {
        _assets.Dispose();
        Directory.Delete(_root, recursive: true);
    }

    private Entity Emitter(AudioSource source, Vector3 position = default)
    {
        var e = _world.CreateEntity();
        _world.Add(e, source);
        _world.Add(e, Transform.At(position));
        return e;
    }

    [Fact]
    public void A_play_on_start_source_starts_once_and_not_again_when_it_ends()
    {
        var e = Emitter(AudioSource.Create(_clip));

        _system.Update(_world, 0.016);
        _device.Voices[0].Stopped = true; // the sound ended
        _system.Update(_world, 0.016);

        Assert.Single(_device.Voices);
        Assert.True(_world.IsAlive(e));
    }

    [Fact]
    public void A_source_with_play_on_start_off_waits_for_play()
    {
        var source = AudioSource.Create(_clip);
        source.PlayOnStart = false;
        var e = Emitter(source);

        _system.Update(_world, 0.016);
        Assert.Empty(_device.Voices);

        _system.Play(_world, e);
        Assert.Single(_device.Voices);
    }

    [Fact]
    public void Play_restarts_a_sound_that_is_already_playing()
    {
        var e = Emitter(AudioSource.Create(_clip));
        _system.Update(_world, 0.016);

        _system.Play(_world, e);

        Assert.Equal(2, _device.Voices.Count);
        Assert.True(_device.Voices[0].Stopped);
        Assert.False(_device.Voices[1].Stopped);
    }

    [Fact]
    public void A_positioned_source_follows_its_entity()
    {
        var source = AudioSource.Create(_clip);
        source.Space = AudioSpace.Positional3D;
        source.Loop = true;
        var e = Emitter(source, new Vector3(1, 2, 3));
        _system.Update(_world, 0.016);
        Assert.Equal(new Vector3(1, 2, 3), _device.Voices[0].Position);

        _world.Set(e, Transform.At(new Vector3(7, 0, 0)));
        _system.Update(_world, 0.016);

        Assert.Equal(new Vector3(7, 0, 0), _device.Voices[0].CurrentPosition);
    }

    [Fact]
    public void A_2d_source_ignores_depth_so_draw_order_is_not_distance()
    {
        var source = AudioSource.Create(_clip);
        source.Space = AudioSpace.Positional2D;
        var listener = _world.CreateEntity();
        _world.Add(listener, new AudioListener());
        _world.Add(listener, Transform.At(new Vector3(0, 0, 0)));
        Emitter(source, new Vector3(4, 5, 9));

        _system.Update(_world, 0.016);

        Assert.Equal(new Vector3(4, 5, 0), _device.Voices[0].Position);
    }

    [Fact]
    public void The_listener_entity_places_the_devices_listener()
    {
        var e = _world.CreateEntity();
        _world.Add(e, new AudioListener());
        _world.Add(e, Transform.At(new Vector3(3, 4, 5)));

        _system.Update(_world, 0.016);

        Assert.Equal(new Vector3(3, 4, 5), _device.ListenerPosition);
        Assert.Equal(-Vector3.UnitZ, _device.ListenerForward);
    }

    [Fact]
    public void Volume_changes_reach_a_playing_sound()
    {
        var e = Emitter(AudioSource.Create(_clip));
        _system.Update(_world, 0.016);

        var source = _world.Get<AudioSource>(e);
        source.Volume = 0.25f;
        _world.Set(e, source);
        _system.Update(_world, 0.016);

        Assert.Equal(0.25f, _device.Voices[0].CurrentVolume);
    }

    [Fact]
    public void Destroying_the_entity_or_removing_the_source_stops_its_sound()
    {
        var a = Emitter(AudioSource.Create(_clip));
        var b = Emitter(AudioSource.Create(_clip));
        _system.Update(_world, 0.016);

        _world.DestroyEntity(a);
        _world.Remove<AudioSource>(b);
        _system.Update(_world, 0.016);

        Assert.All(_device.Voices, v => Assert.True(v.Stopped));
        Assert.Equal(0, _system.TrackedCount);
    }

    [Fact]
    public void A_named_group_is_looked_up_and_an_unknown_one_fails_loudly()
    {
        var sfx = _device.Mixer.CreateGroup("Sfx");
        var source = AudioSource.Create(_clip);
        source.Group = "sfx";
        Emitter(source);
        _system.Update(_world, 0.016);
        Assert.Same(sfx, _device.Voices[0].Group);

        var bad = AudioSource.Create(_clip);
        bad.Group = "Nope";
        Emitter(bad);
        Assert.Throws<InvalidOperationException>(() => _system.Update(_world, 0.016));
    }

    [Fact]
    public void A_source_without_a_clip_fails_loudly()
    {
        Emitter(AudioSource.Create(default));

        Assert.Throws<InvalidOperationException>(() => _system.Update(_world, 0.016));
    }
}
