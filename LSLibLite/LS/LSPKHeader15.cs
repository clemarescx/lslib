using System.Runtime.InteropServices;

namespace LSLibLite.LS;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSPKHeader15
{
    public UInt32 Version;
    public UInt64 FileListOffset;
    public UInt32 FileListSize;
    public Byte Flags;
    public Byte Priority;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public byte[] Md5;
}