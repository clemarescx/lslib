using System.Runtime.InteropServices;

namespace LSLibLite.LS;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSPKHeader7
{
    public UInt32 Version;
    public UInt32 DataOffset;
    public UInt32 NumParts;
    public UInt32 FileListSize;
    public Byte LittleEndian;
    public UInt32 NumFiles;
}