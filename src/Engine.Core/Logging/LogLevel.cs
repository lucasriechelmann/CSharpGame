namespace Engine.Core.Logging;

/// <summary>How serious a log message is. Ordered, so a logger can drop everything below a minimum.</summary>
public enum LogLevel
{
    /// <summary>Detail for developers chasing a problem; dropped by default.</summary>
    Debug,

    /// <summary>Normal events worth a line: a scene loaded, a device chosen.</summary>
    Info,

    /// <summary>Something unexpected that the game recovered from, such as falling back to a silent audio device.</summary>
    Warning,

    /// <summary>Something failed. Logging it does not replace throwing: engine code still fails loudly.</summary>
    Error,
}
