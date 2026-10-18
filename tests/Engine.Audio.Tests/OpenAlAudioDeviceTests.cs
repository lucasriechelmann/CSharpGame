namespace Engine.Audio.Tests;

/// <summary>
/// Talks to the real OpenAL library. Each test returns quietly when the machine has no output device (a build server, say),
/// because that is an environment limit and not a bug; <see cref="NullAudioDevice"/> covers that path in its own tests.
/// </summary>
[Collection("OpenAL")]
public class OpenAlAudioDeviceTests
{
    private static AudioClip OneSecondTone()
    {
        const int rate = 8000;
        var pcm = new byte[rate * 2];
        for (int i = 0; i < rate; i++)
            BitConverter.GetBytes((short)(Math.Sin(2 * Math.PI * 440 * i / rate) * 8000)).CopyTo(pcm, i * 2);
        return new AudioClip(rate, 1, pcm);
    }

    [Fact]
    public void A_played_clip_is_playing_until_stopped()
    {
        if (!OpenAlAudioDevice.TryCreate(out var device, out _)) return;
        using (device)
        {
            var voice = device!.Play(OneSecondTone(), volume: 0f); // silent, so running the tests makes no noise

            Assert.True(device.IsPlaying(voice));
            Assert.Equal(1, device.ActiveVoices);

            device.Stop(voice);

            Assert.False(device.IsPlaying(voice));
            Assert.Equal(0, device.ActiveVoices);
        }
    }

    [Fact]
    public void Preloading_does_not_start_a_sound()
    {
        if (!OpenAlAudioDevice.TryCreate(out var device, out _)) return;
        using (device)
        {
            device!.Preload(OneSecondTone());

            Assert.Equal(0, device.ActiveVoices);
        }
    }

    [Fact]
    public void More_sounds_than_voices_never_fail_because_the_oldest_is_cut_off()
    {
        if (!OpenAlAudioDevice.TryCreate(out var device, out _)) return;
        using (device)
        {
            var clip = OneSecondTone();
            var first = device!.Play(clip, volume: 0f, loop: true);
            for (int i = 0; i < OpenAlAudioDevice.VoiceCount; i++) device.Play(clip, volume: 0f, loop: true);

            Assert.False(device.IsPlaying(first)); // its slot was taken over
            Assert.Equal(OpenAlAudioDevice.VoiceCount, device.ActiveVoices);

            device.StopAll();
            Assert.Equal(0, device.ActiveVoices);
        }
    }

    [Fact]
    public void Master_volume_is_clamped_to_zero_one()
    {
        if (!OpenAlAudioDevice.TryCreate(out var device, out _)) return;
        using (device)
        {
            device!.MasterVolume = 3f;
            Assert.Equal(1f, device.MasterVolume);

            device.MasterVolume = -1f;
            Assert.Equal(0f, device.MasterVolume);
        }
    }

    [Fact]
    public void Playing_after_dispose_throws()
    {
        if (!OpenAlAudioDevice.TryCreate(out var device, out _)) return;
        var clip = OneSecondTone();
        device!.Dispose();

        Assert.Throws<ObjectDisposedException>(() => device.Play(clip));
    }

    [Fact]
    public void A_non_positive_pitch_is_rejected()
    {
        if (!OpenAlAudioDevice.TryCreate(out var device, out _)) return;
        using (device)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => device!.Play(OneSecondTone(), pitch: 0f));
        }
    }
}
