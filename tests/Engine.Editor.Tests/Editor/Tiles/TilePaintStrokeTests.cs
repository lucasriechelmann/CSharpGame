using Engine.Core.Assets;
using Engine.Core.Ecs;
using Engine.Editor.Tiles;
using Engine.Runtime.Editing;
using Engine.Tilemaps;

namespace Engine.Editor.Tests.Editor.Tiles;

public class TilePaintStrokeTests
{
    private static readonly AssetHandle<TileLayer> Handle = new(1);

    private readonly TileLayer _layer = new(32f);
    private readonly EditContext _context = new(new World());

    [Fact]
    public void Painting_sets_the_tile_immediately()
    {
        var stroke = new TilePaintStroke(Handle, _layer);

        Assert.True(stroke.Paint(2, 3, 5));

        Assert.Equal(5, _layer.GetTile(2, 3));
    }

    [Fact]
    public void Painting_the_same_tile_again_changes_nothing()
    {
        _layer.SetTile(1, 1, 4);
        var stroke = new TilePaintStroke(Handle, _layer);

        Assert.False(stroke.Paint(1, 1, 4));
        Assert.True(stroke.IsEmpty);
    }

    [Fact]
    public void Erasing_paints_id_zero()
    {
        _layer.SetTile(1, 1, 4);
        var stroke = new TilePaintStroke(Handle, _layer);

        stroke.Paint(1, 1, 0);

        Assert.Equal(0, _layer.GetTile(1, 1));
        Assert.Equal(new TileChange(1, 1, 4, 0), Assert.Single(stroke.Changes()));
    }

    [Fact]
    public void A_negative_id_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TilePaintStroke(Handle, _layer).Paint(0, 0, -1));
    }

    [Fact]
    public void A_cell_painted_twice_remembers_its_original_id()
    {
        _layer.SetTile(0, 0, 2);
        var stroke = new TilePaintStroke(Handle, _layer);

        stroke.Paint(0, 0, 7);
        stroke.Paint(0, 0, 9);

        Assert.Equal(new TileChange(0, 0, 2, 9), Assert.Single(stroke.Changes()));
    }

    [Fact]
    public void A_cell_restored_within_the_stroke_is_not_a_change()
    {
        _layer.SetTile(0, 0, 2);
        var stroke = new TilePaintStroke(Handle, _layer);

        stroke.Paint(0, 0, 7);
        stroke.Paint(0, 0, 2);

        Assert.True(stroke.IsEmpty);
    }

    [Fact]
    public void The_stroke_becomes_one_undo_step_that_reverts_every_cell()
    {
        _layer.SetTile(0, 0, 1);
        var stroke = new TilePaintStroke(Handle, _layer);
        stroke.Paint(0, 0, 5);
        stroke.Paint(1, 0, 5);
        stroke.Paint(2, 0, 5);
        var stack = new CommandStack(_context);
        stack.Push(stroke.ToCommand("Ground"));

        stack.Undo();

        Assert.Equal(1, _layer.GetTile(0, 0));
        Assert.Equal(0, _layer.GetTile(1, 0));
        Assert.Equal(0, _layer.GetTile(2, 0));
    }

    [Fact]
    public void Redo_paints_the_stroke_again()
    {
        var stroke = new TilePaintStroke(Handle, _layer);
        stroke.Paint(4, 4, 6);
        var stack = new CommandStack(_context);
        stack.Push(stroke.ToCommand("Ground"));
        stack.Undo();

        stack.Redo();

        Assert.Equal(6, _layer.GetTile(4, 4));
    }

    [Fact]
    public void Undo_and_redo_mark_the_layer_as_unsaved()
    {
        var stroke = new TilePaintStroke(Handle, _layer);
        stroke.Paint(0, 0, 3);
        var stack = new CommandStack(_context);
        stack.Push(stroke.ToCommand("Ground"));

        stack.Undo();

        Assert.Contains(Handle, _context.DirtyTileLayers);
    }

    [Fact]
    public void The_description_counts_the_tiles()
    {
        var one = new TilePaintStroke(Handle, _layer);
        one.Paint(0, 0, 1);
        var many = new TilePaintStroke(Handle, _layer);
        many.Paint(1, 0, 1);
        many.Paint(2, 0, 1);

        Assert.Equal("Paint a tile on Ground", one.ToCommand("Ground").Description);
        Assert.Equal("Paint 2 tiles on Ground", many.ToCommand("Ground").Description);
    }
}
