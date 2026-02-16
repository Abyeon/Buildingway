using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Buildingway.Utils.Objects.Vfx;
using Dalamud.Game.ClientState;
using Dalamud.Plugin.Services;

namespace Buildingway.Utils.Objects;

/// <summary>
/// Manager for handling spawning and deleting multiple types of objects.
/// </summary>
public class ObjectManager : IDisposable
{
    public List<Model> Models = [];
    public List<Group> Groups = [];
    public List<BaseVfx> Vfx = [];
    
    private readonly IClientState ClientState;
    private readonly IFramework Framework;

    public ObjectManager(IClientState clientState, IFramework framework)
    {
        ClientState = clientState;
        ClientState.ZoneInit += ClientStateOnZoneInit;
        ClientState.Logout += ClientStateOnLogout;
        
        Framework = framework;
        Framework.Update += FrameworkOnUpdate;
    }

    public bool IsLoading = false;
    private Queue<Placement> placementQueue = new();
    private readonly List<Model> dirtyModels = [];

    private void FrameworkOnUpdate(IFramework framework)
    {
        if (IsLoading)
        {
            var placement = placementQueue.Dequeue();
            Add(placement.Path, placement.Position, placement.Rotation, placement.Scale, placement.Collision);
            
            if (placementQueue.Count == 0) IsLoading = false;
        }

        foreach (var model in dirtyModels.ToList())
        {
            if (model.Dirty)
            {
                model.TryFixCulling();
            }
            else
            {
                dirtyModels.Remove(model);
            }
        }
        
        foreach (var item in Vfx.ToList())
        {
            if (!item.Loop && DateTime.UtcNow >= item.Expires)
            {
                item.Dispose(); // triggers the detour, removing it from the list
            }
            else
            {
                item.CheckForRefresh();
            }
        }
    }

    private void ClientStateOnLogout(int type, int code) => Clear();
    private void ClientStateOnZoneInit(ZoneInitEventArgs obj) => Clear();

    /// <summary>
    /// Takes a path and determines what type of object to spawn.
    /// </summary>
    /// <param name="path">Game path to the file</param>
    /// <param name="position">Object position</param>
    /// <param name="rotation">Object rotation</param>
    /// <param name="scale">Object scale</param>
    /// <param name="collide">Object collision toggle</param>
    public void Add(string path, Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null, bool collide = false)
    {
        var ext = Path.GetExtension(path);
        
        var pos = position ?? Vector3.Zero;
        var rot  = rotation ?? Quaternion.Identity;
        var sca  = scale ?? Vector3.One;

        switch (ext)
        {
            case ".avfx":
                Add(new StaticVfx(path,  pos, sca, 0f, loop: true));
                break;
            case ".mdl":
                Add(new Model(path, pos, rot, sca));
                break;
            case ".sgb":
                Add(new Group(path, pos, rot, sca, collide));
                break;
            default:
                Plugin.Log.Error($"Unsupported extension {ext}");
                break;
        }
    }
    
    public void Add(Model model)
    {
        Models.Add(model);
        dirtyModels.Add(model);
    }
    
    public void Add(Group group)
    {
        Groups.Add(group);
    }
    
    public void Add(BaseVfx vfx)
    {
        Vfx.Add(vfx);
    }
    
    public void LoadLayout(Layout layout)
    {
        Clear();
        placementQueue = new Queue<Placement>(layout.Placements);
        IsLoading = true;
    }

    /// <summary>
    /// Clears tracked BgObjects.
    /// </summary>
    public void ClearModels()
    {
        foreach (var model in Models) model.Dispose();
        Models.Clear();
        dirtyModels.Clear();
    }

    /// <summary>
    /// Clears tracked SharedGroupLayouts.
    /// </summary>
    public void ClearGroups()
    {
        foreach (var group in Groups) group.Dispose();
        Groups.Clear();
    }

    /// <summary>
    /// Clears tracked Vfx objects.
    /// </summary>
    public void ClearVfx()
    {
        foreach (var vfx in Vfx.ToList())
        {
            vfx.Loop = false; // need to disable loop so vfx doesnt get re-enabled after disposing.. oops!
            vfx.Dispose();
        }
        Vfx.Clear();
    }

    /// <summary>
    /// Clears all currently tracked objects.
    /// </summary>
    public void Clear()
    {
        placementQueue.Clear();
        ClearModels();
        ClearGroups();
        ClearVfx();
    }

    public unsafe void InteropRemoved(IntPtr pointer)
    {
        foreach (var item in Vfx)
        {
            if ((IntPtr)item.Vfx == pointer)
            {
                if (item.Loop)
                {
                    item.Refresh();
                }
                else
                {
                    Vfx.Remove(item);
                }

                break;
            }
        }
    }

    public void Dispose() => Clear();
}
