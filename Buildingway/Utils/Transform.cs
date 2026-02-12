using System.Numerics;
using Dalamud.Bindings.ImGuizmo;

namespace Buildingway.Utils;

public struct Transform(Vector3 position, Vector3 scale, Vector3 rotation)
{
    public Vector3 Position = position;
    public Vector3 Scale = scale;
    public Vector3 Rotation = rotation;
    
    public Matrix4x4 GetTransformation()
    {
        var mat = Matrix4x4.Identity;
        ImGuizmo.RecomposeMatrixFromComponents(ref Position.X, ref Rotation.X, ref Scale.X, ref mat.M11);
        return mat;
    }
}
