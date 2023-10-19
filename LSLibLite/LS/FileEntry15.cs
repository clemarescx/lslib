using System.Runtime.InteropServices;

namespace LSLibLite.LS;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct FileEntry15
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
    public byte[] Name;

    public UInt64 OffsetInFile;
    public UInt64 SizeOnDisk;
    public UInt64 UncompressedSize;
    public UInt32 ArchivePart;
    public UInt32 Flags;
    public UInt32 Crc;
    public UInt32 Unknown2;
}