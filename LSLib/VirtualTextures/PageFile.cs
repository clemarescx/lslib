﻿using LSLib.LS;
using System;
using System.Collections.Generic;
using System.IO;

namespace LSLib.VirtualTextures;

public class PageFile : IDisposable
{
    private const uint PageSize = 0x100000;

    private VirtualTileSet TileSet;
    private FileStream Stream;
    private BinaryReader Reader;
    public GTPHeader Header;
    private List<uint[]> ChunkOffsets;

    public PageFile(VirtualTileSet tileset, string path)
    {
        TileSet = tileset;
        Stream = new(path, FileMode.Open, FileAccess.Read);
        Reader = new(Stream);

        Header = BinUtils.ReadStruct<GTPHeader>(Reader);

        var numPages = Stream.Length / PageSize;
        ChunkOffsets = new();

        for (var page = 0; page < numPages; page++)
        {
            var numOffsets = Reader.ReadUInt32();
            var offsets = new uint[numOffsets];
            BinUtils.ReadStructs<uint>(Reader, offsets);
            ChunkOffsets.Add(offsets);

            Stream.Position = (page + 1) * PageSize;
        }
    }

    public void Dispose()
    {
        Reader.Dispose();
        Stream.Dispose();
    }

    private byte[] DoUnpackTileBC(GTPChunkHeader header, int outputSize)
    {
        var parameterBlock = (GTSBCParameterBlock)TileSet.ParameterBlocks[header.ParameterBlockID];
        if (parameterBlock.CompressionName1 != "lz77" || parameterBlock.CompressionName2 != "fastlz0.1.0")
        {
            throw new InvalidDataException($"Unsupported BC compression format: '{parameterBlock.CompressionName1}', '{parameterBlock.CompressionName2}'");
        }

        var buf = Reader.ReadBytes((int)header.Size);
        byte[] outb = new byte[outputSize];
        var decd = FastLZ.fastlz1_decompress(buf, buf.Length, outb);

        byte[] outb2 = new byte[decd];
        Array.Copy(outb, outb2, decd);
        return outb2;
    }

    private byte[] DoUnpackTileUniform(GTPChunkHeader header)
    {
        var parameterBlock = (GTSUniformParameterBlock)TileSet.ParameterBlocks[header.ParameterBlockID];

        byte[] img = new byte[TileSet.Header.TileWidth * TileSet.Header.TileHeight];
        Array.Clear(img, 0, img.Length);
        return img;
    }

    public byte[] UnpackTile(int pageIndex, int chunkIndex, int outputSize)
    {
        Stream.Position = ChunkOffsets[pageIndex][chunkIndex] + pageIndex * PageSize;
        var chunkHeader = BinUtils.ReadStruct<GTPChunkHeader>(Reader);
        return chunkHeader.Codec switch
        {
            GTSCodec.Uniform => DoUnpackTileUniform(chunkHeader),
            GTSCodec.BC      => DoUnpackTileBC(chunkHeader, outputSize),
            _                => throw new InvalidDataException($"Unsupported codec: {chunkHeader.Codec}")
        };
    }

    public BC5Image UnpackTileBC5(int pageIndex, int chunkIndex)
    {
        var compressedSize = TileSet.Header.TileWidth * TileSet.Header.TileHeight * 2;
        var chunk = UnpackTile(pageIndex, chunkIndex, compressedSize);
        return new(chunk, TileSet.Header.TileWidth, TileSet.Header.TileHeight);
    }
}