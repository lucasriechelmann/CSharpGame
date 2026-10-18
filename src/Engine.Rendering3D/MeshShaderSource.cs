namespace Engine.Rendering3D;

/// <summary>GLSL for lit meshes (Blinn-Phong with one sun and a handful of point lights).</summary>
internal static class MeshShaderSource
{
    /// <summary>Most point lights one draw can see; the rest are dropped (the nearest-first choice is made on the CPU).</summary>
    public const int MaxPointLights = 8;

    /// <summary>Transforms to world space (for lighting) and clip space; row-vector matrices arrive transposed by GL, hence matrix * vector.</summary>
    public const string Vertex = """
        #version 330 core
        layout(location = 0) in vec3 aPosition;
        layout(location = 1) in vec3 aNormal;
        layout(location = 2) in vec2 aUv;

        uniform mat4 uModel;
        uniform mat4 uNormalMatrix;
        uniform mat4 uViewProjection;

        out vec3 vWorldPosition;
        out vec3 vNormal;
        out vec2 vUv;

        void main()
        {
            vec4 world = uModel * vec4(aPosition, 1.0);
            vWorldPosition = world.xyz;
            vNormal = mat3(uNormalMatrix) * aNormal;
            vUv = aUv;
            gl_Position = uViewProjection * world;
        }
        """;

    /// <summary>
    /// Works in linear light: the colour and texture are decoded from sRGB, lit, then encoded back, so adding two lights
    /// doubles the energy instead of brightening a gamma-encoded value.
    /// </summary>
    public const string Fragment = """
        #version 330 core
        #define MAX_POINT_LIGHTS 8

        in vec3 vWorldPosition;
        in vec3 vNormal;
        in vec2 vUv;

        uniform sampler2D uTexture;
        uniform vec4 uBaseColor;
        uniform float uShininess;
        uniform float uSpecular;
        uniform int uUnlit;

        uniform vec3 uCameraPosition;
        uniform vec3 uAmbient;
        uniform vec3 uSunDirection;   // direction the light travels
        uniform vec3 uSunColor;       // already multiplied by intensity
        uniform int uPointCount;
        uniform vec3 uPointPosition[MAX_POINT_LIGHTS];
        uniform vec3 uPointColor[MAX_POINT_LIGHTS];   // already multiplied by intensity
        uniform float uPointRadius[MAX_POINT_LIGHTS];

        out vec4 fragColor;

        vec3 toLinear(vec3 c) { return pow(c, vec3(2.2)); }

        // Diffuse + Blinn-Phong specular for light arriving from toLight (unit, surface -> light).
        vec3 shade(vec3 toLight, vec3 lightColor, vec3 normal, vec3 toCamera, vec3 albedo)
        {
            float diffuse = max(dot(normal, toLight), 0.0);
            vec3 halfway = normalize(toLight + toCamera);
            float specular = diffuse > 0.0 ? pow(max(dot(normal, halfway), 0.0), uShininess) * uSpecular : 0.0;
            return (albedo * diffuse + vec3(specular)) * lightColor;
        }

        void main()
        {
            vec4 texel = texture(uTexture, vUv);
            vec3 albedo = toLinear(texel.rgb * uBaseColor.rgb);
            vec3 color;

            if (uUnlit != 0)
            {
                color = albedo;
            }
            else
            {
                vec3 normal = normalize(vNormal);
                if (!gl_FrontFacing) normal = -normal; // double-sided surfaces light their back too
                vec3 toCamera = normalize(uCameraPosition - vWorldPosition);

                color = albedo * toLinear(uAmbient);
                color += shade(-uSunDirection, uSunColor, normal, toCamera, albedo);

                for (int i = 0; i < uPointCount; i++)
                {
                    vec3 offset = uPointPosition[i] - vWorldPosition;
                    float distance = length(offset);
                    float t = clamp(distance / uPointRadius[i], 0.0, 1.0);
                    float falloff = (1.0 - t * t) * (1.0 - t * t); // smooth to exactly zero at the radius
                    color += shade(offset / max(distance, 1e-4), uPointColor[i] * falloff, normal, toCamera, albedo);
                }
            }

            fragColor = vec4(pow(color, vec3(1.0 / 2.2)), 1.0);
        }
        """;
}
