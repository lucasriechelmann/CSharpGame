#:property PublishAot=false
#:property JsonSerializerIsReflectionEnabledByDefault=true

// Regenerates the 3D sample's animated, skinned model: a two-bone arm (a square prism bent at an elbow) with a "wave" animation, saved as a
// standard binary glTF (skin, joints, JOINTS_0/WEIGHTS_0 and keyframed rotations) so the skinning and animation code is exercised on a real file.
//
//   dotnet run tools/make-arm.cs -- samples/Scene3D/Assets/models/arm.glb

using System.Numerics;
using System.Text;
using System.Text.Json;

if (args.Length != 1)
{
    Console.Error.WriteLine("usage: dotnet run tools/make-arm.cs -- <output.glb>");
    return 1;
}

// ---- the mesh: a 0.3 x 0.3 prism from y = 0 to y = 2, with extra rings around the elbow at y = 1 so it bends smoothly ----
float[] ringHeights = [0f, 0.5f, 0.8f, 1f, 1.2f, 1.5f, 2f];
float[] elbowWeight = [0f, 0f, 0.25f, 0.5f, 0.75f, 1f, 1f]; // how much the ring follows the elbow joint (the rest follows the shoulder)
const float half = 0.15f;
Vector2[] corners = [new(-half, -half), new(half, -half), new(half, half), new(-half, half)];

var positions = new List<float>();
var normals = new List<float>();
var uvs = new List<float>();
var joints = new List<byte>();
var weights = new List<float>();
for (int ring = 0; ring < ringHeights.Length; ring++)
{
    for (int corner = 0; corner < 4; corner++)
    {
        var c = corners[corner];
        var normal = Vector3.Normalize(new Vector3(c.X, 0f, c.Y)); // pointing out of the corner: a soft, rounded shading
        positions.AddRange([c.X, ringHeights[ring], c.Y]);
        normals.AddRange([normal.X, normal.Y, normal.Z]);
        uvs.AddRange([corner / 4f, ringHeights[ring] / 2f]);
        joints.AddRange([0, 1, 0, 0]);
        weights.AddRange([1f - elbowWeight[ring], elbowWeight[ring], 0f, 0f]);
    }
}

var indices = new List<ushort>();
for (int ring = 0; ring < ringHeights.Length - 1; ring++)
{
    for (int corner = 0; corner < 4; corner++)
    {
        ushort a = (ushort)(ring * 4 + corner), b = (ushort)(ring * 4 + (corner + 1) % 4);
        ushort c = (ushort)(a + 4), d = (ushort)(b + 4);
        // Counter-clockwise seen from outside: the corners above run clockwise seen from above (x right, z towards the viewer), so the outward side is a, c, b then b, c, d.
        indices.AddRange([a, c, b, b, c, d]);
    }
}

// ---- the animation: the elbow bends to 80 degrees and back, the shoulder sways ----
static float[] Turn(float degrees)
{
    float half = degrees * MathF.PI / 360f; // half the angle in radians
    return [0f, 0f, MathF.Sin(half), MathF.Cos(half)];
}

float[] times = [0f, 1f, 2f];
var elbowKeys = new List<float>();
foreach (float angle in new[] { 0f, 80f, 0f }) elbowKeys.AddRange(Turn(angle));
var shoulderKeys = new List<float>();
foreach (float angle in new[] { 10f, -25f, 10f }) shoulderKeys.AddRange(Turn(angle));

// ---- one binary buffer holding it all ----
var bin = new MemoryStream();
int Append(byte[] bytes)
{
    while (bin.Length % 4 != 0) bin.WriteByte(0);
    int offset = (int)bin.Length;
    bin.Write(bytes);
    return offset;
}

byte[] Bytes(IEnumerable<float> values) => values.SelectMany(BitConverter.GetBytes).ToArray();

int positionsAt = Append(Bytes(positions));
int normalsAt = Append(Bytes(normals));
int uvsAt = Append(Bytes(uvs));
int jointsAt = Append([.. joints]);
int weightsAt = Append(Bytes(weights));
int indicesAt = Append(indices.SelectMany(BitConverter.GetBytes).ToArray());
int inverseBindAt = Append(Bytes(new float[]
{
    1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1,   // the shoulder: bound at the origin
    1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, -1, 0, 1,  // the elbow: bound one unit up
}));
int timesAt = Append(Bytes(times));
int elbowAt = Append(Bytes(elbowKeys));
int shoulderAt = Append(Bytes(shoulderKeys));

