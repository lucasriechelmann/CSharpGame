using Engine.Core.Assets;
using Silk.NET.OpenGL;

namespace Engine.Rendering;

/// <summary>Asset loader that reads image files (PNG, JPG, BMP, TGA) into GPU <see cref="Texture2D"/>s.</summary>
/// <param name="gl">The GL context that will own the textures.</param>
/// <param name="filter">Sampling filter applied to every loaded texture.</param>
public sealed class TextureLoader(GL gl, TextureFilter filter = TextureFilter.Nearest) : IAssetLoader<Texture2D>
{
    /// <inheritdoc />
    public Texture2D Load(string fullPath) => Texture2D.FromFile(gl, fullPath, filter);
}
