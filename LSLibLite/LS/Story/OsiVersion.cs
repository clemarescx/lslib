namespace LSLibLite.LS.Story;

/// <summary>
///     Osiris file format version numbers
/// </summary>
public static class OsiVersion
{
    #region Members

    /// <summary>
    ///     Initial version
    /// </summary>
    public const uint VerInitial = 0x0100;

    /// <summary>
    ///     Added Init/Exit calls to goals
    /// </summary>
    public const uint VerAddInitExitCalls = 0x0101;

    /// <summary>
    ///     Added version string at the beginning of the OSI file
    /// </summary>
    public const uint VerAddVersionString = 0x0102;

    /// <summary>
    ///     Added debug flags in the header
    /// </summary>
    public const uint VerAddDebugFlags = 0x0103;

    /// <summary>
    ///     Started scrambling strings by xor-ing with 0xAD
    /// </summary>
    public const uint VerScramble = 0x0104;

    /// <summary>
    ///     Added custom (string) types
    /// </summary>
    public const uint VerAddTypeMap = 0x0105;

    /// <summary>
    ///     Added Query nodes
    /// </summary>
    public const uint VerAddQuery = 0x0106;

    /// <summary>
    ///     Types can be aliases of any builtin type, not just strings
    /// </summary>
    public const uint VerTypeAliases = 0x0109;

    /// <summary>
    ///     Added INT64, GUIDSTRING types
    /// </summary>
    public const uint VerEnhancedTypes = 0x010a;

    /// <summary>
    ///     Added external string table
    /// </summary>
    public const uint VerExternalStringTable = 0x010b;

    /// <summary>
    ///     Removed external string table
    /// </summary>
    public const uint VerRemoveExternalStringTable = 0x010c;

    /// <summary>
    ///     Added enumerations
    /// </summary>
    public const uint VerEnums = 0x010d;

    /// <summary>
    ///     Last supported Osi version
    /// </summary>
    public const uint VerLastSupported = VerEnums;

    #endregion
}