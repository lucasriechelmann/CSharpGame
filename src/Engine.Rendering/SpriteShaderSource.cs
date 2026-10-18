namespace Engine.Rendering;

/// <summary>GLSL source for the built-in sprite shader: transforms by the view-projection and multiplies texture by vertex tint.</summary>
internal static class SpriteShaderSource
{
    /// <summary>Vertex stage. Attribute locations must match <see cref="SpriteVertex"/>'s layout.</summary>
    public const string Vertex = """
        #version 330 core
        layout(location = 0) in vec3 aPosition;
        layout(location = 1) in vec2 aTexCoord;
        layout(location = 2) in vec4 aColor;

        uniform mat4 uViewProjection;

        out vec2 vTexCoord;
        out vec4 vColor;

        void main()
        {
            vTexCoord = aTexCoord;
            vColor = aColor;
            gl_Position = uViewProjection * vec4(aPosition, 1.0);
        }
        """;

    /// <summary>Fragment stage.</summary>
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
