namespace Engine.Audio.Tests;

public class AudioClipLoaderTests
{
    [Fact]
    public void Reports_the_file_name_for_a_bad_wav()
    {
        string path = Path.Combine(Path.GetTempPath(), $"bad-{Guid.NewGuid():N}.wav");
        File.WriteAllBytes(path, new byte[64]);
        try
        {
            var ex = Assert.Throws<InvalidDataException>(() => new AudioClipLoader().Load(path));

            Assert.Contains(path, ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
