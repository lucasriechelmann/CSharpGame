using Engine.Core.Assets;
using Engine.Core.Tests.Fixtures;

namespace Engine.Core.Tests.Assets;

public sealed class AssetManagerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "csharpgame-tests-" + Guid.NewGuid().ToString("N"));
    private readonly AssetManager _assets;

    public AssetManagerTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "sub"));
        File.WriteAllText(Path.Combine(_root, "hello.txt"), "hello");
        File.WriteAllText(Path.Combine(_root, "sub", "nested.txt"), "nested");

        _assets = new AssetManager(_root);
        _assets.Register(new TextLoader());
    }

    public void Dispose()
    {
        _assets.Dispose();
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void Load_returns_the_asset_through_its_handle()
    {
        var handle = _assets.Load<TextAsset>("hello.txt");

        Assert.True(handle.IsValid);
        Assert.Equal("hello", _assets.Get(handle).Text);
    }

    [Fact]
    public void Loading_the_same_path_twice_returns_the_same_handle_and_loads_once()
    {
        var first = _assets.Load<TextAsset>("hello.txt");
        var second = _assets.Load<TextAsset>("hello.txt");

        Assert.Equal(first, second);
        Assert.Same(_assets.Get(first), _assets.Get(second));
        Assert.Equal(1, _assets.Count);
    }

    [Fact]
    public void Slash_direction_and_leading_dot_slash_do_not_matter()
    {
        var a = _assets.Load<TextAsset>("sub/nested.txt");
        var b = _assets.Load<TextAsset>("sub\\nested.txt");
        var c = _assets.Load<TextAsset>("./sub/nested.txt");

        Assert.Equal(a, b);
        Assert.Equal(a, c);
        Assert.Equal("sub/nested.txt", _assets.NameOf(a));
    }

    [Fact]
    public void Missing_file_throws_FileNotFound()
    {
        Assert.Throws<FileNotFoundException>(() => _assets.Load<TextAsset>("nope.txt"));
    }

    [Fact]
    public void Loading_a_type_without_a_loader_throws()
    {
        Assert.Throws<InvalidOperationException>(() => _assets.Load<string>("hello.txt"));
    }

    [Fact]
    public void Registering_two_loaders_for_one_type_throws()
    {
        Assert.Throws<InvalidOperationException>(() => _assets.Register(new TextLoader()));
    }

    [Fact]
    public void Add_registers_an_in_memory_asset_under_a_name()
    {
        var asset = new TextAsset("made in code");
        var handle = _assets.Add("builtin/note", asset);

        Assert.Same(asset, _assets.Get(handle));
        Assert.Equal("builtin/note", _assets.NameOf(handle));
        Assert.Throws<InvalidOperationException>(() => _assets.Add("builtin/note", new TextAsset("again")));
    }

    [Fact]
    public void Invalid_and_foreign_handles_are_rejected()
    {
        Assert.Throws<KeyNotFoundException>(() => _assets.Get(AssetHandle<TextAsset>.Invalid));
        Assert.Throws<KeyNotFoundException>(() => _assets.Get(new AssetHandle<TextAsset>(99)));
        Assert.False(_assets.TryGet(AssetHandle<TextAsset>.Invalid, out _));
        Assert.False(AssetHandle<TextAsset>.Invalid.IsValid);
        Assert.False(default(AssetHandle<TextAsset>).IsValid);
    }

    [Fact]
    public void Handles_for_the_same_name_but_different_types_are_independent()
    {
        var text = _assets.Add("shared", new TextAsset("t"));
        var other = _assets.Add("shared", new DisposableAsset());

        Assert.Equal(1, text.Id);
        Assert.Equal(1, other.Id);
        Assert.Equal(2, _assets.Count);
    }

    [Fact]
    public void Dispose_disposes_disposable_assets_once()
    {
        var disposable = new DisposableAsset();
        _assets.Add("d", disposable);

        _assets.Dispose();
        _assets.Dispose(); // second call must be harmless

        Assert.Equal(1, disposable.DisposeCount);
        Assert.Equal(0, _assets.Count);
    }
}
