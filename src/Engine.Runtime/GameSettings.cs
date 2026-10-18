using System.Text.Json;

namespace Engine.Runtime;

/// <summary>
/// Player-facing settings of a shipped game, read from a small <c>game.settings.json</c> next to the program so they can be changed without
/// rebuilding: window size, full screen, vsync and the master volume. Every field is optional; a missing file or field means the game's own default.
/// </summary>
public sealed record GameSettings
{
    /// <summary>The file name looked for next to the executable.</summary>
    public const string FileName = "game.settings.json";

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>Window width in pixels, or null to use the game's own.</summary>
    public int? WindowWidth { get; init; }

    /// <summary>Window height in pixels, or null to use the game's own.</summary>
    public int? WindowHeight { get; init; }

    /// <summary>True starts the game full screen.</summary>
    public bool Fullscreen { get; init; }

    /// <summary>Whether to synchronise with the display refresh; default on.</summary>
    public bool VSync { get; init; } = true;

    /// <summary>The master volume, 0 to 1; default full.</summary>
    public float MasterVolume { get; init; } = 1f;

    /// <summary>The settings used when there is no file.</summary>
    public static GameSettings Default => new();

    /// <summary>Reads a settings file. A missing file gives <see cref="Default"/>.</summary>
    /// <param name="path">Path to the settings file.</param>
    /// <exception cref="InvalidDataException">The file exists but is not valid JSON or has an impossible value.</exception>
    public static GameSettings Load(string path)
    {
        if (!File.Exists(path)) return Default;

        GameSettings? settings;
        try
        {
            settings = JsonSerializer.Deserialize<GameSettings>(File.ReadAllText(path), Json);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"'{path}' is not a valid settings file: {ex.Message}", ex);
        }

        settings ??= Default;
        if (settings.WindowWidth is <= 0 || settings.WindowHeight is <= 0)
            throw new InvalidDataException($"'{path}': the window size must be positive.");
        if (settings.MasterVolume is < 0f or > 1f)
            throw new InvalidDataException($"'{path}': masterVolume must be between 0 and 1.");
        return settings;
    }

    /// <summary>Writes the settings.</summary>
    /// <param name="path">Where to write.</param>
    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, Json));
    }
}
