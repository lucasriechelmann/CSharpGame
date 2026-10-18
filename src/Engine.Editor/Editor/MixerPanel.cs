using Engine.Audio;
using ImGuiNET;

namespace Engine.Editor;

/// <summary>
/// The editor's "Mixer" window: the audio mixer's groups as volume sliders with a mute switch each, plus the device's master volume. Changes act at once
/// on sounds that are already playing, so a game's mix can be balanced by ear while it plays inside the editor. Group volumes are not saved: they are
/// set by the game (or its settings) when it starts.
/// </summary>
internal sealed class MixerPanel(Func<IAudioDevice> device)
{
    /// <summary>Draws the window. Call between ImGui frame begin and render.</summary>
    public void Draw()
    {
        if (!ImGui.Begin("Mixer"))
        {
            ImGui.End();
            return;
        }

        var audio = device();
        ImGui.TextDisabled(audio.IsAudible ? audio.Description : "No sound: " + audio.Description);
        ImGui.Separator();

        float master = audio.MasterVolume;
        if (ImGui.SliderFloat("Device volume", ref master, 0f, 1f, "%.2f")) audio.MasterVolume = master;
        ImGui.Dummy(new System.Numerics.Vector2(0f, 4f));
        ImGui.TextUnformatted("Groups");

        foreach (var group in audio.Mixer.Groups) DrawGroup(group);

        ImGui.TextDisabled($"Voices playing: {audio.ActiveVoices}");
        ImGui.End();
    }

    private static void DrawGroup(AudioGroup group)
    {
        ImGui.PushID(group.Name);
        bool muted = group.Muted;
        if (ImGui.Checkbox("##mute", ref muted)) group.Muted = muted;
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Mute");

        ImGui.SameLine();
        float volume = group.Volume;
        ImGui.SetNextItemWidth(220f);
        if (ImGui.SliderFloat(group.Name, ref volume, 0f, 1f, "%.2f")) group.Volume = volume;

        ImGui.SameLine();
        ImGui.TextDisabled($"= {group.EffectiveVolume:0.00}");
        ImGui.PopID();
    }
}
