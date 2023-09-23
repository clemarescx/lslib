using System.Text.RegularExpressions;

namespace LSLib.LS;

public static partial class Regexes
{
    public static readonly Regex VersionRegex = VersionRegexGenerator();
    public static readonly Regex MetaRegex = MetaRegexGenerator();

    public static readonly Regex ScriptRegex = ScriptRegexGenerator();

    public static readonly Regex StatRegex = StatRegexGenerator();

    public static readonly Regex OrphanQueryIgnoresRegex = OrphanQueryIgnoresRegexGenerator();

    public static readonly Regex StoryDefinitionsRegex = StoryDefinitionsRegexGenerator();

    public static readonly Regex TypeCoercionWhitelistRegex = TypeCoercionWhitelistRegexGenerator();

    public static readonly Regex GlobalsRegex = GlobalsRegexGenerator();

    public static readonly Regex LevelObjectsRegex = LevelObjectsRegexGenerator();

    public static readonly Regex ArchivePartRegex = ArchivePartRegexGenerator();

    [GeneratedRegex("^Mods/([^/]+)/meta\\.lsx$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex MetaRegexGenerator();
    [GeneratedRegex("^Mods/([^/]+)/Story/RawFiles/Goals/(.*\\.txt)$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex ScriptRegexGenerator();
    [GeneratedRegex("^Public/([^/]+)/Stats/Generated/Data/(.*\\.txt)$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex StatRegexGenerator();
    [GeneratedRegex("^Mods/([^/]+)/Story/story_orphanqueries_ignore_local\\.txt$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex OrphanQueryIgnoresRegexGenerator();
    [GeneratedRegex("^Mods/([^/]+)/Story/RawFiles/story_header\\.div$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex StoryDefinitionsRegexGenerator();
    [GeneratedRegex("^Mods/([^/]+)/Story/RawFiles/TypeCoercionWhitelist\\.txt$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex TypeCoercionWhitelistRegexGenerator();
    [GeneratedRegex("^Mods/([^/]+)/Globals/.*/.*/.*\\.lsf$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex GlobalsRegexGenerator();
    [GeneratedRegex("^Mods/([^/]+)/Levels/.*/(Characters|Items|Triggers)/.*\\.lsf$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex LevelObjectsRegexGenerator();
    [GeneratedRegex("^(.*)_[0-9]+\\.pak$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex ArchivePartRegexGenerator();
    [GeneratedRegex("^([0-9]+)\\.([0-9]+)\\.([0-9]+)\\.([0-9]+)$")]
    private static partial Regex VersionRegexGenerator();
}