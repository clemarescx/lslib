using LSLibLite.LS.Enums;

namespace LSLibLite.LS;

public class ResourceConversionParameters
{
    #region Members

    /// <summary>
    ///     Store sibling/neighbour node data in LSF files (usually done by savegames only)
    /// </summary>
    public const bool LSFEncodeSiblingData = false;

    /// <summary>
    ///     Pretty-print (format) LSX/LSJ files
    /// </summary>
    public const bool PrettyPrint = true;

    /// <summary>
    ///     LSF/LSB compression method
    /// </summary>
    public const CompressionMethod Compression = CompressionMethod.LZ4;

    /// <summary>
    ///     LSF/LSB compression level (i.e. size/compression time tradeoff)
    /// </summary>
    public const CompressionLevel CompressionLevel = Enums.CompressionLevel.DefaultCompression;

    /// <summary>
    ///     Format of generated LSF files
    /// </summary>
    public LSFVersion LSF = LSFVersion.MaxWriteVersion;

    /// <summary>
    ///     Format of generated LSX files
    /// </summary>
    public LSXVersion LSX = LSXVersion.V4;

    /// <summary>
    ///     Format of generated PAK files
    /// </summary>
    public PackageVersion PAKVersion;

    #endregion

    public static ResourceConversionParameters FromGameVersion(Game game)
    {
        ResourceConversionParameters p = new()
        {
            PAKVersion = game.PAKVersion(),
            LSF = game.LSFVersion(),
            LSX = game.LSXVersion()
        };

        return p;
    }
}