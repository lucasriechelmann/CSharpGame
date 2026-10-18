namespace Engine.Audio.Tests;

public class SpatializationTests
{
    private static readonly Spatialization Model = new(MinDistance: 2f, MaxDistance: 10f, Rolloff: 1f);

    [Theory]
    [InlineData(0f, 1f)]
    [InlineData(2f, 1f)]
    [InlineData(4f, 0.5f)]
    [InlineData(10f, 0.2f)]
    [InlineData(100f, 0.2f)]
    public void Attenuation_is_full_inside_the_minimum_inverse_beyond_it_and_flat_past_the_maximum(float distance, float expected)
    {
        Assert.Equal(expected, Model.Attenuation(distance), 5);
    }

    [Fact]
    public void Zero_rolloff_never_fades_and_larger_rolloff_fades_faster()
    {
        Assert.Equal(1f, new Spatialization(1f, 50f, 0f).Attenuation(40f));
        Assert.True(new Spatialization(1f, 50f, 2f).Attenuation(5f) < new Spatialization(1f, 50f, 1f).Attenuation(5f));
    }

    [Fact]
    public void Validate_rejects_nonsense()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Spatialization(0f).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new Spatialization(5f, 2f).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new Spatialization(1f, 5f, -1f).Validate());
        Spatialization.Default.Validate();
    }
}
