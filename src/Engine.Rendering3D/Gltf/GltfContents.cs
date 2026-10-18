namespace Engine.Rendering3D.Gltf;

/// <summary>The two parts of a glTF asset once the container is opened: the JSON text and, for a .glb, the embedded binary chunk.</summary>
/// <param name="Json">The glTF JSON document.</param>
/// <param name="BinaryChunk">The BIN chunk of a binary .glb (what a buffer without a uri refers to); null for a text .gltf.</param>
public readonly record struct GltfContents(string Json, byte[]? BinaryChunk);
