using System;

namespace LSLib.LS;

public interface ILSReader : IDisposable
{
    Resource Read();
}