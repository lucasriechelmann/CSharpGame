namespace Engine.Audio.Tests;

public class VoicePoolTests
{
    [Fact]
    public void Rejects_a_non_positive_capacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VoicePool(0));
    }

    [Fact]
    public void Hands_out_the_lowest_free_slot_first()
    {
        var pool = new VoicePool(3);

        var a = pool.Acquire(out bool stolenA);
        var b = pool.Acquire(out bool stolenB);

        Assert.Equal(0, a.Index);
        Assert.Equal(1, b.Index);
        Assert.False(stolenA);
        Assert.False(stolenB);
        Assert.Equal(2, pool.ActiveCount);
    }

    [Fact]
    public void A_released_slot_is_reused_with_a_new_generation()
    {
        var pool = new VoicePool(2);
        var first = pool.Acquire(out _);
        pool.Release(first.Index);

        var second = pool.Acquire(out bool stolen);

        Assert.Equal(first.Index, second.Index);
        Assert.NotEqual(first.Generation, second.Generation);
        Assert.False(stolen);
    }

    [Fact]
    public void A_stale_handle_is_not_current_after_its_slot_is_reused()
    {
        var pool = new VoicePool(1);
        var old = pool.Acquire(out _);
        pool.Release(old.Index);
        var fresh = pool.Acquire(out _);

        Assert.False(pool.IsCurrent(old));
        Assert.True(pool.IsCurrent(fresh));
    }

    [Fact]
    public void A_released_handle_is_no_longer_current()
    {
        var pool = new VoicePool(2);
        var voice = pool.Acquire(out _);

        pool.Release(voice.Index);

        Assert.False(pool.IsCurrent(voice));
    }

    [Fact]
    public void None_and_out_of_range_handles_are_never_current()
    {
        var pool = new VoicePool(2);

        Assert.False(pool.IsCurrent(VoiceId.None));
        Assert.False(pool.IsCurrent(new VoiceId(9, 1)));
        Assert.False(pool.IsCurrent(new VoiceId(-1, 1)));
    }

    [Fact]
    public void When_full_the_oldest_voice_is_stolen()
    {
        var pool = new VoicePool(2);
        var oldest = pool.Acquire(out _);
        var newer = pool.Acquire(out _);

        var third = pool.Acquire(out bool stolen);

        Assert.True(stolen);
        Assert.Equal(oldest.Index, third.Index);
        Assert.False(pool.IsCurrent(oldest));
        Assert.True(pool.IsCurrent(newer));
        Assert.True(pool.IsCurrent(third));
        Assert.Equal(2, pool.ActiveCount); // stealing does not change how many are busy
    }

    [Fact]
    public void Stealing_picks_the_oldest_by_start_time_not_by_slot_number()
    {
        var pool = new VoicePool(2);
        var a = pool.Acquire(out _); // slot 0, oldest
        var b = pool.Acquire(out _); // slot 1
        pool.Release(a.Index);
        var c = pool.Acquire(out _); // reuses slot 0, now the newest; slot 1 is the oldest busy voice

        var d = pool.Acquire(out bool stolen);

        Assert.True(stolen);
        Assert.Equal(b.Index, d.Index);
        Assert.True(pool.IsCurrent(c));
    }

    [Fact]
    public void Releasing_a_free_slot_does_not_corrupt_the_count()
    {
        var pool = new VoicePool(2);
        var voice = pool.Acquire(out _);
        pool.Release(voice.Index);

        pool.Release(voice.Index);

        Assert.Equal(0, pool.ActiveCount);
    }
}
