using Engine.Rendering;
using Silk.NET.OpenGL;

namespace Engine.Rendering3D;

// Drawing the gathered lists: the opaque pass (instanced runs), the blended pass (back to front), and binding a material.
public sealed partial class MeshRenderSystem
{
    private void DrawItems()
    {
        _boundMaterial = null;
        _cullEnabled = false;
        DrawOpaque();
        DrawBlended();
    }

    /// <summary>
    /// Opaque and cut-out surfaces, grouped by material and mesh. A run of the same mesh and material is drawn in one instanced call when every
    /// point light fits in the shader at once (their selection per mesh would otherwise differ between the instances of a run); otherwise each
    /// is drawn on its own with the lights nearest to it.
    /// </summary>
    private void DrawOpaque()
    {
        bool allLightsFit = _pointCount <= MeshShaderSource.MaxPointLights;
        if (allLightsFit) UploadAllPointLights();

        int i = 0;
        while (i < _opaqueCount)
        {
            int end = i + 1;
            while (allLightsFit && _opaque[i].Joints == null && end < _opaqueCount && _opaque[end].Joints == null
                   && ReferenceEquals(_opaque[end].Mesh, _opaque[i].Mesh) && ReferenceEquals(_opaque[end].Material, _opaque[i].Material))
                end++;

            BindMaterial(_opaque[i].Material);
            if (!allLightsFit) UploadPointLights(_opaque[i].Centre, _opaque[i].Radius); // runs are single items here: lights are chosen per mesh
            DrawRun(_opaque, i, end);

            i = end;
        }
    }

    /// <summary>Blended surfaces farthest first, each on its own, with depth testing but no depth writes, so nearer glass shows what is behind it.</summary>
    private void DrawBlended()
    {
        if (_blendedCount == 0) return;

        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.DepthMask(false);
        _boundMaterial = null; // the opaque pass left its last material bound; blended ones must rebind

        for (int i = 0; i < _blendedCount; i++)
        {
            BindMaterial(_blended[i].Material);
            UploadPointLights(_blended[i].Centre, _blended[i].Radius);
            DrawRun(_blended, i, i + 1);
        }

        _gl.DepthMask(true);
        _gl.Disable(EnableCap.Blend);
    }

    /// <summary>Draws items [<paramref name="start"/>, <paramref name="end"/>), which share a mesh, as one instanced call.</summary>
    private void DrawRun(DrawItem[] items, int start, int end)
    {
        DrawInstances(items, start, end, _shader);
        DrawnLastFrame += end - start;
        DrawCallsLastFrame++;
    }

    /// <summary>The instanced call itself, without counting it (the shadow pass draws through this too).</summary>
    private void DrawInstances(DrawItem[] items, int start, int end, Engine.Rendering.Shader shader)
    {
        var mesh = items[start].Mesh;
        var joints = items[start].Joints; // a skinned item is a run of one: each has its own pose
        shader.SetUniform("uSkinned", joints != null ? 1 : 0);
        if (joints != null) shader.SetUniform("uJoints[0]", joints);
        _instances.Clear();
        for (int i = start; i < end; i++) _instances.Add(items[i].Model);

        mesh.BindVertexArray();
        _instances.UploadAndAttach();
        mesh.DrawInstanced(end - start);
    }

    private void BindMaterial(Material material)
    {
        // Consecutive meshes usually share a material, so skip the uniform and texture traffic when nothing changed.
        // (A material edited between two draws in one frame is not a case worth handling.)
        if (ReferenceEquals(material, _boundMaterial)) return;
        _boundMaterial = material;

        var baseTexture = material.OwnedTexture ?? Resolve(material.Texture) ?? _white;
        var metallicRoughness = material.OwnedMetallicRoughnessTexture ?? Resolve(material.MetallicRoughnessTexture);
        var normal = material.OwnedNormalTexture ?? Resolve(material.NormalTexture);
        baseTexture.Bind(0);
        (metallicRoughness ?? _white).Bind(1);
        (normal ?? _white).Bind(2);

        var color = material.BaseColor;
        var emissive = material.Emissive;
        _shader.SetUniform("uBaseColor", new System.Numerics.Vector4(color.R, color.G, color.B, color.A));
        _shader.SetUniform("uMetallic", material.Metallic);
        _shader.SetUniform("uRoughness", material.Roughness);
        _shader.SetUniform("uNormalScale", material.NormalScale);
        _shader.SetUniform("uEmissive", new System.Numerics.Vector3(emissive.R, emissive.G, emissive.B));
        _shader.SetUniform("uHasMetallicRoughness", metallicRoughness != null ? 1 : 0);
        _shader.SetUniform("uHasNormal", normal != null ? 1 : 0);
        _shader.SetUniform("uAlphaMode", (int)material.AlphaMode);
        _shader.SetUniform("uAlphaCutoff", material.AlphaCutoff);
        _shader.SetUniform("uUnlit", material.Unlit ? 1 : 0);

        bool cull = !material.DoubleSided;
        if (cull)
        {
            if (!_cullEnabled) _gl.Enable(EnableCap.CullFace);
            _gl.CullFace(TriangleFace.Back);
        }
        else if (_cullEnabled)
        {
            _gl.Disable(EnableCap.CullFace);
        }

        _cullEnabled = cull;
    }

    /// <summary>The texture behind a handle, or null when the handle is empty or its asset is gone (so the material falls back to a plain colour).</summary>
    private Texture2D? Resolve(Engine.Core.Assets.AssetHandle<Texture2D> handle) =>
        handle.IsValid && _assets.TryGet(handle, out var texture) ? texture : null;
}
