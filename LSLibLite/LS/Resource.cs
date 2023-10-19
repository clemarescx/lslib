namespace LSLibLite.LS;

public class Resource
{
    #region Members

    public readonly Dictionary<string, Region?> Regions = new();
    public LSMetadata Metadata;

    #endregion

    #region Constructors

    public Resource()
    {
        Metadata.MajorVersion = 3;
    }

    #endregion
}