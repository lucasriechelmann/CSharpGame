using Engine.Rendering;
using Silk.NET.OpenGL;

namespace Engine.Tilemaps;

/// <summary>
/// Remembers the built mesh of every chunk that has been drawn, so a chunk's quads are computed and uploaded once, not every frame. A cached chunk is
/// rebuilt when its tiles changed (<see cref="TileLayer.TryGetChunk"/>'s version), when the tile set changed how tiles look, or when the atlas texture
/// changed size; chunks not drawn for a while are freed. Owns the GPU meshes.
/// </summary>
internal sealed class TileMeshCache(GL gl) : IDisposable
{
    private const int PruneEveryFrames = 120;
    private const int KeepForFrames = 240;

    private readonly Dictionary<(TileLayer Layer, long Chunk), Entry> _entries = new();
    private readonly List<SpriteVertex> _vertices = new();
    private readonly List<(TileLayer Layer, long Chunk)> _stale = new();
    private long _frame;

    /// <summary>One cached chunk: its mesh (null when it has no static tile), the animated tiles to draw separately, and what it was built from.</summary>
    internal sealed class Entry
    {
        public TileChunkMesh? Mesh;
        public readonly List<AnimatedCell> Animated = new();
        public int Version = -1;
        public TileSet? TileSet;
        public int TileSetVersion;
        public int TextureWidth;
        public int TextureHeight;
        public long LastUsed;
    }

    /// <summary>How many chunks are cached right now.</summary>
    public int Count => _entries.Count;

    /// <summary>Call once per frame: advances the clock chunks are aged by and frees the ones nobody has drawn for a while.</summary>
    public void BeginFrame()
    {
        _frame++;
        if (_frame % PruneEveryFrames != 0) return;

        _stale.Clear();
        foreach (var (key, entry) in _entries)
        {
            if (_frame - entry.LastUsed > KeepForFrames) _stale.Add(key);
        }

        foreach (var key in _stale)
        {
            _entries[key].Mesh?.Dispose();
            _entries.Remove(key);
        }
    }

    /// <summary>Returns the up-to-date entry for a chunk, building it when it is new or out of date.</summary>
    /// <param name="layer">The tile layer.</param>
    /// <param name="chunkX">Chunk column.</param>
    /// <param name="chunkY">Chunk row.</param>
    /// <param name="tiles">The chunk's tiles.</param>
    /// <param name="version">The chunk's change counter.</param>
    /// <param name="tileSet">The layer's tile set.</param>
    /// <param name="texture">The tile set's atlas texture.</param>
    public Entry Get(TileLayer layer, int chunkX, int chunkY, ReadOnlySpan<int> tiles, int version, TileSet tileSet, Texture2D texture)
    {
        var key = (layer, ((long)chunkX << 32) | (uint)chunkY);
        if (!_entries.TryGetValue(key, out var entry))
        {
            entry = new Entry();
            _entries[key] = entry;
        }

        entry.LastUsed = _frame;
        bool current = entry.Version == version && ReferenceEquals(entry.TileSet, tileSet) && entry.TileSetVersion == tileSet.Version
            && entry.TextureWidth == texture.Width && entry.TextureHeight == texture.Height;
        if (current) return entry;

        int quads = TileChunkMeshBuilder.Build(tiles, chunkX, chunkY, layer.TileSize, tileSet, texture.Width, texture.Height, _vertices, entry.Animated);
        entry.Mesh?.Dispose();
        entry.Mesh = quads > 0 ? new TileChunkMesh(gl, _vertices) : null;
        entry.Version = version;
        entry.TileSet = tileSet;
        entry.TileSetVersion = tileSet.Version;
        entry.TextureWidth = texture.Width;
        entry.TextureHeight = texture.Height;
        return entry;
    }

    /// <summary>Frees every cached mesh.</summary>
    public void Dispose()
    {
        foreach (var entry in _entries.Values) entry.Mesh?.Dispose();
        _entries.Clear();
    }
}
