using Engine.Rendering3D;

namespace Engine.Rendering3D.Tests;

public class MaterialJsonTests
{
    [Fact]
    public void Every_key_is_optional()
    {
        var data = MaterialJson.Read("{}");

        Assert.Null(data.BaseColor);
        Assert.Null(data.Texture);
        Assert.Equal(32f, data.Shininess);
        Assert.False(data.Unlit);
        Assert.False(data.DoubleSided);
    }

    [Fact]
    public void Reads_all_fields()
    {
        var data = MaterialJson.Read("""
            { "baseColor": [0.1, 0.2, 0.3, 0.4], "texture": "bricks.png", "shininess": 64, "specular": 0.5, "unlit": true, "doubleSided": true }
            """);

        Assert.Equal([0.1f, 0.2f, 0.3f, 0.4f], data.BaseColor!);
        Assert.Equal("bricks.png", data.Texture);
        Assert.Equal(64f, data.Shininess);
        Assert.Equal(0.5f, data.Specular);
        Assert.True(data.Unlit);
        Assert.True(data.DoubleSided);
    }

    [Fact]
    public void Colour_must_have_three_or_four_numbers()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => MaterialJson.Read("""{ "baseColor": [1, 0] }"""));

        Assert.Contains("3 (RGB) or 4 (RGBA)", ex.Message);
    }

    [Fact]
    public void Shininess_must_be_positive()
    {
        Assert.Throws<InvalidOperationException>(() => MaterialJson.Read("""{ "shininess": 0 }"""));
    }

    [Fact]
    public void Invalid_json_is_reported_as_a_material_error()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => MaterialJson.Read("{ nope"));

        Assert.Contains("Material file is not valid", ex.Message);
    }
}
