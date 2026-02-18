using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Anyder.Objects;
using Anyder.Objects.Vfx;
// using Buildingway.Utils.Objects;
// using Buildingway.Utils.Objects.Vfx;

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
        var vfx    = objectManager.Vfx.OfType<StaticVfx>().Select(x => new Placement(x.Path, x.Transform.Position, x.Transform.Rotation, x.Transform.Scale, false));
        
        Placements = models.Concat(groups).Concat(vfx).ToList();
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
