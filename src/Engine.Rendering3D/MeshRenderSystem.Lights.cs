using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;

namespace Engine.Rendering3D;

// Light gathering and upload: the sun and every point light are collected once per frame, then each mesh gets the few that reach it.
public sealed partial class MeshRenderSystem
{
    private readonly string[] _pointPositionNames = new string[MeshShaderSource.MaxPointLights];
    private readonly string[] _pointColorNames = new string[MeshShaderSource.MaxPointLights];
    private readonly string[] _pointRadiusNames = new string[MeshShaderSource.MaxPointLights];

    private Vector3[] _pointPositions = new Vector3[16];
    private Vector3[] _pointColors = new Vector3[16];
    private float[] _pointRadii = new float[16];
    private int _pointCount;
    private Vector3 _sunDirection = -Vector3.UnitY;
    private Vector3 _sunColor;

    // Uniform array elements are addressed by name ("uPointPosition[3]"); build the strings once so drawing allocates nothing.
    private void InitialiseLights()
    {
        for (int i = 0; i < MeshShaderSource.MaxPointLights; i++)
        {
            _pointPositionNames[i] = $"uPointPosition[{i}]";
            _pointColorNames[i] = $"uPointColor[{i}]";
            _pointRadiusNames[i] = $"uPointRadius[{i}]";
        }
    }

    private void GatherLights(World world)
    {
        _pointCount = 0;
        _sunColor = Vector3.Zero;
        _sunDirection = -Vector3.UnitY;
        world.Query(_collectSun);
        world.Query(_collectPoint);
    }

    private void CollectSun(Entity entity, ref Transform transform, ref DirectionalLight light)
    {
        if (_sunColor != Vector3.Zero || light.Intensity <= 0f) return; // only the first usable one counts

        _sunColor = new Vector3(light.Color.R, light.Color.G, light.Color.B) * light.Intensity;
        _sunDirection = Vector3.Transform(-Vector3.UnitZ, transform.Rotation);
    }

    private void CollectPoint(Entity entity, ref Transform transform, ref PointLight3D light)
    {
        if (light.Intensity <= 0f || light.Radius <= 0f) return;

        if (_pointCount == _pointPositions.Length)
        {
            Array.Resize(ref _pointPositions, _pointCount * 2);
            Array.Resize(ref _pointColors, _pointCount * 2);
            Array.Resize(ref _pointRadii, _pointCount * 2);
        }

        _pointPositions[_pointCount] = transform.Position;
        _pointColors[_pointCount] = new Vector3(light.Color.R, light.Color.G, light.Color.B) * light.Intensity;
        _pointRadii[_pointCount] = light.Radius;
        _pointCount++;
    }

    private void UploadSceneLights()
    {
        var ambient = Lighting.Ambient;
        _shader.SetUniform("uAmbient", new Vector3(ambient.R, ambient.G, ambient.B));
        _shader.SetUniform("uSunDirection", _sunDirection);
        _shader.SetUniform("uSunColor", _sunColor);
    }

    private void UploadPointLights(Vector3 centre, float radius)
    {
        int count = PointLightSelector.Select(_pointPositions.AsSpan(0, _pointCount), _pointRadii.AsSpan(0, _pointCount), centre, radius, _chosen);
        _shader.SetUniform("uPointCount", count);
        for (int i = 0; i < count; i++)
        {
            int light = _chosen[i];
            _shader.SetUniform(_pointPositionNames[i], _pointPositions[light]);
            _shader.SetUniform(_pointColorNames[i], _pointColors[light]);
            _shader.SetUniform(_pointRadiusNames[i], _pointRadii[light]);
        }
    }
}
