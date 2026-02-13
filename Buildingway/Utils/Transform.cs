using System.Numerics;
using Dalamud.Bindings.ImGuizmo;

namespace Buildingway.Utils;

public struct Transform(Vector3 position, Vector3 scale, Quaternion rotation)
{
    public Vector3 Position = position;
    public Vector3 Scale = scale;
    public Quaternion Rotation = rotation;
    
    public Matrix4x4 GetTransformation()
    {
        Matrix4x4 scale       = Matrix4x4.CreateScale(Scale);
        Matrix4x4 rotation    = Matrix4x4.CreateFromQuaternion(Rotation);
        Matrix4x4 translation = Matrix4x4.CreateTranslation(Position);
        
        Matrix4x4 mat = Matrix4x4.Multiply(scale, rotation);
        mat = Matrix4x4.Multiply(mat, translation);
        return mat;
    }
}
