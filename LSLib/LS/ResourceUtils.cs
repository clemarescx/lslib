using System;
using System.Collections.Generic;
using System.IO;
using LSLib.LS.Enums;

namespace LSLib.LS;

public class ResourceConversionParameters
{
    /// <summary>
    /// Format of generated PAK files
    /// </summary>
    public PackageVersion PAKVersion;

    /// <summary>
    /// Format of generated LSF files
    /// </summary>
    public LSFVersion LSF = LSFVersion.MaxWriteVersion;

    /// <summary>
    /// Store sibling/neighbour node data in LSF files (usually done by savegames only)
    /// </summary>
    public bool LSFEncodeSiblingData = false;

    /// <summary>
    /// Format of generated LSX files
    /// </summary>
    public LSXVersion LSX = LSXVersion.V4;

    /// <summary>
    /// Pretty-print (format) LSX/LSJ files
    /// </summary>
    public bool PrettyPrint = true;

    /// <summary>
    /// LSF/LSB compression method
    /// </summary>
    public CompressionMethod Compression = CompressionMethod.LZ4;

    /// <summary>
    /// LSF/LSB compression level (i.e. size/compression time tradeoff)
    /// </summary>
    public CompressionLevel CompressionLevel = CompressionLevel.DefaultCompression;

    public static ResourceConversionParameters FromGameVersion(Game game)
    {
        ResourceConversionParameters p = new()
        {
            PAKVersion = game.PAKVersion(),
            LSF = game.LSFVersion(),
            LSX = game.LSXVersion()
        };

        return p;
    }
}

public class ResourceUtils
{
    public delegate void ProgressUpdateDelegate(string status, long numerator, long denominator);
    public ProgressUpdateDelegate progressUpdate = delegate { };

    public delegate void ErrorDelegate(string path, Exception e);
    public ErrorDelegate errorDelegate = delegate { };

    public static ResourceFormat ExtensionToResourceFormat(string path)
    {
        var extension = Path.GetExtension(path).ToLower();

        return extension switch
        {
            ".lsx"  => ResourceFormat.LSX,
            ".lsb"  => ResourceFormat.LSB,
            ".lsf"  => ResourceFormat.LSF,
            ".lsfx" => ResourceFormat.LSF,
            ".lsbc" => ResourceFormat.LSF,
            ".lsbs" => ResourceFormat.LSF,
            ".lsj"  => ResourceFormat.LSJ,
            _       => throw new ArgumentException("Unrecognized file extension: " + extension)
        };
    }

    public static Resource LoadResource(string inputPath)
    {
        return LoadResource(inputPath, ExtensionToResourceFormat(inputPath));
    }

    public static Resource LoadResource(string inputPath, ResourceFormat format)
    {
        using (var stream = File.Open(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            return LoadResource(stream, format);
        }
    }

    public static Resource LoadResource(Stream stream, ResourceFormat format)
    {
        using ILSReader reader = format switch
        {
            ResourceFormat.LSX => new LSXReader(stream),
            ResourceFormat.LSB => new LSBReader(stream),
            ResourceFormat.LSF => new LSFReader(stream),
            ResourceFormat.LSJ => new LSJReader(stream),
            _                  => throw new ArgumentException("Invalid resource format")
        };

        return reader.Read();
    }

    public static void SaveResource(Resource resource, string outputPath, ResourceConversionParameters conversionParams)
    {
        SaveResource(resource, outputPath, ExtensionToResourceFormat(outputPath), conversionParams);
    }

    public static void SaveResource(
        Resource resource,
        string outputPath,
        ResourceFormat format,
        ResourceConversionParameters conversionParams)
    {
        FileManager.TryToCreateDirectory(outputPath);

        using var file = File.Open(outputPath, FileMode.Create, FileAccess.Write);

        ILSWriter writer = format switch
        {
            ResourceFormat.LSX => new LSXWriter(file) { Version = conversionParams.LSX, PrettyPrint = conversionParams.PrettyPrint },
            ResourceFormat.LSB => new LSBWriter(file),
            ResourceFormat.LSF => new LSFWriter(file)
            {
                Version = conversionParams.LSF,
                EncodeSiblingData = conversionParams.LSFEncodeSiblingData,
                Compression = conversionParams.Compression,
                CompressionLevel = conversionParams.CompressionLevel
            },
            ResourceFormat.LSJ => new LSJWriter(file) { PrettyPrint = conversionParams.PrettyPrint },
            _                  => throw new ArgumentException("Invalid resource format")
        };

        writer.Write(resource);
    }

    private static bool IsA(string path, ResourceFormat format)
    {
        var extension = Path.GetExtension(path).ToLower();
        return format switch
        {
            ResourceFormat.LSX => extension == ".lsx",
            ResourceFormat.LSB => extension == ".lsb",
            ResourceFormat.LSF => extension is ".lsf" or ".lsbc" or ".lsfx",
            ResourceFormat.LSJ => extension == ".lsj",
            _                  => false
        };
    }

    private void EnumerateFiles(
        List<string> paths,
        string rootPath,
        string currentPath,
        ResourceFormat format)
    {
        foreach (string filePath in Directory.GetFiles(currentPath))
        {
            if (IsA(filePath, format))
            {
                var relativePath = filePath.Substring(rootPath.Length);
                if (relativePath[0] == '/' || relativePath[0] == '\\')
                {
                    relativePath = relativePath.Substring(1);
                }

                paths.Add(relativePath);
            }
        }

        foreach (string directoryPath in Directory.GetDirectories(currentPath))
        {
            EnumerateFiles(paths, rootPath, directoryPath, format);
        }
    }

    public void ConvertResources(
        string inputDir,
        string outputDir,
        ResourceFormat inputFormat,
        ResourceFormat outputFormat,
        ResourceConversionParameters conversionParams)
    {
        this.progressUpdate("Enumerating files ...", 0, 1);
        var paths = new List<string>();
        EnumerateFiles(paths, inputDir, inputDir, inputFormat);

        this.progressUpdate("Converting resources ...", 0, 1);
        for (var i = 0; i < paths.Count; i++)
        {
            var path = paths[i];
            var inPath = inputDir + "/" + path;
            var outPath = outputDir + "/" + Path.ChangeExtension(path, outputFormat.ToString().ToLower());

            FileManager.TryToCreateDirectory(outPath);

            this.progressUpdate("Converting: " + inPath, i, paths.Count);
            try
            {
                var resource = LoadResource(inPath, inputFormat);
                SaveResource(resource, outPath, outputFormat, conversionParams);
            }
            catch (Exception ex)
            {
                errorDelegate(inPath, ex);
            }
        }
    }
}