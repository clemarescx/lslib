﻿using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;
using LSLibLite.LS.Enums;
using CompressionLevel = LSLibLite.LS.Enums.CompressionLevel;

namespace LSLibLite.LS;

public static class BinUtils
{
    public static T ReadStruct<T>(BinaryReader reader, [CallerMemberName] string? caller = null) where T : struct
    {
        var count = Marshal.SizeOf(typeof(T));
        var readBuffer = reader.ReadBytes(count);
        var handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
        var outStruct = (T?)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
        handle.Free();
        return outStruct ?? throw new InvalidOperationException($"{caller}: failed deserializing struct of type {typeof(T).Name}");
    }

    public static void ReadStructs<T>(BinaryReader reader, T[] elements, [CallerMemberName] string? caller = null)
    {
        var elementSize = Marshal.SizeOf(typeof(T));
        var bytes = elementSize * elements.Length;
        var readBuffer = reader.ReadBytes(bytes);
        var handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
        var addr = handle.AddrOfPinnedObject();
        for (var i = 0; i < elements.Length; i++)
        {
            var elementAddr = new IntPtr(addr.ToInt64() + elementSize * i);
            elements[i] = Marshal.PtrToStructure<T>(elementAddr)
                       ?? throw new InvalidOperationException($"{caller}: failed deserializing array of struct type {typeof(T).Name}");
        }

        handle.Free();
    }

    public static void WriteStruct<T>(BinaryWriter writer, ref T inStruct) where T : notnull
    {
        var count = Marshal.SizeOf(typeof(T));
        var writeBuffer = new byte[count];
        var handle = GCHandle.Alloc(writeBuffer, GCHandleType.Pinned);
        Marshal.StructureToPtr(inStruct, handle.AddrOfPinnedObject(), true);
        handle.Free();
        writer.Write(writeBuffer);
    }

    public static void WriteStructs<T>(BinaryWriter writer, T[] elements) where T : notnull
    {
        var elementSize = Marshal.SizeOf(typeof(T));
        var bytes = elementSize * elements.Length;
        var writeBuffer = new byte[bytes];
        var handle = GCHandle.Alloc(writeBuffer, GCHandleType.Pinned);
        var addr = handle.AddrOfPinnedObject();
        for (var i = 0; i < elements.Length; i++)
        {
            var elementAddr = new IntPtr(addr.ToInt64() + elementSize * i);
            Marshal.StructureToPtr(elements[i], elementAddr, true);
        }

        handle.Free();
        writer.Write(writeBuffer);
    }

    public static NodeAttribute ReadAttribute(NodeAttribute.DataType type, BinaryReader reader)
    {
        var attr = new NodeAttribute(type);
        switch (type)
        {
            case NodeAttribute.DataType.DT_None:
                break;

            case NodeAttribute.DataType.DT_Byte:
                attr.Value = reader.ReadByte();
                break;

            case NodeAttribute.DataType.DT_Short:
                attr.Value = reader.ReadInt16();
                break;

            case NodeAttribute.DataType.DT_UShort:
                attr.Value = reader.ReadUInt16();
                break;

            case NodeAttribute.DataType.DT_Int:
                attr.Value = reader.ReadInt32();
                break;

            case NodeAttribute.DataType.DT_UInt:
                attr.Value = reader.ReadUInt32();
                break;

            case NodeAttribute.DataType.DT_Float:
                attr.Value = reader.ReadSingle();
                break;

            case NodeAttribute.DataType.DT_Double:
                attr.Value = reader.ReadDouble();
                break;

            case NodeAttribute.DataType.DT_IVec2:
            case NodeAttribute.DataType.DT_IVec3:
            case NodeAttribute.DataType.DT_IVec4:
                {
                    var columns = attr.GetColumns();
                    var vec = new int[columns];
                    for (var i = 0; i < columns; i++)
                    {
                        vec[i] = reader.ReadInt32();
                    }

                    attr.Value = vec;
                    break;
                }

            case NodeAttribute.DataType.DT_Vec2:
            case NodeAttribute.DataType.DT_Vec3:
            case NodeAttribute.DataType.DT_Vec4:
                {
                    var columns = attr.GetColumns();
                    var vec = new float[columns];
                    for (var i = 0; i < columns; i++)
                    {
                        vec[i] = reader.ReadSingle();
                    }

                    attr.Value = vec;
                    break;
                }

            case NodeAttribute.DataType.DT_Mat2:
            case NodeAttribute.DataType.DT_Mat3:
            case NodeAttribute.DataType.DT_Mat3x4:
            case NodeAttribute.DataType.DT_Mat4x3:
            case NodeAttribute.DataType.DT_Mat4:
                {
                    var columns = attr.GetColumns();
                    var rows = attr.GetRows();
                    var mat = new Matrix(rows, columns);
                    attr.Value = mat;

                    for (var col = 0; col < columns; col++)
                    {
                        for (var row = 0; row < rows; row++)
                        {
                            mat[row, col] = reader.ReadSingle();
                        }
                    }

                    break;
                }

            case NodeAttribute.DataType.DT_Bool:
                attr.Value = reader.ReadByte() != 0;
                break;

            case NodeAttribute.DataType.DT_ULongLong:
                attr.Value = reader.ReadUInt64();
                break;

            case NodeAttribute.DataType.DT_Long:
            case NodeAttribute.DataType.DT_Int64:
                attr.Value = reader.ReadInt64();
                break;

            case NodeAttribute.DataType.DT_Int8:
                attr.Value = reader.ReadSByte();
                break;

            case NodeAttribute.DataType.DT_UUID:
                attr.Value = new Guid(reader.ReadBytes(16));
                break;

            default:
                // Strings are serialized differently for each file format and should be
                // handled by the format-specific ReadAttribute()
                throw new InvalidFormatException($"ReadAttribute() not implemented for type {type}");
        }

        return attr;
    }

