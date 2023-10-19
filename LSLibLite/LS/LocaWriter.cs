using System.Runtime.InteropServices;
using System.Text;

namespace LSLibLite.LS;

public class LocaWriter
{
    #region Members

    private readonly Stream stream;

    #endregion

    #region Constructors

    public LocaWriter(Stream stream)
    {
        this.stream = stream;
    }

    #endregion

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

        foreach (var bin in res.Entries.Select(entry => Encoding.UTF8.GetBytes(entry.Text)))
        {
            writer.Write(bin);
            writer.Write((byte)0);
        }
    }
}