using System.Collections.Immutable;
using LSLibLite.LS.Enums;

namespace LSLibLite.LS;

public class Package
{
    #region Members

    public const PackageVersion CurrentVersion = PackageVersion.V18;

    public static readonly ImmutableArray<byte> Signature = "LSPK"u8.ToImmutableArray();
    public readonly List<AbstractFileInfo> Files = new();

    public readonly PackageMetadata Metadata = new();
    public PackageVersion Version;

    #endregion

    public static string MakePartFilename(string path, int part)
    {
        var dirName = Path.GetDirectoryName(path);
        var baseName = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        return $"{dirName}/{baseName}_{part}{extension}";
    }
}