namespace Engine.Audio.Tests;

public class AudioMixerTests
{
    [Fact]
    public void A_new_mixer_has_only_a_full_volume_master()
    {
        var mixer = new AudioMixer();

        Assert.Single(mixer.Groups);
        Assert.Equal(1f, mixer.Master.EffectiveVolume);
    }

    [Fact]
    public void Effective_volume_multiplies_up_the_parent_chain()
    {
        var mixer = new AudioMixer();
        var music = mixer.CreateGroup("Music");
        var menu = mixer.CreateGroup("MenuMusic", music);
        mixer.Master.Volume = 0.5f;
        music.Volume = 0.5f;
        menu.Volume = 0.8f;

        Assert.Equal(0.2f, menu.EffectiveVolume, 5);
    }

    [Fact]
    public void Muting_a_group_silences_it_and_its_children_but_keeps_their_volumes()
    {
        var mixer = new AudioMixer();
        var music = mixer.CreateGroup("Music");
        var menu = mixer.CreateGroup("Menu", music);
        var sfx = mixer.CreateGroup("Sfx");
        music.Muted = true;

        Assert.Equal(0f, menu.EffectiveVolume);
        Assert.Equal(1f, sfx.EffectiveVolume);

        music.Muted = false;
        Assert.Equal(1f, menu.EffectiveVolume);
    }

    [Fact]
    public void Volumes_are_clamped()
    {
        var group = new AudioMixer().Master;

        group.Volume = 3f;
        Assert.Equal(1f, group.Volume);
        group.Volume = -1f;
        Assert.Equal(0f, group.Volume);
    }

    [Fact]
    public void Groups_are_found_by_name_ignoring_case()
    {
        var mixer = new AudioMixer();
        var sfx = mixer.CreateGroup("Sfx");

        Assert.Same(sfx, mixer.Find("sfx"));
        Assert.True(mixer.TryFind("SFX", out var found));
        Assert.Same(sfx, found);
        Assert.False(mixer.TryFind("nope", out _));
        Assert.Throws<KeyNotFoundException>(() => mixer.Find("nope"));
    }

    [Fact]
    public void Duplicate_empty_and_foreign_parents_are_rejected()
    {
        var mixer = new AudioMixer();
        mixer.CreateGroup("Sfx");
        var foreign = new AudioMixer().CreateGroup("Other");

        Assert.Throws<ArgumentException>(() => mixer.CreateGroup("sfx"));
        Assert.Throws<ArgumentException>(() => mixer.CreateGroup(" "));
        Assert.Throws<ArgumentException>(() => mixer.CreateGroup("X", foreign));
    }
}
