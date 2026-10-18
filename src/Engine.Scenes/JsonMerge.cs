using System.Text.Json.Nodes;

namespace Engine.Scenes;

/// <summary>
/// Applies one JSON object on top of another, the way prefab overrides work (RFC 7386 "merge patch"): objects merge
/// recursively, anything else replaces, arrays replace as a whole, and <c>null</c> removes the property.
/// </summary>
public static class JsonMerge
{
    /// <summary>Merges <paramref name="patch"/> into <paramref name="target"/> in place. The patch is not modified or shared.</summary>
    public static void Apply(JsonObject target, JsonObject patch)
    {
        foreach (var (key, value) in patch)
        {
            if (value is null)
            {
                target.Remove(key);
            }
            else if (value is JsonObject patchChild && target[key] is JsonObject targetChild)
            {
                Apply(targetChild, patchChild);
            }
            else if (value is JsonObject onlyInPatch)
            {
                // No object to merge into: copy, but still honour nulls inside it.
                var copy = new JsonObject();
                Apply(copy, onlyInPatch);
                target[key] = copy;
            }
            else
            {
                target[key] = value.DeepClone();
            }
        }
    }
}
