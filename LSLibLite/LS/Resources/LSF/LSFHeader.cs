using System.Runtime.InteropServices;

namespace LSLibLite.LS.Resources.LSF;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSFHeader
{
    /// <summary>
    ///     Possibly version number? (major, minor, rev, build)
    /// </summary>
    public Int32 EngineVersion;
}