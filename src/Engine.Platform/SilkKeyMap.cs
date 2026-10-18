using Engine.Core.Input;
using SilkKey = Silk.NET.Input.Key;

namespace Engine.Platform;

/// <summary>Translates Silk.NET keys to engine <see cref="KeyCode"/>s. Built once by matching enum member names, so unsupported keys simply have no entry.</summary>
internal static class SilkKeyMap
{
    private static readonly Dictionary<SilkKey, KeyCode> Map = Build();

    /// <summary>Looks up the engine key for a Silk key; false when the engine does not support that key.</summary>
    public static bool TryGet(SilkKey key, out KeyCode code) => Map.TryGetValue(key, out code);

    private static Dictionary<SilkKey, KeyCode> Build()
    {
        var map = new Dictionary<SilkKey, KeyCode>();
        foreach (var code in Enum.GetValues<KeyCode>())
        {
            if (code != KeyCode.None && Enum.TryParse(code.ToString(), out SilkKey silkKey))
                map[silkKey] = code;
        }

        return map;
    }
}
