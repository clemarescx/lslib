using System.Runtime.InteropServices;

namespace LSLibLite.LS;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct FileEntry7
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
    public byte[] Name;

    public UInt32 OffsetInFile;
    public UInt32 SizeOnDisk;
    public UInt32 UncompressedSize;
    public UInt32 ArchivePart;
}