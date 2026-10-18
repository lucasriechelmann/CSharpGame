using System.Numerics;
using ImGuiNET;

namespace Engine.Runtime.Menus;

/// <summary>
/// Draws a <see cref="PauseMenu"/> as a centred ImGui window over the game view. It owns no state: the game keeps the menu, feeds it key presses and
/// acts on the choice this returns, so 2D and 3D games share one look.
/// </summary>
public static class PauseMenuView
{
    private const ImGuiWindowFlags Flags =
        ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize;

    /// <summary>Draws the menu (call it from <see cref="Game.Hud"/> while paused).</summary>
    /// <param name="menu">The menu state; hovering an entry highlights it.</param>
    /// <param name="viewOrigin">Top-left of the game view (<see cref="Game.ViewportOrigin"/>).</param>
    /// <param name="viewSize">Size of the game view in pixels.</param>
    /// <param name="hint">Small help line under the entries, naming the keys.</param>
    /// <returns>The entry the player clicked this frame, or null.</returns>
    public static PauseChoice? Draw(PauseMenu menu, Vector2 viewOrigin, Vector2 viewSize, string hint)
    {
        ArgumentNullException.ThrowIfNull(menu);

        ImGui.SetNextWindowPos(viewOrigin + viewSize / 2f, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        ImGui.Begin("##pause", Flags);
        ImGui.SetWindowFontScale(3f);
        ImGui.TextColored(new Vector4(1f, 0.85f, 0.3f, 1f), "PAUSED");
        ImGui.SetWindowFontScale(1.8f);

        PauseChoice? clicked = null;
        foreach (var choice in menu.Choices)
        {
            bool selected = choice == menu.Selected;
            if (ImGui.Selectable(PauseMenu.Label(choice), selected, ImGuiSelectableFlags.None, new Vector2(260f, 0f))) clicked = choice;
            if (ImGui.IsItemHovered()) menu.Select(choice);
        }

        ImGui.SetWindowFontScale(1f);
        ImGui.TextDisabled(hint);
        ImGui.End();
        return clicked;
    }
}
