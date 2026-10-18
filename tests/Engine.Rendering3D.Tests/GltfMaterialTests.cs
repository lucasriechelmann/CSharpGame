using Engine.Rendering;
using Engine.Rendering3D;
using Engine.Rendering3D.Tests.Fixtures;

namespace Engine.Rendering3D.Tests;

public class GltfMaterialTests
{
    private static readonly string FakeImage = "data:image/png;base64," + Convert.ToBase64String([137, 80, 78, 71, 1, 2, 3, 4]);

    private static ModelMaterialData Import(string materialJson, string extra = "")
    {
        string materials = "\"materials\": [ " + materialJson + " ],\n" + extra;
        var model = GltfBytes.Import(GltfBytes.TriangleJson(materialsJson: materials));
        return model.Materials[model.Parts[0].MaterialIndex];
    }

    [Fact]
    public void Metallic_and_roughness_factors_are_read_as_they_are()
    {
        var material = Import("""{ "pbrMetallicRoughness": { "metallicFactor": 0.25, "roughnessFactor": 0.75 } }""");

        Assert.Equal(0.25f, material.Metallic);
        Assert.Equal(0.75f, material.Roughness);
    }

    [Fact]
    public void The_gltf_defaults_are_fully_metallic_and_fully_rough()
    {
        var material = Import("{ }");

        Assert.Equal(1f, material.Metallic);
        Assert.Equal(1f, material.Roughness);
        Assert.Equal(AlphaMode.Opaque, material.AlphaMode);
        Assert.Equal(0.5f, material.AlphaCutoff);
    }

    [Fact]
    public void A_primitive_without_a_material_gets_the_default()
    {
        var model = GltfBytes.Import(GltfBytes.TriangleJson());

        var material = model.Materials[model.Parts[0].MaterialIndex];
        Assert.Equal(ModelMaterialData.Default.Metallic, material.Metallic);
        Assert.Equal(1f, material.Roughness);
    }

    [Fact]
    public void A_normal_map_and_its_scale_are_read()
    {
        var material = Import(
            """{ "normalTexture": { "index": 0, "scale": 0.5 } }""",
            "\"textures\": [ { \"source\": 0 } ], \"images\": [ { \"uri\": \"" + FakeImage + "\" } ],");

        Assert.NotNull(material.NormalImage);
        Assert.Equal(0.5f, material.NormalScale);
        Assert.Null(material.Image);
    }

    [Fact]
    public void A_metallic_roughness_map_is_read_separately_from_the_colour_map()
    {
        var material = Import(
            """{ "pbrMetallicRoughness": { "metallicRoughnessTexture": { "index": 0 } } }""",
            "\"textures\": [ { \"source\": 0 } ], \"images\": [ { \"uri\": \"" + FakeImage + "\" } ],");

        Assert.NotNull(material.MetallicRoughnessImage);
        Assert.Null(material.Image);
    }

    [Fact]
    public void Emissive_factor_is_read()
    {
        var material = Import("""{ "emissiveFactor": [1, 0.5, 0.25] }""");

        Assert.Equal(new Color(1f, 0.5f, 0.25f), material.Emissive);
    }

    [Fact]
    public void An_emissive_factor_with_the_wrong_length_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => Import("""{ "emissiveFactor": [1, 0] }"""));
    }

    [Theory]
    [InlineData("MASK", AlphaMode.Mask)]
    [InlineData("BLEND", AlphaMode.Blend)]
    [InlineData("OPAQUE", AlphaMode.Opaque)]
    public void Alpha_modes_are_read(string mode, AlphaMode expected)
    {
        var material = Import("{ \"alphaMode\": \"" + mode + "\", \"alphaCutoff\": 0.3 }");

        Assert.Equal(expected, material.AlphaMode);
        Assert.Equal(0.3f, material.AlphaCutoff);
    }

    [Fact]
    public void An_unknown_alpha_mode_is_rejected()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Import("""{ "alphaMode": "GLASS" }"""));

        Assert.Contains("GLASS", ex.Message);
    }
}
