namespace Engine.Rendering3D;

/// <summary>GLSL for lit meshes: metallic-roughness (PBR) shading with one sun and a handful of point lights, normal maps, alpha modes and instancing.</summary>
internal static class MeshShaderSource
{
    /// <summary>Most point lights one draw can see; the rest are dropped (the nearest-first choice is made on the CPU).</summary>
    public const int MaxPointLights = 8;

    /// <summary>Most joints one skinned mesh can have: the joint matrices are a uniform array of this size.</summary>
    public const int MaxJoints = 64;

    /// <summary>
    /// Transforms to world space (for lighting) and clip space. The model matrix and the normal matrix are per-instance vertex attributes
    /// (locations 4-7 and 8-10), so a single draw is just an instanced draw of one. Row-vector matrices written row by row arrive as the
    /// transposed matrix GL expects, hence <c>matrix * vector</c>.
    /// </summary>
    public const string Vertex = """
        #version 330 core
        layout(location = 0) in vec3 aPosition;
        layout(location = 1) in vec3 aNormal;
        layout(location = 2) in vec2 aUv;
        layout(location = 3) in vec4 aTangent;
        layout(location = 4) in vec4 aModel0;
        layout(location = 5) in vec4 aModel1;
        layout(location = 6) in vec4 aModel2;
        layout(location = 7) in vec4 aModel3;
        layout(location = 8) in vec3 aNormalMatrix0;
        layout(location = 9) in vec3 aNormalMatrix1;
        layout(location = 10) in vec3 aNormalMatrix2;
        layout(location = 11) in vec4 aJoints;
        layout(location = 12) in vec4 aWeights;

        uniform mat4 uViewProjection;
        uniform mat4 uLightViewProjection;
        uniform int uSkinned;
        uniform mat4 uJoints[64];

        out vec3 vWorldPosition;
        out vec3 vNormal;
        out vec2 vUv;
        out vec3 vTangent;
        out float vTangentSign;
        out vec4 vLightSpace;

        void main()
        {
            mat4 model = mat4(aModel0, aModel1, aModel2, aModel3);
            mat3 normalMatrix = mat3(aNormalMatrix0, aNormalMatrix1, aNormalMatrix2);

            // A skinned vertex is first moved by a weighted blend of its joints' matrices (still in model space), then placed in the world like any other.
            vec4 position = vec4(aPosition, 1.0);
            vec3 normal = aNormal;
            vec3 tangent = aTangent.xyz;
            if (uSkinned != 0)
            {
                mat4 skin = aWeights.x * uJoints[int(aJoints.x)] + aWeights.y * uJoints[int(aJoints.y)]
                          + aWeights.z * uJoints[int(aJoints.z)] + aWeights.w * uJoints[int(aJoints.w)];
                position = skin * position;
                normal = mat3(skin) * normal;
                tangent = mat3(skin) * tangent;
            }

            vec4 world = model * position;
            vWorldPosition = world.xyz;
            vNormal = normalMatrix * normal;
            vTangent = mat3(model) * tangent;
            vTangentSign = aTangent.w;
            vLightSpace = uLightViewProjection * world;
            vUv = aUv;
            gl_Position = uViewProjection * world;
        }
        """;

