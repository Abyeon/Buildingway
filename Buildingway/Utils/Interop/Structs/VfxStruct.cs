using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace Buildingway.Utils.Interop.Structs;

[StructLayout(LayoutKind.Explicit)]
public struct VfxStruct
{
    [FieldOffset( 0x38 )] public byte Flags;
    [FieldOffset( 0x50 )] public Vector3 Position;
    [FieldOffset( 0x60 )] public Quaternion Rotation;
    [FieldOffset( 0x70 )] public Vector3 Scale;

    [FieldOffset( 0x128 )] public int ActorCaster;
    [FieldOffset( 0x130 )] public int ActorTarget;

    [FieldOffset( 0x1B8 )] public int StaticCaster;
    [FieldOffset( 0x1C0 )] public int StaticTarget;
    
    [FieldOffset(0x260)] public byte Red;
    [FieldOffset(0x264)] public byte Green;
    [FieldOffset(0x268)] public byte Blue;
    [FieldOffset(0x26C)] public float Alpha;
}
