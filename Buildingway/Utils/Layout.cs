using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Anyder;
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

        Placements = objectManager.Objects
                               .Where(x => x.Type != ObjectType.ActorVfx && x.Type != ObjectType.Invalid)
                               .Select(x =>
                               {
                                   var transform = x.GetTransform();
                                   var collide = x.Group is { Collide: true };
                                   return new Placement(x.Path, x.Name, transform!.Position, transform!.Rotation, transform!.Scale, collide);
                               }).ToList();
    }
}

public struct Placement
{
    public string Path { get; set; } = "";
    public string? Name { get; set; }
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public Vector3 Scale { get; set; }
    public bool Collision { get; set; }

    public Placement(string path, string name, Vector3 position, Quaternion rotation, Vector3 scale, bool collision)
    {
        Path = path;
        Name = name;
        Position = position;
        Rotation = rotation;
        Scale = scale;
        Collision = collision;
    }
    
    [JsonConstructor]
    public Placement() { }
}
