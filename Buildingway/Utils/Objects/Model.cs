using System;
using Buildingway.Utils.Interop.Structs;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace Buildingway.Utils.Objects;

public unsafe class Model : IDisposable
{
    public readonly BgObject* BgObject;
    public string Path;
    public Transform Transform;

    public Model(string path, Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null)
    {
        Plugin.Log.Verbose($"Creating BgObject {path}");
        if (Plugin.BgObjectFunctions == null) throw new NullReferenceException("BgObject functions are not initialized");
        
        Path = path;
        BgObject = Plugin.BgObjectFunctions.BgObjectCreate(path);
        
        Transform.Position = position ?? Vector3.Zero;
        Transform.Rotation = rotation ?? Quaternion.Identity;
        Transform.Scale = scale ?? Vector3.One;

        if (BgObject->ModelResourceHandle->LoadState == 7)
        {
            var ex = (BgObjectEx*)BgObject;
            ex->UpdateCulling();
            UpdateTransform();
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

    public void UpdateTransform()
    {
        BgObject->Position = Transform.Position;
        BgObject->Rotation = Transform.Rotation;
        BgObject->Scale = Transform.Scale;
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
