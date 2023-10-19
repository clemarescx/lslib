using System.Runtime.InteropServices;

namespace LSLibLite.LS;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LocaHeader
{
    public const uint DefaultSignature = 0x41434f4c; // 'LOCA'

    public UInt32 Signature;
    public UInt32 NumEntries;
    public UInt32 TextsOffset;
}