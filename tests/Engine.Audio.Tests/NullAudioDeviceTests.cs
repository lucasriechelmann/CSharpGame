namespace Engine.Audio.Tests;

public class NullAudioDeviceTests
{
    [Fact]
    public void Plays_nothing_and_says_why()
    {
        using var device = new NullAudioDevice("no sound card");
        var clip = new AudioClip(8000, 1, new byte[4]);

        var voice = device.Play(clip);

        Assert.True(voice.IsNone);
        Assert.False(device.IsPlaying(voice));
        Assert.False(device.IsAudible);
        Assert.Equal(0, device.ActiveVoices);
        Assert.Contains("no sound card", device.Description);
    }

    [Fact]
    public void Still_rejects_a_null_clip_so_bugs_surface_without_a_sound_card()
    {
        using var device = new NullAudioDevice("x");

        Assert.Throws<ArgumentNullException>(() => device.Play(null!));
    }
}
