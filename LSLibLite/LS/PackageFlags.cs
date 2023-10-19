namespace LSLibLite.LS;

[Flags]
public enum PackageFlags
{
    /// <summary>
    ///     Allow memory-mapped access to the files in this archive.
    /// </summary>
    AllowMemoryMapping = 0x02,

    /// <summary>
    ///     All files are compressed into a single LZ4 stream
    /// </summary>
    Solid = 0x04,

    /// <summary>
    ///     Archive contents should be preloaded on game startup.
    /// </summary>
    Preload = 0x08
}