using System.Diagnostics.CodeAnalysis;
using LSLibLite.LS.Enums;

namespace LSLibLite.LS;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class Packager
{
    #region Public Delegates

    public delegate void ProgressUpdateDelegate(
        string status,
        long numerator,
        long denominator,
        AbstractFileInfo file);

    #endregion

    #region Members

    public readonly ProgressUpdateDelegate ProgressUpdate = delegate { };

    #endregion

    public void UncompressPackage(Package package, string outputPath, Func<AbstractFileInfo, bool>? filter = null)
    {
        if (outputPath.Length > 0 && !outputPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.InvariantCultureIgnoreCase))
        {
            outputPath += Path.DirectorySeparatorChar;
        }

        var files = package.Files;

        if (filter != null)
        {
            files = files.FindAll(obj => filter(obj));
        }

        var totalSize = files.Sum(p => (long)p.Size());
        long currentSize = 0;

        var buffer = new byte[32768];
        foreach (var file in files)
        {
            ProgressUpdate(file.Name, currentSize, totalSize, file);
            currentSize += (long)file.Size();

            if (file.IsDeletion())
            {
                continue;
            }

            var outPath = outputPath + file.Name;

            FileManager.TryToCreateDirectory(outPath);

            var inStream = file.MakeStream();

            try
            {
                using var inReader = new BinaryReader(inStream);
                using var outFile = File.Open(outPath, FileMode.Create, FileAccess.Write);
                int read;
                while ((read = inReader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    outFile.Write(buffer, 0, read);
                }
            }
            finally
            {
                file.ReleaseStream();
            }
        }
    }

    public void UncompressPackage(string packagePath, string outputPath, Func<AbstractFileInfo, bool>? filter = null)
    {
        ProgressUpdate("Reading package headers ...", 0, 1, null);
        using var reader = new PackageReader(packagePath);
        var package = reader.Read();
        UncompressPackage(package, outputPath, filter);
    }

    public void CreatePackage(string packagePath, string inputPath)
    {
        FileManager.TryToCreateDirectory(packagePath);

        ProgressUpdate("Enumerating files ...", 0, 1, null);
        var package = CreatePackageFromPath(inputPath);
        package.Metadata.Flags = PackageCreationOptions.Flags;
        package.Metadata.Priority = PackageCreationOptions.Priority;

        ProgressUpdate("Creating archive ...", 0, 1, null);
        using var writer = new PackageWriter(package, packagePath);
        writer.WriteProgress += WriteProgressUpdate;
        writer.Version = PackageCreationOptions.Version;
        writer.Compression = PackageCreationOptions.Compression;
        writer.CompressionLevel = PackageCreationOptions.FastCompression
            ? CompressionLevel.FastCompression
            : CompressionLevel.DefaultCompression;

        writer.Write();
    }

    private void WriteProgressUpdate(AbstractFileInfo file, long numerator, long denominator)
    {
        ProgressUpdate(file.Name, numerator, denominator, file);
    }

    private static Package CreatePackageFromPath(string path)
    {
        var package = new Package();

        if (!path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.InvariantCultureIgnoreCase))
        {
            path += Path.DirectorySeparatorChar;
        }

        var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                             .Select(filePath => (fileName: filePath.Replace(path, string.Empty), path: filePath));

        foreach (var (fileName, filePath) in files)
        {
            var fileInfo = FilesystemFileInfo.CreateFromEntry(filePath, fileName);
            package.Files.Add(fileInfo);
            fileInfo.Dispose();
        }

        return package;
    }
}