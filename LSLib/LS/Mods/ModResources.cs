using LSLib.LS.Story.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace LSLib.LS;

public class ModInfo
{
    public string Name;
    public AbstractFileInfo Meta;
    public readonly Dictionary<string, AbstractFileInfo> Scripts = new();
    public readonly Dictionary<string, AbstractFileInfo> Stats = new();
    public readonly Dictionary<string, AbstractFileInfo> Globals = new();
    public readonly Dictionary<string, AbstractFileInfo> LevelObjects = new();
    public AbstractFileInfo OrphanQueryIgnoreList;
    public AbstractFileInfo StoryHeaderFile;
    public AbstractFileInfo TypeCoercionWhitelistFile;

    public ModInfo(string name)
    {
        Name = name;
    }
}

public sealed class ModResources : IDisposable
{
    public readonly Dictionary<string, ModInfo> Mods = new();
    public readonly List<PackageReader> LoadedPackages = new();

    public void Dispose()
    {
        LoadedPackages.ForEach(p => p.Dispose());
        LoadedPackages.Clear();
    }
}

public class ModPathVisitor
{
    // Pattern for excluding subsequent parts of a multi-part archive

    private readonly ModResources Resources;

    public bool CollectStoryGoals = false;
    public bool CollectStats = false;
    public bool CollectGlobals = false;
    public bool CollectLevels = false;
    public bool LoadPackages = true;
    public TargetGame Game = TargetGame.DOS2;

    public ModPathVisitor(ModResources resources)
    {
        Resources = resources;
    }

    private static void EnumerateFiles(
        ICollection<string> paths,
        string rootPath,
        string currentPath,
        string pattern)
    {
        foreach (var filePath in Directory.GetFiles(currentPath, pattern))
        {
            var relativePath = filePath[rootPath.Length..];
            if (relativePath[0] == '/' || relativePath[0] == '\\')
            {
                relativePath = relativePath[1..];
            }

            paths.Add(relativePath);
        }

        foreach (var directoryPath in Directory.GetDirectories(currentPath))
        {
            EnumerateFiles(paths, rootPath, directoryPath, pattern);
        }
    }

    private ModInfo GetMod(string modName)
    {
        if (!Resources.Mods.TryGetValue(modName, out var mod))
        {
            mod = new ModInfo(modName);
            Resources.Mods[modName] = mod;
        }

        return mod;
    }

    private void AddMetadataToMod(string modName, AbstractFileInfo file)
    {
        GetMod(modName).Meta = file;
    }

    private void AddStatToMod(string modName, string path, AbstractFileInfo file)
    {
        GetMod(modName).Stats[path] = file;
    }

    private void AddScriptToMod(string modName, string scriptName, AbstractFileInfo file)
    {
        GetMod(modName).Scripts[scriptName] = file;
    }

    private void AddGlobalsToMod(string modName, string path, AbstractFileInfo file)
    {
        GetMod(modName).Globals[path] = file;
    }

    private void AddLevelObjectsToMod(string modName, string path, AbstractFileInfo file)
    {
        GetMod(modName).LevelObjects[path] = file;
    }