    /// <summary>
    /// Works in linear light: the colour textures and factors are decoded from sRGB, lit, then encoded back, so adding two lights doubles
    /// the energy instead of brightening a gamma-encoded value. The specular term is the Cook-Torrance microfacet model (GGX distribution,
    /// Smith-Schlick geometry, Schlick Fresnel); diffuse is Lambert, scaled down for metals, which have none. Lights are plain colour times
    /// intensity rather than physical radiance, so the usual 1/pi of the diffuse term is folded into them (diffuse is albedo times light,
    /// and the specular term is multiplied by pi to match), which keeps a light's apparent brightness the same as with simple shading.
    /// </summary>
    public const string Fragment = """
        #version 330 core
        #define MAX_POINT_LIGHTS 8
        #define PI 3.14159265359

        in vec3 vWorldPosition;
        in vec3 vNormal;
        in vec2 vUv;
        in vec3 vTangent;
        in float vTangentSign;
        in vec4 vLightSpace;

        uniform sampler2D uBaseTexture;
        uniform sampler2D uMetallicRoughnessTexture;
        uniform sampler2D uNormalTexture;
        uniform sampler2DShadow uShadowMap;
        uniform int uShadowsEnabled;
        uniform float uShadowBias;
        uniform vec4 uBaseColor;
        uniform float uMetallic;
        uniform float uRoughness;
        uniform float uNormalScale;
        uniform vec3 uEmissive;
        uniform int uHasMetallicRoughness;
        uniform int uHasNormal;
        uniform int uAlphaMode;       // 0 opaque, 1 mask, 2 blend
        uniform float uAlphaCutoff;
        uniform int uUnlit;
        uniform vec3 uCameraPosition;
        uniform vec3 uAmbient;
        uniform vec3 uSkyColor;
        uniform vec3 uGroundColor;
        uniform float uReflectionStrength;
        uniform vec3 uSunDirection;   // direction the light travels
        uniform vec3 uSunColor;       // already multiplied by intensity
        uniform int uPointCount;
        uniform vec3 uPointPosition[MAX_POINT_LIGHTS];
        uniform vec3 uPointColor[MAX_POINT_LIGHTS];   // already multiplied by intensity
        uniform float uPointRadius[MAX_POINT_LIGHTS];

        out vec4 fragColor;

        vec3 toLinear(vec3 c) { return pow(c, vec3(2.2)); }

        // GGX / Trowbridge-Reitz normal distribution: how many microfacets face the half vector.
        float distributionGgx(float nDotH, float alpha)
        {
            float a2 = alpha * alpha;
            float d = nDotH * nDotH * (a2 - 1.0) + 1.0;
            return a2 / (PI * d * d);
        }

        // Smith geometry with Schlick-GGX: how much of the surface hides itself from the light or the eye.
        float geometrySchlick(float nDotX, float roughness)
        {
            float k = (roughness + 1.0) * (roughness + 1.0) / 8.0;
            return nDotX / (nDotX * (1.0 - k) + k);
        }

        // One light arriving from toLight (unit, surface -> light): diffuse plus specular, times the cosine at the surface.
        vec3 shade(vec3 toLight, vec3 lightColor, vec3 normal, vec3 toCamera, vec3 albedo, float metallic, float roughness, vec3 f0)
        {
            float nDotL = max(dot(normal, toLight), 0.0);
            if (nDotL <= 0.0) return vec3(0.0);

            vec3 halfway = normalize(toLight + toCamera);
            float nDotV = max(dot(normal, toCamera), 1e-4);
            float nDotH = max(dot(normal, halfway), 0.0);
            float vDotH = max(dot(toCamera, halfway), 0.0);
            float alpha = roughness * roughness;

            float d = distributionGgx(nDotH, alpha);
            float g = geometrySchlick(nDotL, roughness) * geometrySchlick(nDotV, roughness);
            vec3 fresnel = f0 + (1.0 - f0) * pow(1.0 - vDotH, 5.0);

            vec3 specular = d * g * fresnel / (4.0 * nDotV * nDotL + 1e-4) * PI;
            vec3 diffuse = (1.0 - fresnel) * (1.0 - metallic) * albedo;
            return (diffuse + specular) * lightColor * nDotL;
        }

        // How much of the sun reaches this point, 0 (shadowed) to 1: the pixel is moved into the sun's view and its depth compared with what the
        // sun saw there. A 3x3 grid of comparisons (each already bilinear in hardware) softens the edge; the bias grows on surfaces that slope
        // away from the sun, where depth changes quickly across a texel and would otherwise shadow themselves ("acne").
        float sunVisibility(float nDotL)
        {
            if (uShadowsEnabled == 0) return 1.0;
            vec3 projected = vLightSpace.xyz / vLightSpace.w * 0.5 + 0.5;
            if (projected.z > 1.0) return 1.0;

            float bias = uShadowBias * (1.0 + 3.0 * (1.0 - nDotL));
            vec2 texel = 1.0 / vec2(textureSize(uShadowMap, 0));
            float lit = 0.0;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                    lit += texture(uShadowMap, vec3(projected.xy + vec2(x, y) * texel, projected.z - bias));
            }

            return lit / 9.0;
        }

        void main()
        {
            vec4 texel = texture(uBaseTexture, vUv);
            vec4 base = vec4(toLinear(texel.rgb * uBaseColor.rgb), texel.a * uBaseColor.a);
            if (uAlphaMode == 1 && base.a < uAlphaCutoff) discard;

            vec3 emissive = toLinear(uEmissive);
            vec3 color;
            if (uUnlit != 0)
            {
                color = base.rgb + emissive;
            }
            else
            {
                float metallic = uMetallic;
                float roughness = uRoughness;
                if (uHasMetallicRoughness != 0)
                {
                    vec4 mr = texture(uMetallicRoughnessTexture, vUv);
                    roughness *= mr.g;
                    metallic *= mr.b;
                }

                roughness = clamp(roughness, 0.045, 1.0); // a perfect mirror has an infinitely small highlight, which aliases
                metallic = clamp(metallic, 0.0, 1.0);

                vec3 normal = normalize(vNormal);
                vec3 tangent = normalize(vTangent - normal * dot(normal, vTangent));
                vec3 bitangent = cross(normal, tangent) * vTangentSign;
                if (!gl_FrontFacing)
                {
                    // double-sided surfaces light their back too: flip the whole frame
                    normal = -normal;
                    bitangent = -bitangent;
                }

                if (uHasNormal != 0)
                {
                    vec3 sampled = texture(uNormalTexture, vUv).xyz * 2.0 - 1.0;
                    sampled.xy *= uNormalScale;
                    normal = normalize(mat3(tangent, bitangent, normal) * sampled);
                }

                vec3 albedo = base.rgb;
                vec3 f0 = mix(vec3(0.04), albedo, metallic); // a dielectric reflects 4% at normal incidence; a metal reflects its own colour
                vec3 toCamera = normalize(uCameraPosition - vWorldPosition);

                vec3 ambient = toLinear(uAmbient);
                color = ambient * albedo * (1.0 - metallic);

                // Shiny surfaces reflect the sky (up) and the ground (down); a rough one averages the whole environment instead of mirroring it.
                vec3 sky = toLinear(uSkyColor);
                vec3 ground = toLinear(uGroundColor);
                vec3 reflected = reflect(-toCamera, normal);
                vec3 mirror = mix(ground, sky, clamp(reflected.y * 0.5 + 0.5, 0.0, 1.0));
                vec3 environment = mix(mirror, (sky + ground) * 0.5, roughness);
                float nDotV = max(dot(normal, toCamera), 0.0);
                vec3 environmentFresnel = f0 + (max(vec3(1.0 - roughness), f0) - f0) * pow(1.0 - nDotV, 5.0);
                color += environment * environmentFresnel * uReflectionStrength;
                color += shade(-uSunDirection, uSunColor, normal, toCamera, albedo, metallic, roughness, f0) * sunVisibility(max(dot(normal, -uSunDirection), 0.0));
                for (int i = 0; i < uPointCount; i++)
                {
                    vec3 offset = uPointPosition[i] - vWorldPosition;
                    float distance = length(offset);
                    float t = clamp(distance / uPointRadius[i], 0.0, 1.0);
                    float falloff = (1.0 - t * t) * (1.0 - t * t); // smooth to exactly zero at the radius
                    color += shade(offset / max(distance, 1e-4), uPointColor[i] * falloff, normal, toCamera, albedo, metallic, roughness, f0);
                }

                color += emissive;
            }

            fragColor = vec4(pow(color, vec3(1.0 / 2.2)), uAlphaMode == 2 ? base.a : 1.0);
        }
        """;
}
