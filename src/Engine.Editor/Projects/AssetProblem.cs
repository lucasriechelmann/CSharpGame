namespace Engine.Editor.Projects;

/// <summary>One asset file that failed to load, and why.</summary>
/// <param name="Asset">The asset's name relative to the asset root, such as <c>prefabs/coin.prefab.json</c>.</param>
/// <param name="Message">What went wrong.</param>
public sealed record AssetProblem(string Asset, string Message);
