using System;
using Buildingway.Utils.Interop.Structs;

namespace Buildingway.Utils.Objects.Vfx;

public abstract unsafe class BaseVfx : IDisposable
{
    public string Path = "";
    public DateTime Expires;
    public bool Loop = false;
    
    public VfxStruct* Vfx;
    public bool IsValid => Vfx != null && (IntPtr)Vfx != IntPtr.Zero;

    public abstract void Refresh();
    protected abstract void Remove();

    public void CheckForRefresh()
    {
        if (DateTime.Now >= Expires && Loop)
        {
            Plugin.Log.Verbose($"Refreshing Vfx {Path}");
            if (IsValid) Remove();
            Refresh();
        }
    }
    
    public void Dispose()
    {
        Plugin.Log.Verbose($"Disposing Vfx {Path}");
        
        try
        {
            if (Plugin.VfxFunctions == null) throw new NullReferenceException("Vfx functions are not initialized");
            if (IsValid) Remove();
            Vfx = null;
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, $"Error while trying to dispose {Path}");
        }
        
        GC.SuppressFinalize(this);
    }
}
