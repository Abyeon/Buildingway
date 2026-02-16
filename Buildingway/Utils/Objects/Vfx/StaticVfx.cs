using System;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace Buildingway.Utils.Objects.Vfx;

/// <summary>
/// VFX that is not attached to an actor.
/// </summary>
public unsafe class StaticVfx : BaseVfx
{
    public Transform Transform;

    public StaticVfx(string path, Vector3 position, Quaternion rotation, Vector3 scale, TimeSpan? expiration = null, bool loop = false)
    {
        Plugin.Log.Verbose($"Creating StaticVfx {path}");
        if (Plugin.VfxFunctions == null) throw new NullReferenceException("Vfx functions are not initialized");

        Path = path;
        Transform = new Transform()
        {
            Position = position,
            Rotation = rotation,
            Scale = scale
        };
        
        Transform.OnUpdate += UpdateTransform;
        
        Loop = loop;
        Expires = expiration.HasValue ? DateTime.UtcNow + expiration.Value : DateTime.UtcNow + TimeSpan.FromSeconds(5);
        
        try
        {
            Vfx = Plugin.VfxFunctions.StaticVfxCreate(Path);
            Plugin.VfxFunctions.StaticVfxRun(Vfx);
                
            if (!IsValid)
                throw new Exception("Vfx pointer is null");
                
            UpdateTransform();
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Failed to create Vfx");
        }
    }

    private void UpdateTransform()
    {
        Vfx->Position = Transform.Position;
        Vfx->Scale = Transform.Scale;
        Vfx->Rotation = Transform.Rotation;
        Vfx->Flags |= 0x2;
    }

    public StaticVfx(string path, Vector3 position, Vector3 scale, float rotation, TimeSpan? expiration = null, bool loop = false)
        : this(path, position, Quaternion.CreateFromYawPitchRoll(rotation, 0f, 0f), scale, expiration, loop)
    { }

    public override void Refresh()
    {
        try
        {
            // if (IsValid) Plugin.VfxFunctions.StaticVfxRemove(Vfx);
            Vfx = Plugin.VfxFunctions.StaticVfxCreate(Path);
            Plugin.VfxFunctions.StaticVfxRun(Vfx);
                
            if (!IsValid)
                throw new Exception("Vfx pointer is null");
                
            UpdateTransform();
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Failed to create Vfx");
        }
    }

    protected override void Remove()
    {
        Plugin.VfxFunctions.StaticVfxRemove(Vfx);
    }
}
