using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace LSLibLite.LS;

[StructLayout(LayoutKind.Sequential)]
public struct LSBHeader
{
    /// <summary>
    ///     LSB file signature since BG3
    /// </summary>
    public static readonly ImmutableArray<byte> SignatureBG3 = "LSFM"u8.ToImmutableArray();

    /// <summary>
    ///     LSB signature up to FW3 (DOS2 DE)
    /// </summary>
    public const uint SignatureFW3 = 0x40000000;

    public UInt32 Signature;
    public UInt32 TotalSize;
    public UInt32 BigEndian;
    public UInt32 Unknown;
    public LSMetadata Metadata;
}