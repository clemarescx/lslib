namespace LSLibLite.LS;

public class NotAPackageException : Exception
{
    #region Constructors

    public NotAPackageException(string message) : base(message) { }

    #endregion
}