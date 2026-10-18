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
        Assert.Equal(0f, data.Metallic);
        Assert.Equal(0.6f, data.Roughness);
        Assert.Equal(1f, data.NormalScale);
        Assert.True(data.CastShadows);
        Assert.False(data.Unlit);
        Assert.False(data.DoubleSided);
        Assert.Equal(AlphaMode.Opaque, MaterialJson.ParseAlphaMode(data.AlphaMode));
    }

    [Fact]
    public void Reads_all_fields()
    {
        var data = MaterialJson.Read("""
            {
              "baseColor": [0.1, 0.2, 0.3, 0.4], "texture": "bricks.png", "metallic": 0.9, "roughness": 0.2,
              "metallicRoughnessTexture": "bricks_mr.png", "normalTexture": "bricks_n.png", "normalScale": 0.5,
              "emissive": [1, 0.5, 0], "alphaMode": "mask", "alphaCutoff": 0.3,
              "unlit": true, "doubleSided": true, "castShadows": false
            }
            """);

        Assert.Equal([0.1f, 0.2f, 0.3f, 0.4f], data.BaseColor!);
        Assert.Equal("bricks.png", data.Texture);
        Assert.Equal(0.9f, data.Metallic);
        Assert.Equal(0.2f, data.Roughness);
        Assert.Equal("bricks_mr.png", data.MetallicRoughnessTexture);
        Assert.Equal("bricks_n.png", data.NormalTexture);
        Assert.Equal(0.5f, data.NormalScale);
        Assert.Equal([1f, 0.5f, 0f], data.Emissive!);
        Assert.Equal(AlphaMode.Mask, MaterialJson.ParseAlphaMode(data.AlphaMode));
        Assert.Equal(0.3f, data.AlphaCutoff);
        Assert.True(data.Unlit);
        Assert.True(data.DoubleSided);
        Assert.False(data.CastShadows);
    }

    [Fact]
    public void Colour_must_have_three_or_four_numbers()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => MaterialJson.Read("""{ "baseColor": [1, 0] }"""));

        Assert.Contains("3 (RGB) or 4 (RGBA)", ex.Message);
    }

    [Fact]
    public void Emissive_must_have_three_numbers()
    {
        Assert.Throws<InvalidOperationException>(() => MaterialJson.Read("""{ "emissive": [1, 0, 0, 1] }"""));
    }

    [Theory]
    [InlineData("""{ "roughness": 1.5 }""")]
    [InlineData("""{ "roughness": -0.1 }""")]
    [InlineData("""{ "metallic": 2 }""")]
    [InlineData("""{ "alphaCutoff": 3 }""")]
    public void Factors_outside_zero_to_one_are_rejected(string json)
    {
        Assert.Throws<InvalidOperationException>(() => MaterialJson.Read(json));
    }

    [Theory]
    [InlineData("shininess")]
    [InlineData("specular")]
    public void The_old_blinn_phong_keys_say_what_to_use_instead(string key)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => MaterialJson.Read("{ \"" + key + "\": 10 }"));

        Assert.Contains("roughness", ex.Message);
    }

    [Fact]
    public void An_unknown_key_is_an_error_naming_it()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => MaterialJson.Read("""{ "metalic": 1 }"""));

        Assert.Contains("metalic", ex.Message);
    }

    [Fact]
    public void An_unknown_alpha_mode_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => MaterialJson.Read("""{ "alphaMode": "translucent" }"""));
    }

    [Theory]
    [InlineData("OPAQUE", AlphaMode.Opaque)]
    [InlineData("Mask", AlphaMode.Mask)]
    [InlineData("blend", AlphaMode.Blend)]
    [InlineData(null, AlphaMode.Opaque)]
    public void Alpha_modes_are_case_insensitive(string? name, AlphaMode expected)
    {
        Assert.Equal(expected, MaterialJson.ParseAlphaMode(name));
    }

    [Fact]
    public void Invalid_json_is_reported_as_a_material_error()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => MaterialJson.Read("{ nope"));

        Assert.Contains("Material file is not valid", ex.Message);
    }

    [Fact]
    public void Only_blend_counts_as_blended()
    {
        Assert.False(new Material { AlphaMode = AlphaMode.Opaque }.IsBlended);
        Assert.False(new Material { AlphaMode = AlphaMode.Mask }.IsBlended);
        Assert.True(new Material { AlphaMode = AlphaMode.Blend }.IsBlended);
    }
}
