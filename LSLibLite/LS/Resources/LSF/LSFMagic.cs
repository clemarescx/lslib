using System.Runtime.InteropServices;

namespace LSLibLite.LS.Resources.LSF;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSFMagic
{
    /// <summary>
    ///     LSOF file signature
    /// </summary>
    public static readonly byte[] Signature = "LSOF"u8.ToArray();

    /// <summary>
    ///     LSOF file signature; should be the same as LSFHeader.Signature
    /// </summary>
    public UInt32 Magic;

    /// <summary>
    ///     Version of the LSOF file; D:OS EE is version 1/2, D:OS 2 is version 3
    /// </summary>
    public UInt32 Version;
}