namespace Engine.Rendering3D;

/// <summary>How a material's alpha (from its base colour and texture) is used. The same three modes as glTF.</summary>
public enum AlphaMode
{
    /// <summary>Alpha is ignored: the surface is solid. Drawn first, with depth writes, and casts shadows.</summary>
    Opaque,

    /// <summary>Alpha is a cut-out: pixels below <see cref="Material.AlphaCutoff"/> are discarded, the rest are solid (leaves, fences). Still drawn with the opaque surfaces.</summary>
    Mask,

    /// <summary>Alpha blends the surface with what is behind it (glass, smoke). Drawn after everything opaque, back to front, without writing depth, and casts no shadow.</summary>
    Blend,
}
