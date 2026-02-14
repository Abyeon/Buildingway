using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Buildingway.Utils.Objects;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;

namespace Buildingway.Utils;

public class Layout
{
    public uint ZoneId { get; set; }
    public Vector3 StartPosition { get; set; }
    public List<Placement> Placements { get; set; } = null!;

    [JsonConstructor]
    public Layout() { }

    public Layout(ObjectManager objectManager)
    {
        if (Plugin.ObjectTable.LocalPlayer == null) throw new NullReferenceException("Player is null, cannot create layout!");
        var player = Plugin.ObjectTable.LocalPlayer;
        
        ZoneId = Plugin.ClientState.TerritoryType;
        StartPosition = player.Position;
        
        var models = objectManager.Models.Select(x => new Placement(x.Path, x.Transform.Position, x.Transform.Rotation, x.Transform.Scale, false));
        var groups = objectManager.Groups.Select(x => new Placement(x.Path, x.Transform.Position, x.Transform.Rotation, x.Transform.Scale, x.Collide));
        
        // need to handle vfx differently later
        // var vfx = objectManager.Vfx.Select(x => new Placement(x.Path, x.Transform.Position, x.Transform.Rotation, x.Transform.Scale)).ToList();
        
        Placements = models.Concat(groups).ToList();
    }
}

public struct Placement(string path, Vector3 position, Quaternion rotation, Vector3 scale, bool collision)
{
    public string Path { get; set; } = path;
    public Vector3 Position { get; set; } = position;
    public Quaternion Rotation { get; set; } = rotation;
    public Vector3 Scale { get; set; } = scale;
    public bool Collision { get; set; } = collision;
}

// public struct TransformData(Vector3 position, Quaternion rotation, Vector3 scale)
// {
//     public Vec3 Position { get; set; } = new(position);
//     public Vec4 Rotation { get; set; } = new(rotation);
//     public Vec3 Scale { get; set; }    = new(scale);
// }
//
// public struct Vec3(Vector3 vector3)
// {
//     public float X { get; set; } = vector3.X;
//     public float Y { get; set; } = vector3.Y;
//     public float Z { get; set; } = vector3.Z;
//     
//     public Vector3 ToVector3() => new(X, Y, Z);
// }
//
// public struct Vec4(Quaternion quaternion)
// {
//     public float X { get; set; } = quaternion.X;
//     public float Y { get; set; } = quaternion.Y;
//     public float Z { get; set; } = quaternion.Z;
//     public float W { get; set; } = quaternion.W;
//     
//     public Quaternion ToQuaternion() => new(X, Y, Z, W);
// }
