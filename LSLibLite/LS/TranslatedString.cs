namespace LSLibLite.LS;

public class TranslatedString
{
    #region Members

    public string? Value;
    public string? Handle;
    public ushort Version = 0;

    #endregion

    public override string ToString()
    {
        return Value;
    }
}