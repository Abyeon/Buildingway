using System.Runtime.InteropServices;

namespace Buildingway.Utils.Interop.Structs;

// Thanks to ICritical
// https://github.com/Critical-Impact/CriticalCommonLib/blob/main/GameStructs/HousingTerritory2.cs#L6

[StructLayout(LayoutKind.Explicit, Size = 41376)]
public unsafe struct HousingTerritory2 {
    [FieldOffset(38560)] public ulong HouseId;
    public uint TerritoryTypeId => (uint)((HouseId >> 32) & 0xFFFF);
}
