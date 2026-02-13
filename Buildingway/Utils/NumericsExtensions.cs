using System;
using System.Numerics;

namespace Buildingway.Utils;

// yoinked from brio, which is yoinked from clientstructs (and the endless cycle of stealing continues)
// https://github.com/Etheirys/Brio/blob/1472d6142af2d6e42c7c14aba2e548cbb8084a38/Brio/Core/MathHelpers.cs#L7
public static class NumericsExtensions
{
    public const float DegreesToRadians = MathF.PI / 180.0f;
    public const float RadiansToDegrees = 180.0f / MathF.PI;
    
    public const float Deg2Rad = MathF.PI * 2.0f / 360.0f;
    public const float Rad2Deg = 1.0f / Deg2Rad;

    public static Vector3 ToEuler(this Quaternion r)
    {
        float yaw = MathF.Atan2(2.0f * (r.Y * r.W + r.X * r.Z), 1.0f - 2.0f * (r.X * r.X + r.Y * r.Y));
        float pitch = MathF.Asin(2.0f * (r.X * r.W - r.Y * r.Z));
        float roll = MathF.Atan2(2.0f * (r.X * r.Y + r.Z * r.W), 1.0f - 2.0f * (r.X * r.X + r.Z * r.Z));

        return new Vector3(yaw, pitch, roll) * RadiansToDegrees;
    }
    
    public static Quaternion ToEulerAngles(this Vector3 euler) => FromEulerRad(euler * Deg2Rad);
    
    public static Quaternion Normalize(Quaternion value)
    {
        var sqrMagnitude = value.X * value.X + value.Y * value.Y + value.Z * value.Z + value.W * value.W;

        var length = MathF.Sqrt(sqrMagnitude);
        if(length < float.Epsilon)
            return Quaternion.Identity;

        return new Quaternion(value.X / length, value.Y / length, value.Z / length, value.W / length);
    }
    
    public static Quaternion ToQuaternion(this Vector3 euler)
    {
        euler *= DegreesToRadians;
        Quaternion quaternion = Quaternion.CreateFromYawPitchRoll(euler.X, euler.Y, euler.Z);
        return Quaternion.Normalize(quaternion);
    }
    
    private static Quaternion FromEulerRad(Vector3 euler)
    {
        var halfX = euler.X * 0.5f;
        var cX = MathF.Cos(halfX);
        var sX = MathF.Sin(halfX);

        var halfY = euler.Y * 0.5f;
        var cY = MathF.Cos(halfY);
        var sY = MathF.Sin(halfY);

        var halfZ = euler.Z * 0.5f;
        var cZ = MathF.Cos(halfZ);
        var sZ = MathF.Sin(halfZ);

        var qX = new Quaternion(sX, 0.0f, 0.0f, cX);
        var qY = new Quaternion(0.0f, sY, 0.0f, cY);
        var qZ = new Quaternion(0.0f, 0.0f, sZ, cZ);

        return Normalize(qZ * qY * qX);
    }
}
