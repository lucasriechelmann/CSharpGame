#:project ../src/Engine.Rendering3D/Engine.Rendering3D.csproj

// Regenerates the 3D sample's model: a small house made from the engine's own MeshBuilder shapes plus a hand-built roof, saved as a
// standard binary glTF so the model loader is exercised on a real file.
//
//   dotnet run tools/make-models.cs -- samples/Scene3D/Assets/models/house.glb

using System.Numerics;
using Engine.Rendering;
using Engine.Rendering3D;
using Engine.Rendering3D.Gltf;

if (args.Length != 1)
{
    Console.Error.WriteLine("usage: dotnet run tools/make-models.cs -- <output.glb>");
    return 1;
}

static Matrix4x4 At(float x, float y, float z) => Matrix4x4.CreateTranslation(x, y, z);

// A square pyramid, base half-size 1 on the XZ plane, apex at height h; every face flat shaded.
static MeshData Pyramid(float height)
{
    Vector3 a = new(-1f, 0f, -1f), b = new(1f, 0f, -1f), c = new(1f, 0f, 1f), d = new(-1f, 0f, 1f), apex = new(0f, height, 0f);
    var vertices = new List<Vertex3D>();
    var indices = new List<uint>();
    void Triangle(Vector3 p, Vector3 q, Vector3 r)
    {
        var normal = Vector3.Normalize(Vector3.Cross(q - p, r - p));
        foreach (var point in new[] { p, q, r })
        {
            indices.Add((uint)vertices.Count);
            vertices.Add(new Vertex3D(point, normal, Vector2.Zero));
        }
    }

    Triangle(d, c, apex); // front
    Triangle(c, b, apex); // right
    Triangle(b, a, apex); // back
    Triangle(a, d, apex); // left
    Triangle(a, b, c);    // underside
    Triangle(a, c, d);
    return new MeshData([.. vertices], [.. indices]);
}

var parts = new List<GltfExportPart>
{
    new("walls", MeshBuilder.Cube(new Vector3(3f, 2f, 3f)), new Color(0.93f, 0.86f, 0.7f), At(0f, 1f, 0f)),
    new("roof", Pyramid(1.3f), new Color(0.62f, 0.2f, 0.15f), Matrix4x4.CreateScale(1.15f, 1f, 1.15f) * At(0f, 2f, 0f)),
    new("chimney", MeshBuilder.Cube(new Vector3(0.4f, 1f, 0.4f)), new Color(0.45f, 0.45f, 0.48f), At(0.7f, 2.9f, -0.6f)),
    new("door", MeshBuilder.Cube(new Vector3(0.7f, 1.2f, 0.1f)), new Color(0.35f, 0.2f, 0.1f), At(0f, 0.6f, 1.5f)),
    new("window left", MeshBuilder.Cube(new Vector3(0.6f, 0.6f, 0.1f)), new Color(0.65f, 0.85f, 1f), At(-0.95f, 1.2f, 1.5f)),
    new("window right", MeshBuilder.Cube(new Vector3(0.6f, 0.6f, 0.1f)), new Color(0.65f, 0.85f, 1f), At(0.95f, 1.2f, 1.5f)),
};

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0]))!);
File.WriteAllBytes(args[0], GltfWriter.WriteGlb(parts));
Console.WriteLine($"Wrote {args[0]} ({parts.Count} parts)");
return 0;
