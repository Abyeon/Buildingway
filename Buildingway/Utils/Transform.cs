using System;
using System.Numerics;
using Dalamud.Bindings.ImGuizmo;

namespace Buildingway.Utils;

public class Transform
{
    public Vector3 Position;
    public Vector3 Scale;
    public Quaternion Rotation;

    public Transform(Vector3 position, Vector3 scale, Quaternion rotation)
    {
        Position = position;
        Scale = scale;
        Rotation = rotation;
    }

    public Transform() { }

    public Matrix4x4 GetTransformation()
    {
        Matrix4x4 scale       = Matrix4x4.CreateScale(Scale);
        Matrix4x4 rotation    = Matrix4x4.CreateFromQuaternion(Rotation);
        Matrix4x4 translation = Matrix4x4.CreateTranslation(Position);
        
        Matrix4x4 mat = Matrix4x4.Multiply(scale, rotation);
        mat = Matrix4x4.Multiply(mat, translation);
        return mat;
    }

    public void Update()
    {
        OnUpdate.Invoke();
    }
    
    public event Action OnUpdate = null!;
}