    public static void WriteAttribute(BinaryWriter writer, NodeAttribute attr)
    {
        if (attr.Value is not { } attrValue)
        {
            throw new Exception("NodeAttribute.Value is null");
        }

        switch (attr.Type)
        {
            case NodeAttribute.DataType.DT_None:
                break;

            case NodeAttribute.DataType.DT_Byte:
                writer.Write((byte)attrValue);
                break;

            case NodeAttribute.DataType.DT_Short:
                writer.Write((short)attrValue);
                break;

            case NodeAttribute.DataType.DT_UShort:
                writer.Write((ushort)attrValue);
                break;

            case NodeAttribute.DataType.DT_Int:
                writer.Write((int)attrValue);
                break;

            case NodeAttribute.DataType.DT_UInt:
                writer.Write((uint)attrValue);
                break;

            case NodeAttribute.DataType.DT_Float:
                writer.Write((float)attrValue);
                break;

            case NodeAttribute.DataType.DT_Double:
                writer.Write((double)attrValue);
                break;

            case NodeAttribute.DataType.DT_IVec2:
            case NodeAttribute.DataType.DT_IVec3:
            case NodeAttribute.DataType.DT_IVec4:
                foreach (var item in (int[])attrValue)
                {
                    writer.Write(item);
                }

                break;

            case NodeAttribute.DataType.DT_Vec2:
            case NodeAttribute.DataType.DT_Vec3:
            case NodeAttribute.DataType.DT_Vec4:
                foreach (var item in (float[])attrValue)
                {
                    writer.Write(item);
                }

                break;

            case NodeAttribute.DataType.DT_Mat2:
            case NodeAttribute.DataType.DT_Mat3:
            case NodeAttribute.DataType.DT_Mat3x4:
            case NodeAttribute.DataType.DT_Mat4x3:
            case NodeAttribute.DataType.DT_Mat4:
                {
                    var mat = (Matrix)attrValue;
                    for (var col = 0; col < mat.cols; col++)
                    {
                        for (var row = 0; row < mat.rows; row++)
                        {
                            writer.Write((float)mat[row, col]);
                        }
                    }

                    break;
                }

            case NodeAttribute.DataType.DT_Bool:
                writer.Write(
                    (byte)((bool)attrValue
                        ? 1
                        : 0));

                break;

            case NodeAttribute.DataType.DT_ULongLong:
                writer.Write((ulong)attrValue);
                break;

            case NodeAttribute.DataType.DT_Long:
            case NodeAttribute.DataType.DT_Int64:
                writer.Write((long)attrValue);
                break;

            case NodeAttribute.DataType.DT_Int8:
                writer.Write((sbyte)attrValue);
                break;

            case NodeAttribute.DataType.DT_UUID:
                writer.Write(((Guid)attrValue).ToByteArray());
                break;

            default:
                throw new InvalidFormatException($"WriteAttribute() not implemented for type {attr.Type}");
        }
    }

    public static CompressionMethod CompressionFlagsToMethod(byte flags)
    {
        return (flags & 0x0f) switch
        {
            (int)CompressionMethod.None => CompressionMethod.None,
            (int)CompressionMethod.Zlib => CompressionMethod.Zlib,
            (int)CompressionMethod.LZ4 => CompressionMethod.LZ4,
            _ => throw new ArgumentException("Invalid compression method")
        };
    }

