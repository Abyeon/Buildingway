using System;
using System.Text;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;

namespace Buildingway.Utils.Interop.Structs;

public unsafe class SharedGroupLayoutFunctions
{
    public delegate void FixGroupChildrenDelegate(SharedGroupLayoutInstance* self);
    public delegate void AssignResourceHandlerDelegate(SharedGroupLayoutInstance* self, byte* pathBytes);
    public delegate SharedGroupLayoutInstance* CtorDelegate(SharedGroupLayoutInstance* self);
    public delegate byte LoadSgbDelegate(SharedGroupLayoutInstance* self, byte* pathBytes);
    public delegate LayerManager* GetPreferredLayerManagerDelegate(LayoutManager* layoutManager);
    
    [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 33 F6 C7 41 ?? ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 71 ?? ?? ?? ?? 48 8B F9 48 89 71 ?? 89 71 ?? 48 89 71 ?? C7 41 ?? ?? ?? ?? ?? 48 83 C1 ?? E8 ?? ?? ?? ?? 48 89 77")]
    public CtorDelegate? CtorInternal = null;
    
    [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 48 8B 81 ?? ?? ?? ?? 48 8D 79")]
    public LoadSgbDelegate? LoadSgbInternal = null;
    
    [Signature("BA ?? ?? ?? ?? E9 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 4C 8B 81")]
    public GetPreferredLayerManagerDelegate? GetPreferredLayerManagerInternal = null;

    [Signature("48 89 5C 24 ?? 57 48 83 EC ?? 48 83 B9 ?? ?? ?? ?? 00 48 8B DA 48 8B F9 0F 85")]
    public AssignResourceHandlerDelegate? AssignResourceInternal = null;
    
    [Signature("40 55 57 41 55 48 8D 6C 24 ?? 48 81 EC ?? ?? ?? ?? 48 8B F9")]
    public FixGroupChildrenDelegate? FixGroupChildrenInternal = null;
    
    
    public SharedGroupLayoutFunctions()
    {
        Plugin.GameInteropProvider.InitializeFromAttributes(this);
    }

    public SharedGroupLayoutInstance* Ctor(SharedGroupLayoutInstance* self)
    {
        if (CtorInternal == null)
            throw new InvalidOperationException("Ctor sig was not found!");
        
        return CtorInternal(self);
    }

    public byte LoadSgb(SharedGroupLayoutInstance* self, string path)
    {
        if (LoadSgbInternal == null)
            throw new InvalidOperationException("LoadSgb sig was not found!");

        var bytes = Encoding.UTF8.GetBytes(path + "\0");
        fixed (byte* pathPtr = bytes)
        { 
            return LoadSgbInternal(self, pathPtr);
        }
    }

    public void FixGroupChildren(SharedGroupLayoutInstance* self)
    {
        if (FixGroupChildrenInternal == null)
            throw new InvalidOperationException("FixGroup sig was not found!");
        
        FixGroupChildrenInternal(self);
    }

    public void AssignResource(SharedGroupLayoutInstance* self, string path)
    {
        if (AssignResourceInternal == null)
            throw new InvalidOperationException("AssignResource sig was not found!");
        
        var bytes = Encoding.UTF8.GetBytes(path + "\0");
        fixed (byte* pathPtr = bytes)
        { 
            AssignResourceInternal(self, pathPtr);
        }
    }

    public LayerManager* GetPreferredLayerManager(LayoutManager* self)
    {
        if (GetPreferredLayerManagerInternal == null)
            throw new InvalidOperationException("GetPreferredLayerManager sig was not found!");
        
        return GetPreferredLayerManagerInternal(self);
    }
}
