using System;
using System.Runtime.InteropServices;

namespace Buildingway.Utils.Interop.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0xE0)]
public unsafe struct BgObjectEx
{
    [FieldOffset(0x00)] public void** VirtualTable;
    [FieldOffset(0x38)] public ulong Flags;

    /// <summary>
    /// used in vanilla with Highlight Potential Targets, Housing object outlines. can be set by GameObject.Highlight
    /// </summary>
    /// <remarks>
    /// (&amp; 0xF0) >> 4 == ObjectHighlightColor
    /// &amp; 0x0F == other state (3 = Default)
    /// </remarks>
    [FieldOffset(0x89)] public byte HighlightFlags;
    
    /// <summary>
    /// 0-255 with 0 being fully visible
    /// </summary>
    [FieldOffset(0xCA)] public byte Alpha;

    public void Dtor()
    {
        fixed (BgObjectEx* thisPtr = &this)
        {
            var funcPtr = VirtualTable[0];
            var del = Marshal.GetDelegateForFunctionPointer<DtorDelegate>((IntPtr)funcPtr);
            del((IntPtr)thisPtr, (char)0);
        }
    }
    
    public void CleanupRender()
    {
        fixed (BgObjectEx* thisPtr = &this)
        {
            var funcPtr = VirtualTable[1];
            var del = Marshal.GetDelegateForFunctionPointer<CleanupDelegate>((IntPtr)funcPtr);
            del((IntPtr)thisPtr);
        }
    }
    
    public byte UpdateRender()
    {
        fixed (BgObjectEx* thisPtr = &this)
        {
            var funcPtr = VirtualTable[4];
            var del = Marshal.GetDelegateForFunctionPointer<UpdateDelegate>((IntPtr)funcPtr);
            return del((IntPtr)thisPtr);
        }
    }
    
    public void UpdateCulling()
    {
        fixed (BgObjectEx* thisPtr = &this)
        {
            var funcPtr = VirtualTable[8];
            var del = Marshal.GetDelegateForFunctionPointer<UpdateCullingDelegate>((IntPtr)funcPtr);
            del((IntPtr)thisPtr);
        }
    }
    
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate ulong DtorDelegate(IntPtr self, char a2);
    
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void CleanupDelegate(IntPtr self);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate byte UpdateDelegate(IntPtr self);
    
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate BgObjectEx* UpdateCullingDelegate(IntPtr self);
}
