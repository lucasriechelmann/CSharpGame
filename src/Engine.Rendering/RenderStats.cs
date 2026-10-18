namespace Engine.Rendering;

/// <summary>Counters for one rendered frame, handy for the debug overlay.</summary>
/// <param name="DrawCalls">GPU draw calls issued. Fewer is better; batching keeps this near the number of distinct textures.</param>
/// <param name="Sprites">Sprites submitted.</param>
public readonly record struct RenderStats(int DrawCalls, int Sprites);
