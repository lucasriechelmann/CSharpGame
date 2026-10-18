namespace Engine.Tilemaps;

/// <summary>
/// The shader for cached chunk meshes: the sprite shader plus two uniforms, so one cached mesh can be placed anywhere (<c>uOffset</c>, the layer's origin
/// with parallax and pixel snapping applied) and faded or tinted (<c>uTint</c>) without being rebuilt.
/// </summary>
internal static class TileMeshShaderSource
{
    public const string Vertex = """
        #version 330 core
        layout(location = 0) in vec3 aPosition;
        layout(location = 1) in vec2 aTexCoord;
        layout(location = 2) in vec4 aColor;

        uniform mat4 uViewProjection;
        uniform vec3 uOffset;
        uniform vec4 uTint;

        out vec2 vTexCoord;
        out vec4 vColor;

        void main()
        {
            vTexCoord = aTexCoord;
            vColor = aColor * uTint;
            gl_Position = uViewProjection * vec4(aPosition + uOffset, 1.0);
        }
        """;

    public const string Fragment = """
        #version 330 core
        in vec2 vTexCoord;
        in vec4 vColor;

        uniform sampler2D uTexture;

        out vec4 FragColor;

        void main()
        {
            FragColor = texture(uTexture, vTexCoord) * vColor;
        }
        """;
}
