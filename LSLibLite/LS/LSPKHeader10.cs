using System.Runtime.InteropServices;

namespace LSLibLite.LS;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSPKHeader10
{
    public UInt32 Version;
    public UInt32 DataOffset;
    public UInt32 FileListSize;
    public UInt16 NumParts;
    public Byte Flags;
    public Byte Priority;
    public UInt32 NumFiles;
}