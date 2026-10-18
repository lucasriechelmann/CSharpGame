using System.Text;

namespace Engine.Audio.Tests.Fixtures;

/// <summary>Builds tiny WAV files in memory so decoder tests can state exactly which bytes they feed in.</summary>
internal static class WavBuilder
{
    public static byte[] Pcm(int channels, int sampleRate, int bits, byte[] data, bool extraChunkBeforeData = false, ushort format = 1)
    {
        using var stream = new MemoryStream();
        using var w = new BinaryWriter(stream, Encoding.ASCII);

        w.Write(Encoding.ASCII.GetBytes("RIFF"));
        w.Write(0); // patched below
        w.Write(Encoding.ASCII.GetBytes("WAVE"));

        w.Write(Encoding.ASCII.GetBytes("fmt "));
        w.Write(16);
        w.Write(format);
        w.Write((ushort)channels);
        w.Write(sampleRate);
        w.Write(sampleRate * channels * bits / 8);
        w.Write((ushort)(channels * bits / 8));
        w.Write((ushort)bits);

        if (extraChunkBeforeData)
        {
            w.Write(Encoding.ASCII.GetBytes("LIST"));
            w.Write(3); // odd length, so a pad byte follows
            w.Write(new byte[] { 1, 2, 3, 0 });
        }

        w.Write(Encoding.ASCII.GetBytes("data"));
        w.Write(data.Length);
        w.Write(data);

        w.Flush();
        var bytes = stream.ToArray();
        BitConverter.GetBytes(bytes.Length - 8).CopyTo(bytes, 4);
        return bytes;
    }
}
