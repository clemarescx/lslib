using System.Runtime.InteropServices;

namespace LSLibLite.LS;

[StructLayout(LayoutKind.Sequential)]
public struct LSMetadata
{
    public const uint CurrentMajorVersion = 33;

    public UInt64 Timestamp;
    public UInt32 MajorVersion;
    public UInt32 MinorVersion;
    public UInt32 Revision;
    public UInt32 BuildNumber;
}