using System.Numerics;
using Engine.Core.Components;
using Engine.Rendering3D;

namespace Engine.Rendering3D.Tests;

public class ModelMatrixTests
{
    [Fact]
    public void Scale_then_rotate_then_translate()
    {
        var transform = new Transform
        {
            Position = new Vector3(10f, 0f, 0f),
            Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2f),
            Scale = new Vector3(2f, 1f, 1f),
        };

        // Local +X, scaled to 2, then turned a quarter around Y (+X becomes -Z), then moved by 10 on X.
        var world = Vector3.Transform(Vector3.UnitX, ModelMatrix.From(transform));

        Assert.Equal(10f, world.X, 4);
        Assert.Equal(0f, world.Y, 4);
        Assert.Equal(-2f, world.Z, 4);
    }

    [Fact]
    public void Identity_transform_gives_the_identity_matrix()
    {
        Assert.Equal(Matrix4x4.Identity, ModelMatrix.From(Transform.Identity));
    }

    [Fact]
    public void Normal_matrix_keeps_normals_perpendicular_under_non_uniform_scale()
    {
        var model = Matrix4x4.CreateScale(4f, 1f, 1f);
        var tangent = Vector3.Normalize(new Vector3(1f, -1f, 0f)); // lies in a surface whose normal is (1, 1, 0)
        var normal = Vector3.Normalize(new Vector3(1f, 1f, 0f));

        var worldTangent = Vector3.TransformNormal(tangent, model);
        var worldNormal = Vector3.TransformNormal(normal, ModelMatrix.NormalMatrix(model));
        var naiveNormal = Vector3.TransformNormal(normal, model);

        Assert.Equal(0f, Vector3.Dot(worldTangent, worldNormal), 4);
        Assert.NotEqual(0f, Vector3.Dot(worldTangent, naiveNormal), 2); // the plain model matrix would tilt the lighting
    }

    [Fact]
    public void Normal_matrix_of_a_singular_model_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => ModelMatrix.NormalMatrix(Matrix4x4.CreateScale(1f, 0f, 1f)));
    }

    [Fact]
    public void Max_scale_is_the_largest_axis_stretch()
    {
        var model = Matrix4x4.CreateScale(1f, 3f, 2f) * Matrix4x4.CreateRotationX(0.7f);

        Assert.Equal(3f, ModelMatrix.MaxScale(model), 4);
    }
}
