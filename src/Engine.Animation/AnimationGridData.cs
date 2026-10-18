namespace Engine.Animation;

/// <summary>JSON shape for describing clip frames as cells of a regular sprite sheet (see <see cref="AnimationClip.FromGrid"/>).</summary>
public sealed class AnimationGridData
{
    /// <summary>Width of one cell in pixels.</summary>
    public int CellWidth { get; set; }

    /// <summary>Height of one cell in pixels.</summary>
    public int CellHeight { get; set; }

    /// <summary>Number of cells per row in the sheet.</summary>
    public int Columns { get; set; }

    /// <summary>The cells to play, counted left to right then top to bottom from 0.</summary>
    public List<int> Cells { get; set; } = new();
}
