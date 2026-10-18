using System.Text.Json.Nodes;

namespace Engine.Scenes;

/// <summary>
/// Applies one JSON object on top of another, the way prefab overrides work (RFC 7386 "merge patch"): objects merge
/// recursively, anything else replaces, arrays replace as a whole, and <c>null</c> removes the property.
/// </summary>
public static class JsonMerge
{
    /// <summary>
    /// The patch that turns <paramref name="from"/> into <paramref name="to"/> when given to <see cref="Apply"/>: the inverse of
    /// merging. Properties that are equal are left out, changed ones are included (objects only as deep as they actually differ,
    /// arrays and scalars whole), and a property present in <paramref name="from"/> but missing from <paramref name="to"/> becomes
    /// <c>null</c>, which means "remove".
    /// </summary>
    /// <param name="from">The base, such as a prefab's components.</param>
    /// <param name="to">The result, such as an entity's current components.</param>
    /// <returns>The patch; empty when the two are equal.</returns>
    public static JsonObject Diff(JsonObject from, JsonObject to)
    {
        var patch = new JsonObject();

        foreach (var (key, value) in to)
        {
            if (value is null) continue; // a null never appears in a document written by the serializer

            if (!from.TryGetPropertyValue(key, out var before) || before is null)
            {
                patch[key] = value.DeepClone();
            }
            else if (value is JsonObject after && before is JsonObject beforeObject)
            {
                var child = Diff(beforeObject, after);
                if (child.Count > 0) patch[key] = child;
            }
            else if (!JsonNode.DeepEquals(before, value))
            {
                patch[key] = value.DeepClone();
            }
        }

        foreach (var (key, _) in from)
        {
            if (!to.ContainsKey(key)) patch[key] = null;
        }

        return patch;
    }

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
