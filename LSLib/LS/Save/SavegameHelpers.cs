using LSLib.LS.Enums;
using LSLib.LS.Story;
using System;
using System.IO;
using System.Linq;

namespace LSLib.LS.Save;

public sealed class SavegameHelpers : IDisposable
{
    private readonly Package _package;
    private readonly PackageReader _reader;


    public SavegameHelpers(string path)
    {
        _reader = new PackageReader(path);
        _package = _reader.Read();
    }

    public void Dispose()
    {
        _reader.Dispose();
    }

    public Story.Story LoadStory()
    {
        var storyInfo = _package.Files.Find(p => p.Name == "StorySave.bin");
        if (storyInfo != null)
        {
            var resourceStream = storyInfo.MakeStream();
            try
            {
                return LoadStory(resourceStream);
            }
            finally
            {
                storyInfo.ReleaseStream();
            }
        }

        var globals = LoadGlobals();

        var storyNode = globals.Regions["Story"].Children["Story"][0];
        var storyStream = new MemoryStream(
            storyNode.Attributes["Story"].Value as byte[] ?? throw new InvalidOperationException("Cannot proceed with null Story node"));

        return LoadStory(storyStream);
    }

    public void ResaveStory(Story.Story story, Game game, string path)
    {
        // Re-package global.lsf/StorySave.bin
        var rewrittenPackage = new Package();
        var conversionParams = ResourceConversionParameters.FromGameVersion(game);

        var storyBin = _package.Files.Find(p => p.Name == "StorySave.bin");
        if (storyBin == null)
        {
            var globalsStream = ResaveStoryToGlobals(story, conversionParams);

            var globalsLsf = _package.Files.Find(p => p.Name.ToLowerInvariant() == "globals.lsf");
            var globalsRepacked = StreamFileInfo.CreateFromStream(globalsStream, globalsLsf.Name);
            rewrittenPackage.Files.Add(globalsRepacked);

            var files = _package.Files.Where(x => x.Name.ToLowerInvariant() != "globals.lsf").ToList();
            rewrittenPackage.Files.AddRange(files);
        }
        else
        {
            // Save story resource and pack into the Story.Story attribute in globals.lsf
            var storyStream = new MemoryStream();
            var storyWriter = new StoryWriter();
            storyWriter.Write(storyStream, story, true);
            storyStream.Seek(0, SeekOrigin.Begin);

            var storyRepacked = StreamFileInfo.CreateFromStream(storyStream, "StorySave.bin");
            rewrittenPackage.Files.Add(storyRepacked);

            var files = _package.Files.Where(x => x.Name != "StorySave.bin").ToList();
            rewrittenPackage.Files.AddRange(files);
        }

        using var packageWriter = new PackageWriter(rewrittenPackage, path);
        packageWriter.Version = conversionParams.PAKVersion;
        packageWriter.Compression = CompressionMethod.Zlib;
        packageWriter.CompressionLevel = CompressionLevel.DefaultCompression;
        packageWriter.Write();
    }

    private Resource LoadGlobals()
    {
        var globalsInfo = _package.Files.Find(p => p.Name.ToLowerInvariant() == "globals.lsf");
        if (globalsInfo == null)
        {
            throw new InvalidDataException("The specified package is not a valid savegame (globals.lsf not found)");
        }

        Resource resource;
        var resourceStream = globalsInfo.MakeStream();
        try
        {
            using var resourceReader = new LSFReader(resourceStream);
            resource = resourceReader.Read();
        }
        finally
        {
            globalsInfo.ReleaseStream();
        }

        return resource;
    }

    private static Story.Story LoadStory(Stream s) => new StoryReader().Read(s);

    private MemoryStream ResaveStoryToGlobals(Story.Story story, ResourceConversionParameters conversionParams)
    {
        var globals = LoadGlobals();

        // Save story resource and pack into the Story.Story attribute in globals.lsf
        using (var storyStream = new MemoryStream())
        {
            var storyWriter = new StoryWriter();
            storyWriter.Write(storyStream, story, true);

            var storyNode = globals.Regions["Story"].Children["Story"][0];
            storyNode.Attributes["Story"].Value = storyStream.ToArray();
        }

        // Save globals.lsf
        var rewrittenStream = new MemoryStream();
        var resourceWriter = new LSFWriter(rewrittenStream)
        {
            Version = conversionParams.LSF,
            EncodeSiblingData = false
        };

        resourceWriter.Write(globals);
        rewrittenStream.Seek(0, SeekOrigin.Begin);
        return rewrittenStream;
    }
}