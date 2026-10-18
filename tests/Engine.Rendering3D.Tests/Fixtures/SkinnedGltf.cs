using System.Globalization;
using System.Numerics;
using Engine.Rendering3D;

namespace Engine.Rendering3D.Tests.Fixtures;

/// <summary>
/// A hand-built glTF with a two-joint "arm": node 0 is the root joint at the origin, node 1 the tip joint one unit above it, node 2 carries a
/// skinned mesh of four vertices (two at each joint). Animation 0 lifts the tip joint from y = 1 to y = 2 over one second and turns the root a
/// quarter about Z; animation 1 steps the tip between two positions; animation 2 uses a cubic spline.
/// </summary>
internal static class SkinnedGltf
{
    public static ModelData Import() => GltfBytes.Import(Json());

    public static string Json()
    {
        var data = new List<byte>();
        int Add(byte[] bytes)
        {
            while (data.Count % 4 != 0) data.Add(0);
            int offset = data.Count;
            data.AddRange(bytes);
            return offset;
        }

        int positions = Add(GltfBytes.Floats(0, 0, 0, 1, 0, 0, 0, 1, 0, 1, 1, 0));
        int joints = Add([0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0]);               // 4 vertices x 4 unsigned bytes
        int weights = Add(GltfBytes.Floats(1, 0, 0, 0, 1, 0, 0, 0, 0.5f, 0.5f, 0, 0, 0, 1, 0, 0));
        int indices = Add(GltfBytes.UShorts(0, 1, 2, 1, 3, 2));
        int inverseBind = Add(GltfBytes.Floats(
            1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1,
            1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, -1, 0, 1)); // the tip joint was bound one unit up, so undoing it moves down by one
        int times = Add(GltfBytes.Floats(0, 1));
        int tipPositions = Add(GltfBytes.Floats(0, 1, 0, 0, 2, 0));
        float s = MathF.Sqrt(0.5f);
        int rootRotations = Add(GltfBytes.Floats(0, 0, 0, 1, 0, 0, s, s));                   // identity to a quarter turn about Z
        int stepPositions = Add(GltfBytes.Floats(0, 1, 0, 5, 5, 5));
        int splineValues = Add(GltfBytes.Floats(
            0, 0, 0,  0, 1, 0,  0, 0, 0,    // key 0: in-tangent, value, out-tangent
            0, 0, 0,  0, 2, 0,  0, 0, 0));  // key 1

        string buffer = GltfBytes.DataUri([.. data]);
        return $$"""
            {
              "asset": { "version": "2.0" },
              "scene": 0,
              "scenes": [ { "nodes": [0, 2] } ],
              "nodes": [
                { "name": "root", "children": [1] },
                { "name": "tip", "translation": [0, 1, 0] },
                { "name": "arm", "mesh": 0, "skin": 0 }
              ],
              "meshes": [ { "primitives": [ { "attributes": { "POSITION": 0, "JOINTS_0": 1, "WEIGHTS_0": 2 }, "indices": 3 } ] } ],
              "skins": [ { "joints": [0, 1], "inverseBindMatrices": 4 } ],
              "animations": [
                {
                  "name": "lift",
                  "samplers": [ { "input": 5, "output": 6 }, { "input": 5, "output": 7 } ],
                  "channels": [
                    { "sampler": 0, "target": { "node": 1, "path": "translation" } },
                    { "sampler": 1, "target": { "node": 0, "path": "rotation" } }
                  ]
                },
                {
                  "name": "hop",
                  "samplers": [ { "input": 5, "output": 8, "interpolation": "STEP" } ],
                  "channels": [ { "sampler": 0, "target": { "node": 1, "path": "translation" } } ]
                },
                {
                  "samplers": [ { "input": 5, "output": 9, "interpolation": "CUBICSPLINE" } ],
                  "channels": [ { "sampler": 0, "target": { "node": 1, "path": "translation" } } ]
                }
              ],
              "accessors": [
                { "bufferView": 0, "componentType": 5126, "count": 4, "type": "VEC3" },
                { "bufferView": 1, "componentType": 5121, "count": 4, "type": "VEC4" },
                { "bufferView": 2, "componentType": 5126, "count": 4, "type": "VEC4" },
                { "bufferView": 3, "componentType": 5123, "count": 6, "type": "SCALAR" },
                { "bufferView": 4, "componentType": 5126, "count": 2, "type": "MAT4" },
                { "bufferView": 5, "componentType": 5126, "count": 2, "type": "SCALAR" },
                { "bufferView": 6, "componentType": 5126, "count": 2, "type": "VEC3" },
                { "bufferView": 7, "componentType": 5126, "count": 2, "type": "VEC4" },
                { "bufferView": 8, "componentType": 5126, "count": 2, "type": "VEC3" },
                { "bufferView": 9, "componentType": 5126, "count": 6, "type": "VEC3" }
              ],
              "bufferViews": [
                { "buffer": 0, "byteOffset": {{positions}}, "byteLength": 48 },
                { "buffer": 0, "byteOffset": {{joints}}, "byteLength": 16 },
                { "buffer": 0, "byteOffset": {{weights}}, "byteLength": 64 },
                { "buffer": 0, "byteOffset": {{indices}}, "byteLength": 12 },
                { "buffer": 0, "byteOffset": {{inverseBind}}, "byteLength": 128 },
                { "buffer": 0, "byteOffset": {{times}}, "byteLength": 8 },
                { "buffer": 0, "byteOffset": {{tipPositions}}, "byteLength": 24 },
                { "buffer": 0, "byteOffset": {{rootRotations}}, "byteLength": 32 },
                { "buffer": 0, "byteOffset": {{stepPositions}}, "byteLength": 24 },
                { "buffer": 0, "byteOffset": {{splineValues}}, "byteLength": 72 }
              ],
              "buffers": [ { "byteLength": {{data.Count}}, "uri": "{{buffer}}" } ]
            }
            """;
    }
}