    private void DiscoverPackagedFile(AbstractFileInfo file)
    {
        if (file.IsDeletion())
        {
            return;
        }

        if (file.Name.EndsWith("meta.lsx", StringComparison.Ordinal))
        {
            var match = Regexes.MetaRegex.Match(file.Name);
            if (match is { Success: true })
            {
                AddMetadataToMod(match.Groups[1].Value, file);
            }
        }

        if (CollectStoryGoals)
        {
            if (file.Name.EndsWith(".txt", StringComparison.Ordinal) && file.Name.Contains("/Story/RawFiles/Goals"))
            {
                var match = Regexes.ScriptRegex.Match(file.Name);
                if (match is { Success: true })
                {
                    AddScriptToMod(match.Groups[1].Value, match.Groups[2].Value, file);
                }
            }

            if (file.Name.EndsWith("/Story/story_orphanqueries_ignore_local.txt", StringComparison.Ordinal))
            {
                var match = Regexes.OrphanQueryIgnoresRegex.Match(file.Name);
                if (match is { Success: true })
                {
                    GetMod(match.Groups[1].Value).OrphanQueryIgnoreList = file;
                }
            }

            if (file.Name.EndsWith("/Story/RawFiles/story_header.div", StringComparison.Ordinal))
            {
                var match = Regexes.StoryDefinitionsRegex.Match(file.Name);
                if (match is { Success: true })
                {
                    GetMod(match.Groups[1].Value).StoryHeaderFile = file;
                }
            }

            if (file.Name.EndsWith("/Story/RawFiles/TypeCoercionWhitelist.txt", StringComparison.Ordinal))
            {
                var match = Regexes.TypeCoercionWhitelistRegex.Match(file.Name);
                if (match is { Success: true })
                {
                    GetMod(match.Groups[1].Value).TypeCoercionWhitelistFile = file;
                }
            }
        }

        if (CollectStats && file.Name.EndsWith(".txt", StringComparison.Ordinal) && file.Name.Contains("/Stats/Generated/Data"))
        {
            var match = Regexes.StatRegex.Match(file.Name);
            if (match is { Success: true })
            {
                AddStatToMod(match.Groups[1].Value, match.Groups[2].Value, file);
            }
        }

        if (CollectGlobals && file.Name.EndsWith(".lsf", StringComparison.Ordinal) && file.Name.Contains("/Globals/"))
        {
            var match = Regexes.GlobalsRegex.Match(file.Name);
            if (match is { Success: true })
            {
                AddGlobalsToMod(match.Groups[1].Value, match.Groups[0].Value, file);
            }
        }

        if (CollectLevels && file.Name.EndsWith(".lsf", StringComparison.Ordinal) && file.Name.Contains("/Levels/"))
        {
            var match = Regexes.LevelObjectsRegex.Match(file.Name);
            if (match is { Success: true })
            {
                AddLevelObjectsToMod(match.Groups[1].Value, match.Groups[0].Value, file);
            }
        }
    }

    public void DiscoverPackage(string packagePath)
    {
        var reader = new PackageReader(packagePath);
        Resources.LoadedPackages.Add(reader);
        var package = reader.Read();

        foreach (var file in package.Files)
        {
            DiscoverPackagedFile(file);
        }
    }

    // List of packages we won't ever load
    // These packages don't contain any mod resources, but have a large
    // file table that makes loading unneccessarily slow.
    private readonly HashSet<string> _packageBlacklist = new()
    {
        "Assets.pak",
        "Effects.pak",
        "Engine.pak",
        "EngineShaders.pak",
        "Game.pak",
        "GamePlatform.pak",
        "Gustav_Textures.pak",
        "Icons.pak",
        "LowTex.pak",
        "Materials.pak",
        "Minimaps.pak",
        "Models.pak",
        "SharedSoundBanks.pak",
        "SharedSounds.pak",
        "Textures.pak",
        "VirtualTextures.pak"
    };

    private void DiscoverBuiltinPackages(string gameDataPath)
    {
        // Collect priority value from headers
        var packagePriorities = new List<(string Path, int Priority)>();

        foreach (var path in Directory.GetFiles(gameDataPath, "*.pak"))
        {
            var baseName = Path.GetFileName(path);
            if (!_packageBlacklist.Contains(baseName)
                // Don't load 2nd, 3rd, ... parts of a multi-part archive
             && !Regexes.ArchivePartRegex.IsMatch(baseName))
            {
                var reader = new PackageReader(path, true);
                var package = reader.Read();
                packagePriorities.Add((path, package.Metadata.Priority));
            }
        }

        packagePriorities.Sort(((string _, int Priority) a, (string _, int Priority) b) => a.Priority.CompareTo(b.Priority));

        // Load non-patch packages first
        foreach (var package in packagePriorities)
        {
            DiscoverPackage(package.Path);
        }
    }

    public void DiscoverUserPackages(string gameDataPath)
    {
        foreach (var packagePath in Directory.GetFiles(gameDataPath, "*.pak"))
        {
            // Don't load 2nd, 3rd, ... parts of a multi-part archive
            if (!Regexes.ArchivePartRegex.IsMatch(packagePath))
            {
                DiscoverPackage(packagePath);
            }
        }
    }

    private void DiscoverModGoals(string modName, string modPath)
    {
        var goalPath = $@"{modPath}\Story\RawFiles\Goals";
        if (!Directory.Exists(goalPath))
        {
            return;
        }

        List<string> goalFiles = new();
        EnumerateFiles(goalFiles, goalPath, goalPath, "*.txt");

        foreach (var goalFile in goalFiles)
        {
            var fileInfo = new FilesystemFileInfo
            {
                FilesystemPath = $"{goalPath}\\{goalFile}",
                Name = goalFile
            };

            AddScriptToMod(modName, goalFile, fileInfo);
        }
    }

