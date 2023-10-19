namespace LSLibLite.LS;

public class PackageMetadata
{
    #region Members

    /// <summary>
    ///     Load priority. Packages with higher priority are loaded later (i.e. they override earlier packages).
    /// </summary>
    public byte Priority;

    /// <summary>
    ///     Package flags bitmask. Allowed values are in the PackageFlags enumeration.
    /// </summary>
    public PackageFlags Flags = 0;

    #endregion
}