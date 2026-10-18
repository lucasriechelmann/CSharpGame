using System.Numerics;
using Engine.Audio.Tests.Fixtures;

namespace Engine.Audio.Tests;

/// <summary>Mixer groups, positioned sounds and streams against the real OpenAL library; each test returns quietly with no output device, and all sounds are silent.</summary>
[Collection("OpenAL")]
public class OpenAlAudioDeviceFeatureTests
{
    private static AudioClip Tone(int seconds = 1)
    {
        const int rate = 8000;
        return new AudioClip(rate, 1, new byte[rate * 2 * seconds]);
    }

    private static WavStream ShortStream(int frames) => new(new MemoryStream(WavBuilder.Pcm(1, 8000, 16, new byte[frames * 2])));

    [Fact]
    public void A_positioned_sound_plays_and_can_be_moved_with_the_listener_placed()
    {
        if (!OpenAlAudioDevice.TryCreate(out var device, out _)) return;
        using (device)
        {
            device!.SetListener(Vector3.Zero, -Vector3.UnitZ, Vector3.UnitY);
            var voice = device.PlayAt(Tone(), new Vector3(5, 0, 0), Spatialization.Default, volume: 0f);

            device.SetPosition(voice, new Vector3(-5, 0, 0));

            Assert.True(device.IsPlaying(voice));
        }
    }

    [Fact]
    public void An_invalid_spatialization_is_rejected_before_anything_plays()
    {
        if (!OpenAlAudioDevice.TryCreate(out var device, out _)) return;
        using (device)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => device!.PlayAt(Tone(), Vector3.Zero, new Spatialization(0f)));
            Assert.Equal(0, device!.ActiveVoices);
        }
    }

    [Fact]
    public void A_group_and_set_volume_can_change_while_the_sound_plays()
    {
        if (!OpenAlAudioDevice.TryCreate(out var device, out _)) return;
        using (device)
        {
            var music = device!.Mixer.CreateGroup("Music");
            var voice = device.PlayIn(Tone(), music, volume: 0f, loop: true);

            music.Muted = true;
            device.SetVolume(voice, 0.5f);
            device.Update();

            Assert.True(device.IsPlaying(voice));
            device.Stop(voice);
            Assert.Equal(0, device.ActiveVoices);
        }
    }

    [Fact]
    public void A_stream_plays_to_its_end_then_frees_its_voice_and_disposes_the_source()
    {
        if (!OpenAlAudioDevice.TryCreate(out var device, out _)) return;
        using (device)
        {
            var voice = device!.PlayStream(ShortStream(400), volume: 0f);
            Assert.True(device.IsPlaying(voice));
            Assert.Equal(1, device.ActiveVoices);

            for (int i = 0; i < 200 && device.ActiveVoices > 0; i++)
            {
                Thread.Sleep(10); // 400 frames at 8 kHz is 50 ms of audio
                device.Update();
            }

            Assert.Equal(0, device.ActiveVoices);
            Assert.False(device.IsPlaying(voice));
        }
    }

    [Fact]
    public void A_looping_stream_keeps_playing_until_stopped()
    {
        if (!OpenAlAudioDevice.TryCreate(out var device, out _)) return;
        using (device)
        {
            var voice = device!.PlayStream(ShortStream(400), volume: 0f, loop: true);

            for (int i = 0; i < 15; i++)
            {
                Thread.Sleep(10);
                device.Update();
            }

            Assert.True(device.IsPlaying(voice));
            device.Stop(voice);
            Assert.Equal(0, device.ActiveVoices);
        }
    }

    [Fact]
    public void Stopping_all_voices_also_releases_streams()
    {
        if (!OpenAlAudioDevice.TryCreate(out var device, out _)) return;
        using (device)
        {
            device!.PlayStream(ShortStream(400), volume: 0f, loop: true);
            device.Play(Tone(), volume: 0f, loop: true);

            device.StopAll();

            Assert.Equal(0, device.ActiveVoices);
        }
    }
}
