using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace Engine.Editor.Theme;

/// <summary>
/// Makes ImGui look like a Windows 11 app: the Segoe UI font with Segoe Fluent Icons merged in, the dark Fluent palette with the system
/// blue as the accent, rounded corners, soft one-pixel borders and the roomier padding of Windows 11 controls. Call
/// <see cref="Configure"/> from the window's ImGui setup callback, which runs after the ImGui context exists and before its font atlas is built.
/// </summary>
public static class Win11Theme
{
    /// <summary>The text size in pixels (Windows 11's 14 px at 100% scaling reads small in a 1280x720 tool window, so a little larger).</summary>
    public const float FontSize = 16f;

    /// <summary>The accent colour used for fills: the Windows 11 default blue.</summary>
    public static readonly Vector4 Accent = Rgb(0x00, 0x78, 0xD4);

    /// <summary>The brighter accent used for text and thin highlights on dark surfaces.</summary>
    public static readonly Vector4 AccentLight = Rgb(0x60, 0xCD, 0xFF);

    private static readonly string FontFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");

    private static nint _iconRanges;

    /// <summary>Loads the fonts and sets the style. Safe to call when the Windows fonts are missing: ImGui's built-in font stays.</summary>
    public static void Configure()
    {
        LoadFonts();
        ApplyStyle();
    }

    /// <summary>Applies the Fluent dark colours, rounding and spacing to ImGui's style.</summary>
    public static void ApplyStyle()
    {
        var style = ImGui.GetStyle();

        style.WindowRounding = 8f;
        style.ChildRounding = 6f;
        style.PopupRounding = 8f;
        style.FrameRounding = 4f;
        style.GrabRounding = 4f;
        style.ScrollbarRounding = 10f;
        style.TabRounding = 5f;

        style.WindowBorderSize = 1f;
        style.ChildBorderSize = 0f;
        style.PopupBorderSize = 1f;
        style.FrameBorderSize = 1f;
        style.TabBorderSize = 0f;

        style.WindowPadding = new Vector2(10f, 10f);
        style.FramePadding = new Vector2(10f, 6f);
        style.ItemSpacing = new Vector2(8f, 6f);
        style.ItemInnerSpacing = new Vector2(6f, 4f);
        style.CellPadding = new Vector2(8f, 4f);
        style.IndentSpacing = 18f;
        style.ScrollbarSize = 12f;
        style.GrabMinSize = 12f;
        style.WindowTitleAlign = new Vector2(0f, 0.5f);
        style.SeparatorTextBorderSize = 1f;
        style.DockingSeparatorSize = 3f;
        style.WindowMenuButtonPosition = ImGuiDir.None;

        var colors = style.Colors;
        colors[(int)ImGuiCol.Text] = Rgb(0xFF, 0xFF, 0xFF);
        colors[(int)ImGuiCol.TextDisabled] = Rgba(0xFF, 0xFF, 0xFF, 0.54f);
        colors[(int)ImGuiCol.WindowBg] = Rgb(0x20, 0x20, 0x20);
        colors[(int)ImGuiCol.ChildBg] = Rgb(0x1C, 0x1C, 0x1C);
        colors[(int)ImGuiCol.PopupBg] = Rgb(0x2B, 0x2B, 0x2B);
        colors[(int)ImGuiCol.Border] = Rgba(0xFF, 0xFF, 0xFF, 0.09f);
        colors[(int)ImGuiCol.BorderShadow] = Rgba(0, 0, 0, 0f);

        colors[(int)ImGuiCol.FrameBg] = Rgba(0xFF, 0xFF, 0xFF, 0.06f);
        colors[(int)ImGuiCol.FrameBgHovered] = Rgba(0xFF, 0xFF, 0xFF, 0.09f);
        colors[(int)ImGuiCol.FrameBgActive] = Rgba(0xFF, 0xFF, 0xFF, 0.04f);

        colors[(int)ImGuiCol.TitleBg] = Rgb(0x1C, 0x1C, 0x1C);
        colors[(int)ImGuiCol.TitleBgActive] = Rgb(0x26, 0x26, 0x26);
        colors[(int)ImGuiCol.TitleBgCollapsed] = Rgb(0x1C, 0x1C, 0x1C);
        colors[(int)ImGuiCol.MenuBarBg] = Rgb(0x1C, 0x1C, 0x1C);

        colors[(int)ImGuiCol.ScrollbarBg] = Rgba(0, 0, 0, 0f);
        colors[(int)ImGuiCol.ScrollbarGrab] = Rgba(0xFF, 0xFF, 0xFF, 0.30f);
        colors[(int)ImGuiCol.ScrollbarGrabHovered] = Rgba(0xFF, 0xFF, 0xFF, 0.45f);
        colors[(int)ImGuiCol.ScrollbarGrabActive] = Rgba(0xFF, 0xFF, 0xFF, 0.60f);

        colors[(int)ImGuiCol.CheckMark] = Rgb(0xFF, 0xFF, 0xFF);
        colors[(int)ImGuiCol.SliderGrab] = AccentLight;
        colors[(int)ImGuiCol.SliderGrabActive] = Accent;

        colors[(int)ImGuiCol.Button] = Rgba(0xFF, 0xFF, 0xFF, 0.08f);
        colors[(int)ImGuiCol.ButtonHovered] = Rgba(0xFF, 0xFF, 0xFF, 0.12f);
        colors[(int)ImGuiCol.ButtonActive] = Rgba(0xFF, 0xFF, 0xFF, 0.05f);

        colors[(int)ImGuiCol.Header] = Rgba(0xFF, 0xFF, 0xFF, 0.09f);
        colors[(int)ImGuiCol.HeaderHovered] = Rgba(0xFF, 0xFF, 0xFF, 0.07f);
        colors[(int)ImGuiCol.HeaderActive] = Rgba(0xFF, 0xFF, 0xFF, 0.12f);

        colors[(int)ImGuiCol.Separator] = Rgba(0xFF, 0xFF, 0xFF, 0.09f);
        colors[(int)ImGuiCol.SeparatorHovered] = AccentLight;
        colors[(int)ImGuiCol.SeparatorActive] = Accent;
        colors[(int)ImGuiCol.ResizeGrip] = Rgba(0xFF, 0xFF, 0xFF, 0.10f);
        colors[(int)ImGuiCol.ResizeGripHovered] = AccentLight with { W = 0.6f };
        colors[(int)ImGuiCol.ResizeGripActive] = Accent;

        colors[(int)ImGuiCol.Tab] = Rgba(0xFF, 0xFF, 0xFF, 0.00f);
        colors[(int)ImGuiCol.TabHovered] = Rgba(0xFF, 0xFF, 0xFF, 0.08f);
        colors[(int)ImGuiCol.TabActive] = Rgba(0xFF, 0xFF, 0xFF, 0.12f);
        colors[(int)ImGuiCol.TabUnfocused] = Rgba(0xFF, 0xFF, 0xFF, 0.00f);
        colors[(int)ImGuiCol.TabUnfocusedActive] = Rgba(0xFF, 0xFF, 0xFF, 0.07f);

        colors[(int)ImGuiCol.DockingPreview] = Accent with { W = 0.45f };
        colors[(int)ImGuiCol.DockingEmptyBg] = Rgb(0x16, 0x16, 0x16);
        colors[(int)ImGuiCol.TextSelectedBg] = Accent with { W = 0.55f };
        colors[(int)ImGuiCol.DragDropTarget] = AccentLight;
        colors[(int)ImGuiCol.NavHighlight] = AccentLight;
        colors[(int)ImGuiCol.NavWindowingHighlight] = Rgba(0xFF, 0xFF, 0xFF, 0.60f);
        colors[(int)ImGuiCol.NavWindowingDimBg] = Rgba(0, 0, 0, 0.35f);
        colors[(int)ImGuiCol.ModalWindowDimBg] = Rgba(0, 0, 0, 0.45f);
        colors[(int)ImGuiCol.TableHeaderBg] = Rgb(0x2B, 0x2B, 0x2B);
        colors[(int)ImGuiCol.TableBorderStrong] = Rgba(0xFF, 0xFF, 0xFF, 0.12f);
        colors[(int)ImGuiCol.TableBorderLight] = Rgba(0xFF, 0xFF, 0xFF, 0.07f);
        colors[(int)ImGuiCol.TableRowBg] = Rgba(0, 0, 0, 0f);
        colors[(int)ImGuiCol.TableRowBgAlt] = Rgba(0xFF, 0xFF, 0xFF, 0.025f);
        colors[(int)ImGuiCol.PlotLines] = AccentLight;
        colors[(int)ImGuiCol.PlotHistogram] = Accent;
    }

