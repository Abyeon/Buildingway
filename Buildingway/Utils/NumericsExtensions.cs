using System;
using System.Numerics;

namespace Buildingway.Utils;

public static class NumericsExtensions
{
    public static Quaternion ToQuaternion(this Vector3 euler)
    {
        var cy = (float)Math.Cos(euler.Z * 0.5);
        var sy = (float)Math.Sin(euler.Z * 0.5);
        var cp = (float)Math.Cos(euler.Y * 0.5);
        var sp = (float)Math.Sin(euler.Y * 0.5);
        var cr = (float)Math.Cos(euler.X * 0.5);
        var sr = (float)Math.Sin(euler.X * 0.5);

        return new Quaternion
        {
            W = (cr * cp * cy + sr * sp * sy),
            X = (sr * cp * cy - cr * sp * sy),
            Y = (cr * sp * cy + sr * cp * sy),
            Z = (cr * cp * sy - sr * sp * cy)
        };
    }
    
    public static Vector3 ToEulerAngles(this Quaternion quat)
    {
        Vector3 angles = new();

        // roll / x
        double sinrCosp = 2 * (quat.W * quat.X + quat.Y * quat.Z);
        double cosrCosp = 1 - 2 * (quat.X * quat.X + quat.Y * quat.Y);
        angles.X = (float)Math.Atan2(sinrCosp, cosrCosp);

        // pitch / y
        double sinp = 2 * (quat.W * quat.Y - quat.Z * quat.X);
        if (Math.Abs(sinp) >= 1)
        {
            angles.Y = (float)Math.CopySign(Math.PI / 2, sinp);
        }
        else
        {
            angles.Y = (float)Math.Asin(sinp);
        }

        // yaw / z
        double sinyCosp = 2 * (quat.W * quat.Z + quat.X * quat.Y);
        double cosyCosp = 1 - 2 * (quat.Y * quat.Y + quat.Z * quat.Z);
        angles.Z = (float)Math.Atan2(sinyCosp, cosyCosp);

        return angles;
    }
}
