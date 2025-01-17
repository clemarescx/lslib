﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace LSLib.LS;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LocaHeader
{
    public const uint DefaultSignature = 0x41434f4c; // 'LOCA'

    public UInt32 Signature;
    public UInt32 NumEntries;
    public UInt32 TextsOffset;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LocaEntry
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    public byte[] Key;

    public UInt16 Version;
    public UInt32 Length;

    public string KeyString
    {
        get
        {
            var nameLen = Array.FindIndex(Key, c => c == 0) is var nullIdx and not -1 
                ? nullIdx
                : Key.Length;

            return Encoding.UTF8.GetString(Key, 0, nameLen);
        }

        set
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            Key = new byte[64];
            Array.Clear(Key, 0, Key.Length);
            Array.Copy(bytes, Key, bytes.Length);
        }
    }
}

public class LocalizedText
{
    public string Key;
    public ushort Version;
    public string Text;
}

public class LocaResource
{
    public List<LocalizedText> Entries;
}

public sealed class LocaReader : IDisposable
{
    private Stream Stream;

    public LocaReader(Stream stream)
    {
        Stream = stream;
    }

    public void Dispose()
    {
        Stream.Dispose();
    }

    public LocaResource Read()
    {
        using var reader = new BinaryReader(Stream);
        var loca = new LocaResource
        {
            Entries = new List<LocalizedText>()
        };

        var header = BinUtils.ReadStruct<LocaHeader>(reader);

        if (header.Signature != (ulong)LocaHeader.DefaultSignature)
        {
            throw new InvalidDataException("Incorrect signature in localization file");
        }

        var entries = new LocaEntry[header.NumEntries];
        BinUtils.ReadStructs(reader, entries);

        Stream.Position = header.TextsOffset;
        foreach (var entry in entries)
        {
            var text = Encoding.UTF8.GetString(reader.ReadBytes((int)entry.Length - 1));
            loca.Entries.Add(
                new LocalizedText
                {
                    Key = entry.KeyString,
                    Version = entry.Version,
                    Text = text
                });

            reader.ReadByte();
        }

        return loca;
    }
}

public class LocaWriter
{
    private Stream stream;

    public LocaWriter(Stream stream)
    {
        this.stream = stream;
    }

    public void Write(LocaResource res)
    {
        using var writer = new BinaryWriter(stream);
        var header = new LocaHeader
        {
            Signature = LocaHeader.DefaultSignature,
            NumEntries = (uint)res.Entries.Count,
            TextsOffset = (uint)(Marshal.SizeOf(typeof(LocaHeader)) + Marshal.SizeOf(typeof(LocaEntry)) * res.Entries.Count)
        };

        BinUtils.WriteStruct(writer, ref header);

        var entries = new LocaEntry[header.NumEntries];
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = res.Entries[i];
            entries[i] = new LocaEntry
            {
                KeyString = entry.Key,
                Version = entry.Version,
                Length = (uint)Encoding.UTF8.GetByteCount(entry.Text) + 1
            };
        }

        BinUtils.WriteStructs(writer, entries);

        foreach (var entry in res.Entries)
        {
            var bin = Encoding.UTF8.GetBytes(entry.Text);
            writer.Write(bin);
            writer.Write((byte)0);
        }
    }
}

public sealed class LocaXmlReader : IDisposable
{
    private Stream stream;
    private XmlReader reader;
    private LocaResource resource;

    public LocaXmlReader(Stream stream)
    {
        this.stream = stream;
    }

    public void Dispose()
    {
        stream.Dispose();
    }

    private void ReadElement()
    {
        switch (reader.Name)
        {
            case "contentList":
                // Root element
                break;

            case "content":
                var key = reader["contentuid"];
                var version = reader["version"] != null
                    ? ushort.Parse(reader["version"])
                    : (ushort)1;

                var text = reader.ReadString();

                resource.Entries.Add(
                    new LocalizedText
                    {
                        Key = key,
                        Version = version,
                        Text = text
                    });

                break;

            default:
                throw new InvalidFormatException($"Unknown element encountered: {reader.Name}");
        }
    }

    public LocaResource Read()
    {
        resource = new LocaResource
        {
            Entries = new List<LocalizedText>()
        };

        using (reader = XmlReader.Create(stream))
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    ReadElement();
                }
            }
        }

        return resource;
    }
}

public class LocaXmlWriter
{
    private Stream stream;
    private XmlWriter writer;

    public LocaXmlWriter(Stream stream)
    {
        this.stream = stream;
    }

    public void Write(LocaResource res)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "\t"
        };

        using (writer = XmlWriter.Create(stream, settings))
        {
            writer.WriteStartElement("contentList");

            foreach (var entry in res.Entries)
            {
                writer.WriteStartElement("content");
                writer.WriteAttributeString("contentuid", entry.Key);
                writer.WriteAttributeString("version", entry.Version.ToString());
                writer.WriteString(entry.Text);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.Flush();
        }
    }
}

public enum LocaFormat
{
    Loca,
    Xml
}

public static class LocaUtils
{
    public static LocaFormat ExtensionToFileFormat(string path)
    {
        var extension = Path.GetExtension(path).ToLower();

        return extension switch
        {
            ".loca" => LocaFormat.Loca,
            ".xml"  => LocaFormat.Xml,
            _       => throw new ArgumentException($"Unrecognized file extension: {extension}")
        };
    }

    public static LocaResource Load(string inputPath)
    {
        return Load(inputPath, ExtensionToFileFormat(inputPath));
    }

    public static LocaResource Load(string inputPath, LocaFormat format)
    {
        using var stream = File.Open(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Load(stream, format);
    }

    public static LocaResource Load(Stream stream, LocaFormat format)
    {
        switch (format)
        {
            case LocaFormat.Loca:
            {
                using var reader = new LocaReader(stream);
                return reader.Read();
            }

            case LocaFormat.Xml:
            {
                using var reader = new LocaXmlReader(stream);
                return reader.Read();
            }

            default:
                throw new ArgumentException("Invalid loca format");
        }
    }

    public static void Save(LocaResource resource, string outputPath)
    {
        Save(resource, outputPath, ExtensionToFileFormat(outputPath));
    }

    public static void Save(LocaResource resource, string outputPath, LocaFormat format)
    {
        FileManager.TryToCreateDirectory(outputPath);

        using var file = File.Open(outputPath, FileMode.Create, FileAccess.Write);
        switch (format)
        {
            case LocaFormat.Loca:
            {
                var writer = new LocaWriter(file);
                writer.Write(resource);
                break;
            }

            case LocaFormat.Xml:
            {
                var writer = new LocaXmlWriter(file);
                writer.Write(resource);
                break;
            }

            default:
                throw new ArgumentException("Invalid loca format");
        }
    }
}