int vertexCount = ringHeights.Length * 4;
var document = new Dictionary<string, object?>
{
    ["asset"] = new { version = "2.0", generator = "CSharpGame tools/make-arm.cs" },
    ["scene"] = 0,
    ["scenes"] = new[] { new { nodes = new[] { 0, 2 } } },
    ["nodes"] = new object[]
    {
        new { name = "shoulder", children = new[] { 1 } },
        new { name = "elbow", translation = new[] { 0f, 1f, 0f } },
        new { name = "arm", mesh = 0, skin = 0 },
    },
    ["meshes"] = new[]
    {
        new { primitives = new[] { new { attributes = new { POSITION = 0, NORMAL = 1, TEXCOORD_0 = 2, JOINTS_0 = 3, WEIGHTS_0 = 4 }, indices = 5, material = 0 } } },
    },
    ["materials"] = new[]
    {
        new { name = "skin", pbrMetallicRoughness = new { baseColorFactor = new[] { 0.85f, 0.55f, 0.35f, 1f }, metallicFactor = 0f, roughnessFactor = 0.55f }, doubleSided = true },
    },
    ["skins"] = new[] { new { joints = new[] { 0, 1 }, inverseBindMatrices = 6 } },
    ["animations"] = new[]
    {
        new
        {
            name = "wave",
            samplers = new[] { new { input = 7, output = 8 }, new { input = 7, output = 9 } },
            channels = new[]
            {
                new { sampler = 0, target = new { node = 1, path = "rotation" } },
                new { sampler = 1, target = new { node = 0, path = "rotation" } },
            },
        },
    },
    ["accessors"] = new object[]
    {
        new { bufferView = 0, componentType = 5126, count = vertexCount, type = "VEC3", min = new[] { -half, 0f, -half }, max = new[] { half, 2f, half } },
        new { bufferView = 1, componentType = 5126, count = vertexCount, type = "VEC3" },
        new { bufferView = 2, componentType = 5126, count = vertexCount, type = "VEC2" },
        new { bufferView = 3, componentType = 5121, count = vertexCount, type = "VEC4" },
        new { bufferView = 4, componentType = 5126, count = vertexCount, type = "VEC4" },
        new { bufferView = 5, componentType = 5123, count = indices.Count, type = "SCALAR" },
        new { bufferView = 6, componentType = 5126, count = 2, type = "MAT4" },
        new { bufferView = 7, componentType = 5126, count = times.Length, type = "SCALAR", min = new[] { 0f }, max = new[] { 2f } },
        new { bufferView = 8, componentType = 5126, count = times.Length, type = "VEC4" },
        new { bufferView = 9, componentType = 5126, count = times.Length, type = "VEC4" },
    },
    ["bufferViews"] = new object[]
    {
        new { buffer = 0, byteOffset = positionsAt, byteLength = positions.Count * 4 },
        new { buffer = 0, byteOffset = normalsAt, byteLength = normals.Count * 4 },
        new { buffer = 0, byteOffset = uvsAt, byteLength = uvs.Count * 4 },
        new { buffer = 0, byteOffset = jointsAt, byteLength = joints.Count },
        new { buffer = 0, byteOffset = weightsAt, byteLength = weights.Count * 4 },
        new { buffer = 0, byteOffset = indicesAt, byteLength = indices.Count * 2 },
        new { buffer = 0, byteOffset = inverseBindAt, byteLength = 128 },
        new { buffer = 0, byteOffset = timesAt, byteLength = times.Length * 4 },
        new { buffer = 0, byteOffset = elbowAt, byteLength = elbowKeys.Count * 4 },
        new { buffer = 0, byteOffset = shoulderAt, byteLength = shoulderKeys.Count * 4 },
    },
    ["buffers"] = new[] { new { byteLength = (int)bin.Length } },
};

// ---- the .glb container: a 12-byte header, a JSON chunk (padded with spaces) and a BIN chunk (padded with zeros), each 4-byte aligned ----
byte[] json = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(document));
int jsonPadded = (json.Length + 3) & ~3;
int binPadded = ((int)bin.Length + 3) & ~3;
using var output = new MemoryStream();
using (var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true))
{
    writer.Write(0x46546C67u); // "glTF"
    writer.Write(2u);
    writer.Write((uint)(12 + 8 + jsonPadded + 8 + binPadded));
    writer.Write((uint)jsonPadded);
    writer.Write(0x4E4F534Au); // "JSON"
    writer.Write(json);
    for (int i = json.Length; i < jsonPadded; i++) writer.Write((byte)' ');
    writer.Write((uint)binPadded);
    writer.Write(0x004E4942u); // "BIN\0"
    writer.Write(bin.ToArray());
    for (int i = (int)bin.Length; i < binPadded; i++) writer.Write((byte)0);
}

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0]))!);
File.WriteAllBytes(args[0], output.ToArray());
Console.WriteLine($"Wrote {args[0]}: {vertexCount} vertices, {indices.Count / 3} triangles, 2 joints, 1 animation, {output.Length} bytes.");
return 0;
