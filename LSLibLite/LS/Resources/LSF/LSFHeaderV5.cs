using System.Runtime.InteropServices;

namespace LSLibLite.LS.Resources.LSF;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSFHeaderV5
{
    /// <summary>
    ///     Possibly version number? (major, minor, rev, build)
    /// </summary>
    public Int64 EngineVersion;
}