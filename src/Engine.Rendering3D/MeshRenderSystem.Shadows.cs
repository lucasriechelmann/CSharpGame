using System.Numerics;
using Engine.Rendering;
using Silk.NET.OpenGL;

namespace Engine.Rendering3D;

// The sun's shadow: a depth map drawn from the sun's point of view, sampled by the main shader.
public sealed partial class MeshRenderSystem
{
    /// <summary>The texture unit the main shader reads the shadow map from (0-2 are the material's textures).</summary>
    private const int ShadowTextureUnit = 3;

    private DrawItem[] _casters = new DrawItem[64];
    private int _casterCount;
    private bool _shadowsActive;
    private SunShadowFit _shadowFit;

    /// <summary>Whether the sun's shadow map was drawn in the last frame (shadows are enabled and there is a sun).</summary>
    public bool ShadowsActiveLastFrame => _shadowsActive;

    /// <summary>Decides whether shadows are drawn this frame and, if so, where the sun's view is fitted. Needs the lights gathered first.</summary>
    private void PrepareShadows()
    {
        _casterCount = 0;
        _shadowsActive = Lighting.ShadowsEnabled && _sunColor != Vector3.Zero;
        if (!_shadowsActive) return;

        float distance = MathF.Max(Lighting.ShadowDistance, _camera.Near + 0.1f);
        _shadowFit = ShadowMath.FitToCamera(_camera, _sunDirection, distance, _shadowMap.Resolution);
    }

    /// <summary>
    /// Remembers a mesh as a shadow caster when it is solid (not blended), its material allows it and it could fall inside the sun's view. Casters
    /// are not culled by the camera: a tall thing behind the player can still throw its shadow into the picture.
    /// </summary>
    private void CollectCaster(Mesh mesh, Material material, in Matrix4x4 model, Vector3 centre, float radius, Matrix4x4[]? joints)
    {
        if (!_shadowsActive || !material.CastShadows || material.IsBlended) return;

        // The sun's view is a box of half-size Radius in its own x and y, so a sphere is outside it when it is farther out than that plus itself.
        // A skinned mesh skips the test: its pose moves it away from the bounds measured on the bind pose.
        if (joints == null)
        {
            var clip = Vector4.Transform(new Vector4(centre, 1f), _shadowFit.ViewProjection);
            float margin = radius / _shadowFit.Radius;
            if (MathF.Abs(clip.X) > 1f + margin || MathF.Abs(clip.Y) > 1f + margin) return;
        }

        Push(ref _casters, ref _casterCount, new DrawItem { Mesh = mesh, Material = material, Model = model, Centre = centre, Radius = radius, Joints = joints });
    }

    /// <summary>Draws every caster into the shadow map from the sun's point of view.</summary>
    private void RenderShadowMap()
    {
        if (!_shadowsActive) return;

        _casters.AsSpan(0, _casterCount).Sort(ByMaterialThenMesh);

        _shadowMap.Begin();
        _gl.Enable(EnableCap.DepthTest);
        _gl.DepthFunc(DepthFunction.Less);
        _gl.Disable(EnableCap.Blend);
        _gl.Disable(EnableCap.CullFace); // thin and double-sided casters must shadow from both sides
        _gl.Enable(EnableCap.PolygonOffsetFill); // pushes stored depth slightly away, the first line of defence against self-shadowing
        _gl.PolygonOffset(2f, 4f);

        _shadowShader.Use();
        _shadowShader.SetUniform("uLightViewProjection", _shadowFit.ViewProjection);
        _shadowShader.SetUniform("uBaseTexture", 0);

        Material? bound = null;
        int i = 0;
        while (i < _casterCount)
        {
            int end = i + 1;
            while (_casters[i].Joints == null && end < _casterCount && _casters[end].Joints == null
                   && ReferenceEquals(_casters[end].Mesh, _casters[i].Mesh) && ReferenceEquals(_casters[end].Material, _casters[i].Material))
                end++;

            var material = _casters[i].Material;
            if (!ReferenceEquals(material, bound))
            {
                bound = material;
                (material.OwnedTexture ?? Resolve(material.Texture) ?? _white).Bind(0);
                var color = material.BaseColor;
                _shadowShader.SetUniform("uBaseColor", new Vector4(color.R, color.G, color.B, color.A));
                _shadowShader.SetUniform("uAlphaMode", (int)material.AlphaMode);
                _shadowShader.SetUniform("uAlphaCutoff", material.AlphaCutoff);
            }

            DrawInstances(_casters, i, end, _shadowShader);
            i = end;
        }

        _gl.Disable(EnableCap.PolygonOffsetFill);
        _shadowMap.End();
    }

    /// <summary>Uploads the sun's matrix and settings for the main shader and binds the shadow map; with shadows off the shader is told to skip the lookup.</summary>
    private void UploadShadowUniforms()
    {
        _shader.SetUniform("uShadowsEnabled", _shadowsActive ? 1 : 0);
        _shader.SetUniform("uShadowBias", Lighting.ShadowBias);
        _shader.SetUniform("uLightViewProjection", _shadowsActive ? _shadowFit.ViewProjection : Matrix4x4.Identity);
        _shader.SetUniform("uShadowMap", ShadowTextureUnit);
        _shadowMap.Bind(ShadowTextureUnit);
        _gl.ActiveTexture(TextureUnit.Texture0); // leave unit 0 current: materials bind their textures there first
    }
}
