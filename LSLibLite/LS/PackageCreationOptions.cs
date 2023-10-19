using LSLibLite.LS.Enums;

namespace LSLibLite.LS;

public class PackageCreationOptions
{
    #region Members

    public const PackageVersion Version = PackageVersion.V16;
    public const CompressionMethod Compression = CompressionMethod.None;
    public const bool FastCompression = true;
    public const PackageFlags Flags = 0;
    public const byte Priority = 0;

    #endregion
}