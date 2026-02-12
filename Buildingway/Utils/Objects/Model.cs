using System;
using Buildingway.Utils.Interop.Structs;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace Buildingway.Utils.Objects;

public unsafe class Model : IDisposable
{
    public readonly BgObject* BgObject;
    public string Path { get; private set; }

    public Vector3 Position
    {
        get => BgObject->Position;
        set => BgObject->Position = value;
    }

    public Quaternion Rotation
    {
        get => BgObject->Rotation;
        set => BgObject->Rotation = value;
    }

    public Vector3 Scale
    {
        get => BgObject->Scale;
        set => BgObject->Scale = value;
    }

    public Model(string path, Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null)
    {
        Plugin.Log.Verbose($"Creating BgObject {path}");
        if (Plugin.BgObjectFunctions == null) throw new NullReferenceException("BgObject functions are not initialized");
        
        Path = path;
        BgObject = Plugin.BgObjectFunctions.BgObjectCreate(path);
        
        if (position != null) Position = position.Value;
        if (rotation != null) Rotation = rotation.Value;
        if (scale != null) Scale = scale.Value;

        if (BgObject->ModelResourceHandle->LoadState == 7)
        {
            var ex = (BgObjectEx*)BgObject;
            ex->UpdateCulling();
        }
        else
        {
            Plugin.Framework.RunOnTick(TryFixCulling);
        }
    }

    public void SetAlpha(byte alpha)
    {
        var ex = (BgObjectEx*)BgObject;
        ex->Alpha = alpha;
        UpdateRender();
    }

    public void UpdateRender()
    {
        Plugin.Log.Verbose($"Updating BgObject {Path}");
        var ex = (BgObjectEx*)BgObject;
        ex->UpdateRender();
    }

    private void TryFixCulling()
    {
        Plugin.Log.Verbose($"Trying to fix BgObject culling {Path}");
        if (BgObject == null) return;
        
        if (BgObject->ModelResourceHandle->LoadState == 7)
        {
            var ex = (BgObjectEx*)BgObject;
            ex->UpdateCulling();
            return;
        }
        
        Plugin.Framework.RunOnTick(TryFixCulling);
    }

    public void Dispose()
    {
        Plugin.Log.Verbose($"Disposing BgObject {Path}");
        Plugin.Framework.RunOnFrameworkThread(() =>
        {
            if (BgObject == null) return;
        
            var ex = (BgObjectEx*) BgObject;
            ex->CleanupRender();
            ex->Dtor();
        });
    }
}
