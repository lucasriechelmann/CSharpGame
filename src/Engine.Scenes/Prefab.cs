using System.Text.Json.Nodes;

namespace Engine.Scenes;

/// <summary>
/// A reusable entity template loaded from a <c>.prefab.json</c> file: the same shape as a scene entry
/// (<c>components</c>, optionally based on another <c>prefab</c>). It is an asset, so it is cached by name like any other.
/// </summary>
/// <param name="Template">The parsed entity JSON. Treated as read-only; instantiation works on a copy.</param>
public sealed record Prefab(JsonObject Template);
