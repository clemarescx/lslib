namespace LSLibLite.LS;

public abstract class AbstractFileInfo
{
    #region Members

    public string Name;

    #endregion

    public abstract ulong Size();
    public abstract uint CRC();
    public abstract Stream? MakeStream();
    public abstract void ReleaseStream();
    public abstract bool IsDeletion();
}