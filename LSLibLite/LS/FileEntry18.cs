using System.Runtime.InteropServices;

namespace LSLibLite.LS;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct FileEntry18
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
    public byte[] Name;

    public UInt32 OffsetInFile1;
    public UInt16 OffsetInFile2;
    public Byte ArchivePart;
    public Byte Flags;
    public UInt32 SizeOnDisk;
    public UInt32 UncompressedSize;
}