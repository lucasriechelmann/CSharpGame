using System.Numerics;
using Engine.Core.Components;
using Engine.Physics2D;
using ImGuiNET;

namespace Platformer;

// The sample's debug panels: live character tuning, physics settings and camera.
public sealed partial class PlatformerGame
{
    /// <inheritdoc />
    protected override void DebugGui()
    {
        ImGui.SetNextWindowPos(new Vector2(10f, 185f), ImGuiCond.FirstUseEver);
        ImGui.Begin("Sample");
        ImGui.Text("A/D or arrows: move   Space/W/Up: jump");
        ImGui.Text("Left click: spawn 5   Q/E or scroll: zoom   R: reset camera");
        ImGui.Text($"Sprites: {_batch.Stats.Sprites}   Draw calls: {_batch.Stats.DrawCalls}");

        var mouseWorld = _camera.ScreenToWorld(Input.MousePosition);
        ImGui.Text($"Mouse world: {mouseWorld.X:F0}, {mouseWorld.Y:F0}");

        float zoom = _camera.Zoom;
        if (ImGui.SliderFloat("Zoom", ref zoom, 0.1f, 8f)) _camera.Zoom = zoom;

        ImGui.ColorEdit3("Clear color", ref _clear);
        ImGui.End();

        DrawHeroPanel();
    }

    private void DrawHeroPanel()
    {
        ref var controller = ref World.Get<PlatformerController>(_hero);
        ref var body = ref World.Get<PhysicsBody>(_hero);
        ref var transform = ref World.Get<Transform>(_hero);

        ImGui.SetNextWindowPos(new Vector2(10f, 375f), ImGuiCond.FirstUseEver);
        ImGui.Begin("Hero feel");
        ImGui.Text($"Pos: {transform.Position.X:F0}, {transform.Position.Y:F0}   Vel: {body.Velocity.X:F0}, {body.Velocity.Y:F0}");
        ImGui.Text($"Contacts: {body.Contacts}   Jumping: {controller.IsJumping}");
        ImGui.Text($"Coyote: {controller.CoyoteTimer:F3}s   Buffer: {controller.JumpBufferTimer:F3}s");
        ImGui.Separator();

        float gravity = _physicsSettings.Gravity;
        if (ImGui.SliderFloat("Gravity", ref gravity, -4000f, -200f)) _physicsSettings.Gravity = gravity;
        ImGui.SliderFloat("Move speed", ref controller.Tuning.MoveSpeed, 50f, 600f);
        ImGui.SliderFloat("Ground accel", ref controller.Tuning.GroundAcceleration, 200f, 8000f);
        ImGui.SliderFloat("Air accel", ref controller.Tuning.AirAcceleration, 200f, 8000f);
        ImGui.SliderFloat("Jump speed", ref controller.Tuning.JumpSpeed, 200f, 1200f);
        ImGui.SliderFloat("Coyote time", ref controller.Tuning.CoyoteTime, 0f, 0.4f);
        ImGui.SliderFloat("Jump buffer", ref controller.Tuning.JumpBufferTime, 0f, 0.4f);
        ImGui.SliderFloat("Jump cut", ref controller.Tuning.JumpCutMultiplier, 0f, 1f);
        ImGui.SliderFloat("Fall gravity x", ref controller.Tuning.FallGravityMultiplier, 1f, 4f);

        if (ImGui.Button("Reset tuning")) controller.Tuning = PlatformerTuning.Default;
        ImGui.End();
    }
}
