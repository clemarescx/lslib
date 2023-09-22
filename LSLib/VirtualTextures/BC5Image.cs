using LSLib.Granny;
using LSLib.LS;
using System;
using System.IO;

namespace LSLib.VirtualTextures;

public class BC5Image
{
    public readonly byte[] Data;
    public readonly int Width;
    public readonly int Height;

    public BC5Image(byte[] data, int width, int height)
    {
        Data = data;
        Width = width;
        Height = height;
    }

    public BC5Image(int width, int height)
    {
        Data = new byte[width * height];
        Width = width;
        Height = height;
    }

    public int CalculateOffset(int x, int y)
    {
        if ((x % 4) != 0 || (y % 4) != 0)
        {
            throw new ArgumentException("BC coordinates must be multiples if 4");
        }

        return ((x >> 2) + (y >> 2) * (Width >> 2)) << 4;
    }

    public void CopyTo(BC5Image destination, int srcX, int srcY, int dstX, int dstY, int width, int height)
    {
        if ((srcX % 4) != 0 || (srcY % 4) != 0 || (dstX % 4) != 0 || (dstY % 4) != 0 || (width % 4) != 0 || (height % 4) != 0)
        {
            throw new ArgumentException("BC coordinates must be multiples if 4");
        }

        var wrX = dstX;
        var wrY = dstY;
        for (var y = srcY; y < srcY + height; y += 4)
        {
            for (var x = srcX; x < srcX + width; x += 4)
            {
                var srcoff = CalculateOffset(x, y);
                var dstoff = destination.CalculateOffset(wrX, wrY);
                Array.Copy(Data, srcoff, destination.Data, dstoff, 16);
                wrX += 4;
            }

            wrY += 4;
            wrX = dstX;
        }
    }

    public void SaveDDS(string path)
    {
        var header = new DDSHeader
        {
            dwMagic = 0x20534444,
            dwSize = 0x7c,
            dwFlags = 0x1007,
            dwWidth = (uint)Width,
            dwHeight = (uint)Height,
            dwPitchOrLinearSize = (uint)(Width * Height),
            dwDepth = 1,
            dwMipMapCount = 1,
            dwPFSize = 32,
            dwPFFlags = 0x04,
            dwFourCC = 0x35545844,
            dwCaps = 0x1000
        };

        using var pagef = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(pagef);
        BinUtils.WriteStruct<DDSHeader>(bw, ref header);
        bw.Write(Data, 0, Data.Length);
    }
}