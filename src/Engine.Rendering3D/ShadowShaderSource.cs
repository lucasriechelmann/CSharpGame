namespace Engine.Rendering3D;

/// <summary>GLSL for the depth-only pass that fills the shadow map: transform by the sun's matrix (skinned meshes included), and discard cut-out pixels so a leaf casts a leaf-shaped shadow.</summary>
internal static class ShadowShaderSource
{
    /// <summary>Reads the same per-instance model matrix attributes as the main shader (locations 4-7) and the same joint data (11, 12), so the same instance buffer and skeleton serve both passes.</summary>
    public const string Vertex = """
        #version 330 core
        layout(location = 0) in vec3 aPosition;
        layout(location = 2) in vec2 aUv;
        layout(location = 4) in vec4 aModel0;
        layout(location = 5) in vec4 aModel1;
        layout(location = 6) in vec4 aModel2;
        layout(location = 7) in vec4 aModel3;
        layout(location = 11) in vec4 aJoints;
        layout(location = 12) in vec4 aWeights;

        uniform mat4 uLightViewProjection;
        uniform int uSkinned;
        uniform mat4 uJoints[64];

        out vec2 vUv;

        void main()
        {
            mat4 model = mat4(aModel0, aModel1, aModel2, aModel3);
            vec4 position = vec4(aPosition, 1.0);
            if (uSkinned != 0)
            {
                mat4 skin = aWeights.x * uJoints[int(aJoints.x)] + aWeights.y * uJoints[int(aJoints.y)]
                          + aWeights.z * uJoints[int(aJoints.z)] + aWeights.w * uJoints[int(aJoints.w)];
                position = skin * position;
            }

            vUv = aUv;
            gl_Position = uLightViewProjection * model * position;
        }
        """;

    /// <summary>Writes no colour; only the depth the rasteriser stores matters. Cut-out materials discard pixels below their alpha cut-off.</summary>
    public const string Fragment = """
        #version 330 core
        in vec2 vUv;

        uniform sampler2D uBaseTexture;
        uniform vec4 uBaseColor;
        uniform int uAlphaMode;
        uniform float uAlphaCutoff;

        void main()
        {
            if (uAlphaMode == 1 && texture(uBaseTexture, vUv).a * uBaseColor.a < uAlphaCutoff) discard;
        }
        """;
}