    private void DiscoverModStats(string modName, string modPublicPath)
    {
        var statsPath = $@"{modPublicPath}\Stats\Generated\Data";
        if (!Directory.Exists(statsPath))
        {
            return;
        }

        List<string> statFiles = new();
        EnumerateFiles(statFiles, statsPath, statsPath, "*.txt");

        foreach (var statFile in statFiles)
        {
            var fileInfo = new FilesystemFileInfo
            {
                FilesystemPath = $"{statsPath}\\{statFile}",
                Name = statFile
            };

            AddStatToMod(modName, statFile, fileInfo);
        }
    }

    private void DiscoverModGlobals(string modName, string modPath)
    {
        var globalsPath = $@"{modPath}\Globals";
        if (!Directory.Exists(globalsPath))
        {
            return;
        }

        List<string> globalFiles = new();
        EnumerateFiles(globalFiles, globalsPath, globalsPath, "*.lsf");

        foreach (var globalFile in globalFiles)
        {
            var fileInfo = new FilesystemFileInfo
            {
                FilesystemPath = $"{globalsPath}\\{globalFile}",
                Name = globalFile
            };

            AddGlobalsToMod(modName, globalFile, fileInfo);
        }
    }

    private void DiscoverModLevelObjects(string modName, string modPath)
    {
        var levelsPath = $@"{modPath}\Levels";
        if (!Directory.Exists(levelsPath))
        {
            return;
        }

        List<string> levelFiles = new();
        EnumerateFiles(levelFiles, levelsPath, levelsPath, "*.lsf");

        foreach (var levelFile in levelFiles)
        {
            var fileInfo = new FilesystemFileInfo
            {
                FilesystemPath = $"{levelsPath}\\{levelFile}",
                Name = levelFile
            };

            AddLevelObjectsToMod(modName, levelFile, fileInfo);
        }
    }

    private void DiscoverModDirectory(string modName, string modPath, string publicPath)
    {
        // Trigger mod entry creation even if there are no resources
        GetMod(modName);

        if (CollectStoryGoals)
        {
            DiscoverModGoals(modName, modPath);

            var headerPath = $@"{modPath}\Story\RawFiles\story_header.div";
            if (File.Exists(headerPath))
            {
                var fileInfo = new FilesystemFileInfo
                {
                    FilesystemPath = headerPath,
                    Name = headerPath
                };

                GetMod(modName).StoryHeaderFile = fileInfo;
            }

            var orphanQueryIgnoresPath = $@"{modPath}\Story\story_orphanqueries_ignore_local.txt";
            if (File.Exists(orphanQueryIgnoresPath))
            {
                var fileInfo = new FilesystemFileInfo
                {
                    FilesystemPath = orphanQueryIgnoresPath,
                    Name = orphanQueryIgnoresPath
                };

                GetMod(modName).OrphanQueryIgnoreList = fileInfo;
            }

            var typeCoercionWhitelistPath = $@"{modPath}\Story\RawFiles\TypeCoercionWhitelist.txt";
            if (File.Exists(typeCoercionWhitelistPath))
            {
                var fileInfo = new FilesystemFileInfo
                {
                    FilesystemPath = typeCoercionWhitelistPath,
                    Name = typeCoercionWhitelistPath
                };

                GetMod(modName).TypeCoercionWhitelistFile = fileInfo;
            }
        }

        if (CollectStats)
        {
            DiscoverModStats(modName, publicPath);
        }

        if (CollectGlobals)
        {
            DiscoverModGlobals(modName, modPath);
        }

        if (CollectLevels)
        {
            DiscoverModLevelObjects(modName, modPath);
        }
    }

    public void DiscoverMods(string gameDataPath)
    {
        var modsPath = Path.Combine(gameDataPath, "Mods");
        var publicPath = Path.Combine(gameDataPath, "Public");

        if (!Directory.Exists(modsPath))
        {
            return;
        }

        var modPaths = Directory.EnumerateDirectories(modsPath);

        foreach (var modPath in modPaths)
        {
            if (File.Exists(Path.Combine(modPath, "meta.lsx")))
            {
                var modName = Path.GetFileNameWithoutExtension(modPath);
                var modPublicPath = Path.Combine(publicPath, Path.GetFileName(modPath));
                DiscoverModDirectory(modName, modPath, modPublicPath);
            }
        }
    }

    public void Discover(string gameDataPath)
    {
        if (LoadPackages)
        {
            DiscoverBuiltinPackages(gameDataPath);
        }

        DiscoverMods(gameDataPath);
    }
}