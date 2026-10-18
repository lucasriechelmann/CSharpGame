using System.Numerics;

namespace Engine.Rendering3D;

/// <summary>
/// Builds the basic shapes procedurally: handy placeholders and the engine's own test geometry. All faces are counter-clockwise
/// seen from outside and have outward normals, which is what back-face culling and lighting expect.
/// </summary>
public static class MeshBuilder
{
    /// <summary>An axis-aligned box centred on the origin. Each face has its own four vertices so its normal is flat.</summary>
    /// <param name="size">Edge lengths along X, Y and Z.</param>
    public static MeshData Cube(Vector3 size)
    {
        if (size.X <= 0f || size.Y <= 0f || size.Z <= 0f)
            throw new ArgumentOutOfRangeException(nameof(size), $"Cube size must be positive on every axis, got {size}.");

        var half = size * 0.5f;
        var vertices = new Vertex3D[24];
        var indices = new uint[36];

        // For each face: outward normal, then the two in-plane axes (u to the right, v up when seen from outside; u x v = normal).
        (Vector3 Normal, Vector3 U, Vector3 V)[] faces =
        [
            (Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY),
            (-Vector3.UnitZ, -Vector3.UnitX, Vector3.UnitY),
            (Vector3.UnitX, -Vector3.UnitZ, Vector3.UnitY),
            (-Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY),
            (Vector3.UnitY, Vector3.UnitX, -Vector3.UnitZ),
            (-Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ),
        ];

        for (int f = 0; f < faces.Length; f++)
        {
            var (normal, u, v) = faces[f];
            var centre = normal * half;
            var du = u * half;
            var dv = v * half;
            int baseVertex = f * 4;
            vertices[baseVertex + 0] = new Vertex3D(centre - du - dv, normal, new Vector2(0f, 1f));
            vertices[baseVertex + 1] = new Vertex3D(centre + du - dv, normal, new Vector2(1f, 1f));
            vertices[baseVertex + 2] = new Vertex3D(centre + du + dv, normal, new Vector2(1f, 0f));
            vertices[baseVertex + 3] = new Vertex3D(centre - du + dv, normal, new Vector2(0f, 0f));
            WriteQuad(indices, f * 6, (uint)baseVertex);
        }

        return new MeshData(vertices, indices);
    }

    /// <summary>A flat rectangle in the XZ plane facing up (+Y), centred on the origin; the texture repeats <paramref name="uvRepeat"/> times.</summary>
    /// <param name="width">Extent along X.</param>
    /// <param name="depth">Extent along Z.</param>
    /// <param name="uvRepeat">How many times the texture tiles across each side (1 = once).</param>
    public static MeshData Plane(float width, float depth, float uvRepeat = 1f)
    {
        if (width <= 0f || depth <= 0f)
            throw new ArgumentOutOfRangeException(nameof(width), $"Plane size must be positive, got {width} x {depth}.");

        float hx = width * 0.5f;
        float hz = depth * 0.5f;
        var up = Vector3.UnitY;
        var vertices = new[]
        {
            new Vertex3D(new Vector3(-hx, 0f, hz), up, new Vector2(0f, uvRepeat)),
            new Vertex3D(new Vector3(hx, 0f, hz), up, new Vector2(uvRepeat, uvRepeat)),
            new Vertex3D(new Vector3(hx, 0f, -hz), up, new Vector2(uvRepeat, 0f)),
            new Vertex3D(new Vector3(-hx, 0f, -hz), up, new Vector2(0f, 0f)),
        };
        var indices = new uint[6];
        WriteQuad(indices, 0, 0);
        return new MeshData(vertices, indices);
    }

    /// <summary>A UV sphere (latitude/longitude grid) centred on the origin with smooth normals.</summary>
    /// <param name="radius">Distance from the centre to the surface.</param>
    /// <param name="segments">Slices around the Y axis (at least 3).</param>
    /// <param name="rings">Bands from pole to pole (at least 2).</param>
    public static MeshData Sphere(float radius, int segments = 32, int rings = 16)
    {
        if (radius <= 0f) throw new ArgumentOutOfRangeException(nameof(radius), $"Sphere radius must be positive, got {radius}.");
        if (segments < 3) throw new ArgumentOutOfRangeException(nameof(segments), $"A sphere needs at least 3 segments, got {segments}.");
        if (rings < 2) throw new ArgumentOutOfRangeException(nameof(rings), $"A sphere needs at least 2 rings, got {rings}.");

        // One extra column so the seam vertex can have u = 1 as well as u = 0.
        int columns = segments + 1;
        var vertices = new Vertex3D[columns * (rings + 1)];
        for (int ring = 0; ring <= rings; ring++)
        {
            float v = ring / (float)rings;
            float polar = v * MathF.PI; // 0 at the top pole, pi at the bottom
            float y = MathF.Cos(polar);
            float ringRadius = MathF.Sin(polar);
            for (int segment = 0; segment <= segments; segment++)
            {
                float u = segment / (float)segments;
                float azimuth = u * MathF.Tau;
                var normal = new Vector3(ringRadius * MathF.Cos(azimuth), y, -ringRadius * MathF.Sin(azimuth));
                vertices[ring * columns + segment] = new Vertex3D(normal * radius, normal, new Vector2(u, v));
            }
        }

        var indices = new List<uint>(segments * rings * 6);
        for (int ring = 0; ring < rings; ring++)
        {
            for (int segment = 0; segment < segments; segment++)
            {
                uint a = (uint)(ring * columns + segment); // upper left
                uint b = a + 1;                            // upper right
                uint c = a + (uint)columns;                // lower left
                uint d = c + 1;                            // lower right
                // Counter-clockwise seen from outside; the quads touching a pole collapse to one triangle.
                if (ring != 0) { indices.Add(a); indices.Add(c); indices.Add(b); }
                if (ring != rings - 1) { indices.Add(b); indices.Add(c); indices.Add(d); }
            }
        }

        return new MeshData(vertices, [.. indices]);
    }

    /// <summary>Writes the two triangles of a quad whose corners are bottom-left, bottom-right, top-right, top-left (counter-clockwise).</summary>
    private static void WriteQuad(uint[] indices, int at, uint first)
    {
        indices[at + 0] = first;
        indices[at + 1] = first + 1;
        indices[at + 2] = first + 2;
        indices[at + 3] = first + 2;
        indices[at + 4] = first + 3;
        indices[at + 5] = first;
    }
}
