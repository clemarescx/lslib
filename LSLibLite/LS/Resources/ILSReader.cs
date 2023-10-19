namespace LSLibLite.LS.Resources;

public interface ILSReader : IDisposable
{
    Resource? Read();
}