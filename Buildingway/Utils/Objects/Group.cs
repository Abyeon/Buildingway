using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using FFXIVClientStructs.FFXIV.Client.System.Memory;

namespace Buildingway.Utils.Objects;

public unsafe class Group : IDisposable
{
    public SharedGroupLayoutInstance* Data;
    public string Path;
    
    public Transform Transform;
    
    public bool Collide;

    public Group(string path, Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null, bool collide = true)
    {
        Data = IMemorySpace.GetDefaultSpace()->Malloc<SharedGroupLayoutInstance>();
        Plugin.SharedGroupLayoutFunctions.Ctor(Data);
        
        Plugin.Log.Verbose($"Attempting to create group {path} @ {((IntPtr)Data):x8}");
        Path = path;

        Transform = new Transform()
        {
            Position = position ?? Vector3.Zero,
            Rotation = rotation ?? Quaternion.Identity,
            Scale = scale ?? Vector3.One
        };

        Transform.OnUpdate += UpdateTransform;
        
        Collide = collide;
        
        Plugin.Framework.RunOnTick(SetModel);
    }

    private void SetModel()
    {
        Plugin.SharedGroupLayoutFunctions.LoadSgb(Data, Path);
        
        UpdateTransform();

        var first = Data->Instances.Instances.First;
        var last = Data->Instances.Instances.Last;
        
        if (first != last)
        {
            Plugin.SharedGroupLayoutFunctions.FixGroupChildren(Data);
        }
    }

    private void UpdateTransform()
    {
        var t = Data->GetTransformImpl();
        t->Translation = Transform.Position;
        t->Rotation = Transform.Rotation;
        t->Scale = Transform.Scale;

        Data->SetTransformImpl(t);
        Data->SetColliderActive(Collide);
    }

    public void DrawInfo()
    {
        
    }
    
    public void Dispose()
    {
        Plugin.Log.Verbose($"Disposing group {Path}");
        if (Data == null) return;
        
        Data->Deinit();
        Data->Dtor(0);
        
        IMemorySpace.Free(Data);
        
        Data = null;
    }
}
