﻿using LSLibLite.LS.Enums;
using LSLibLite.LS.Resources;
using LSLibLite.LS.Resources.LSB;
using LSLibLite.LS.Resources.LSF;
using LSLibLite.LS.Resources.LSJ;
using LSLibLite.LS.Resources.LSX;

namespace LSLibLite.LS;

// ReSharper disable once UnusedType.Global
public class ResourceUtils
{
    #region Public Delegates

    public delegate void ProgressUpdateDelegate(string status, long numerator, long denominator);

    public delegate void ErrorDelegate(string path, Exception e);

    #endregion

    #region Members

    public readonly ErrorDelegate errorDelegate = delegate { };
    public readonly ProgressUpdateDelegate progressUpdate = delegate { };

    #endregion

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
            _       => throw new ArgumentException($"Unrecognized file extension: {extension}")
        };
    }

    public static Resource? LoadResource(string inputPath)
    {
        return LoadResource(inputPath, ExtensionToResourceFormat(inputPath));
    }

    public static Resource? LoadResource(string inputPath, ResourceFormat format)
    {
        using var stream = File.Open(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
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

    public static void SaveResource(
        Resource? resource,
        string outputPath,
        ResourceFormat format,
        ResourceConversionParameters conversionParams)
    {
        FileManager.TryToCreateDirectory(outputPath);

        using var file = File.Open(outputPath, FileMode.Create, FileAccess.Write);

        ILSWriter writer = format switch
        {
            ResourceFormat.LSX => new LSXWriter(file) { Version = conversionParams.LSX, PrettyPrint = ResourceConversionParameters.PrettyPrint },
            ResourceFormat.LSB => new LSBWriter(file),
            ResourceFormat.LSF => new LSFWriter(file)
            {
                Version = conversionParams.LSF,
                EncodeSiblingData = ResourceConversionParameters.LSFEncodeSiblingData,
                Compression = ResourceConversionParameters.Compression,
                CompressionLevel = ResourceConversionParameters.CompressionLevel
            },
            ResourceFormat.LSJ => new LSJWriter(file),
            _                  => throw new ArgumentException("Invalid resource format")
        };

        writer.Write(resource);
    }

    public void ConvertResources(
        string inputDir,
        string outputDir,
        ResourceFormat inputFormat,
        ResourceFormat outputFormat,
        ResourceConversionParameters conversionParams)
    {
        progressUpdate("Enumerating files ...", 0, 1);
        var paths = new List<string>();
        EnumerateFiles(paths, inputDir, inputDir, inputFormat);

        progressUpdate("Converting resources ...", 0, 1);
        for (var i = 0; i < paths.Count; i++)
        {
            var path = paths[i];
            var inPath = $"{inputDir}/{path}";
            var outPath = $"{outputDir}/{Path.ChangeExtension(path, outputFormat.ToString().ToLower())}";

            FileManager.TryToCreateDirectory(outPath);

            progressUpdate($"Converting: {inPath}", i, paths.Count);
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

    private static void EnumerateFiles(
        ICollection<string> paths,
        string rootPath,
        string currentPath,
        ResourceFormat format)
    {
        foreach (var filePath in Directory.GetFiles(currentPath).Where(filePath => IsA(filePath, format)))
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
            EnumerateFiles(paths, rootPath, directoryPath, format);
        }
    }
}