    /// <summary>Adds Segoe UI as the default font and merges the Segoe Fluent Icons glyphs into it, so icon codes (see <see cref="Icons"/>) can sit inside text.</summary>
    private static unsafe void LoadFonts()
    {
        string text = Path.Combine(FontFolder, "segoeui.ttf");
        if (!File.Exists(text)) return;

        var fonts = ImGui.GetIO().Fonts;
        fonts.AddFontFromFileTTF(text, FontSize);

        string icons = Path.Combine(FontFolder, "SegoeIcons.ttf");
        if (!File.Exists(icons)) return;

        // The glyph range list must outlive the atlas build, so it lives in unmanaged memory for the life of the program.
        _iconRanges = Marshal.AllocHGlobal(3 * sizeof(ushort));
        var ranges = (ushort*)_iconRanges;
        ranges[0] = 0xE000;
        ranges[1] = 0xF8FF;
        ranges[2] = 0;

        var config = ImGuiNative.ImFontConfig_ImFontConfig();
        config->MergeMode = 1;
        config->PixelSnapH = 1;
        config->GlyphOffset = new Vector2(0f, 1f);
        fonts.AddFontFromFileTTF(icons, FontSize, new ImFontConfigPtr(config), _iconRanges);
        ImGuiNative.ImFontConfig_destroy(config);
    }

    private static Vector4 Rgb(byte r, byte g, byte b) => new(r / 255f, g / 255f, b / 255f, 1f);

    private static Vector4 Rgba(byte r, byte g, byte b, float a) => new(r / 255f, g / 255f, b / 255f, a);

    /// <summary>
    /// Stops ImGui from writing its settings file on its own (it would otherwise save into <c>imgui.ini</c> in the working folder, and save whatever state the
    /// windows happen to be in when the program is closed or killed). The editor saves its layout only when asked, with <see cref="SaveLayout"/>.
    /// </summary>
    public static unsafe void DisableLayoutAutoSave() => ImGui.GetIO().NativePtr->IniFilename = null;

    /// <summary>Loads a layout saved earlier with <see cref="SaveLayout"/>. Call it from the setup callback. Does nothing when the file does not exist.</summary>
    /// <param name="path">The layout file.</param>
    public static void LoadLayout(string path)
    {
        if (File.Exists(path)) ImGui.LoadIniSettingsFromDisk(path);
    }

    /// <summary>Saves where every window and dock is now.</summary>
    /// <param name="path">The layout file.</param>
    public static void SaveLayout(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        ImGui.SaveIniSettingsToDisk(path);
    }
}
