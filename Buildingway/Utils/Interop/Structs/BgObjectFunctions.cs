using System;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace Buildingway.Utils.Interop.Structs;

public unsafe class BgObjectFunctions
{
    public delegate BgObject* BgObjectCreateDelegate(string path, string pool, nint a3);
    
    [Signature("48 89 5C 24 ?? 57 48 83 EC ?? 49 8B D8 48 8B F9 4D 85 C0 75 ?? 48 8B 05")]
    public BgObjectCreateDelegate? BgObjectCreateInternal = null;
    
    public BgObjectFunctions()
    {
        Plugin.GameInteropProvider.InitializeFromAttributes(this);
    }
    
    public BgObject* BgObjectCreate(string path)
    {
        if (BgObjectCreateInternal == null)
            throw new InvalidOperationException($"BgObjectCreate sig was not found!");
        
        // return BgObjectCreateInternal(path, "Client.LayoutEngine.Layer.BgPartsLayoutInstance", 0);
        return BgObjectCreateInternal(path, "Client.System.Scheduler.Instance.BgObject", 0);
    }
}