    public static CompressionLevel CompressionFlagsToLevel(byte flags)
    {
        return (flags & 0xf0) switch
        {
            (int)CompressionFlags.FastCompress => CompressionLevel.FastCompression,
            (int)CompressionFlags.DefaultCompress => CompressionLevel.DefaultCompression,
            (int)CompressionFlags.MaxCompressionLevel => CompressionLevel.MaxCompression,
            _ => throw new ArgumentException("Invalid compression flags")
        };
    }

    public static byte MakeCompressionFlags(CompressionMethod method, CompressionLevel level)
    {
        if (method == CompressionMethod.None)
        {
            return 0;
        }

        byte flags = method switch
        {
            CompressionMethod.Zlib => 0x1,
            CompressionMethod.LZ4 => 0x2,
            _ => 0
        };

        flags |= level switch
        {
            CompressionLevel.FastCompression => 0x10,
            CompressionLevel.DefaultCompression => 0x20,
            CompressionLevel.MaxCompression => 0x40,
            _ => 0
        };

        return flags;
    }

    public static byte[] Decompress(
        byte[] compressed,
        int decompressedSize,
        byte compressionFlags,
        bool chunked = false)
    {
        switch ((CompressionMethod)(compressionFlags & 0x0F))
        {
            case CompressionMethod.None:
                return compressed;

            case CompressionMethod.Zlib:
                {
                    using var compressedStream = new MemoryStream(compressed);
                    using var decompressedStream = new MemoryStream();
                    using var stream = new ZLibStream(compressedStream, CompressionMode.Decompress);
                    var buf = new byte[0x10000];
                    int length;
                    while ((length = stream.Read(buf, 0, buf.Length)) > 0)
                    {
                        decompressedStream.Write(buf, 0, length);
                    }

                    return decompressedStream.ToArray();
                }

            case CompressionMethod.LZ4:
                if (chunked)
                {
                    using var compressedStream = new MemoryStream(compressed);
                    using var source = LZ4Stream.Decode(compressedStream);
                    using var memoryStream = new MemoryStream();
                    source.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
                else
                {
                    var decompressed = new byte[decompressedSize];
                    LZ4Codec.Decode(compressed, decompressed);
                    return decompressed;
                }

            default:
                {
                    var msg = $"No decoder found for this format: {compressionFlags}";
                    throw new InvalidDataException(msg);
                }
        }
    }

    public static byte[] Compress(byte[] uncompressed, byte compressionFlags)
    {
        return Compress(uncompressed, (CompressionMethod)(compressionFlags & 0x0F), CompressionFlagsToLevel(compressionFlags));
    }

    public static byte[] Compress(
        byte[] uncompressed,
        CompressionMethod method,
        CompressionLevel compressionLevel,
        bool chunked = false)
    {
        return method switch
        {
            CompressionMethod.None => uncompressed,
            CompressionMethod.Zlib => CompressZlib(uncompressed, compressionLevel),
            CompressionMethod.LZ4 => CompressLZ4(uncompressed, compressionLevel, chunked),
            _ => throw new ArgumentException("Invalid compression method specified")
        };
    }

    public static byte[] CompressZlib(byte[] uncompressed, CompressionLevel compressionLevel)
    {
        var level = compressionLevel switch
        {
            CompressionLevel.FastCompression => System.IO.Compression.CompressionLevel.Fastest,
            CompressionLevel.DefaultCompression => System.IO.Compression.CompressionLevel.Optimal,
            CompressionLevel.MaxCompression => System.IO.Compression.CompressionLevel.SmallestSize,
            _ => System.IO.Compression.CompressionLevel.Optimal
        };

        using var outputStream = new MemoryStream();
        using var compressor = new ZLibStream(outputStream, level);
        compressor.Write(uncompressed, 0, uncompressed.Length);
        compressor.Flush();
        return outputStream.ToArray();
    }

    public static byte[] CompressLZ4(byte[] uncompressed, CompressionLevel compressionLevel, bool chunked = false)
    {
        Span<byte> compressed = new();
        if (chunked)
        {
            LZ4Codec.Encode(uncompressed.AsSpan(), compressed);
        }
        else if (compressionLevel == CompressionLevel.FastCompression)
        {
            LZ4Codec.Encode(uncompressed, compressed);
        }
        else
        {
            LZ4Codec.Encode(uncompressed, compressed, LZ4Level.L12_MAX);
        }

        return compressed.ToArray();
    }
}