using System.Text;

namespace LSLibLite.LS.Story;

public class OsiWriter : BinaryWriter
{
    #region Members

    public readonly Dictionary<uint, uint> TypeAliases = new();

    // Use 16-bit instead of 32-bit type IDs, BG3 Patch8+
    public bool ShortTypeIds;
    public byte Scramble = 0x00;
    public Dictionary<uint, OsirisEnum> Enums = new();
    public uint MinorVersion;
    public uint MajorVersion;

    #endregion

    #region Constructors

    public OsiWriter(Stream stream, bool leaveOpen) : base(stream, Encoding.UTF8, leaveOpen) { }

    #endregion

    #region Properties

    public uint Ver => MajorVersion << 8 | MinorVersion;

    #endregion

    public override void Write(string? s)
    {
        var bytes = Encoding.UTF8.GetBytes(s);
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(bytes[i] ^ Scramble);
        }

        Write(bytes, 0, bytes.Length);
        Write(Scramble);
    }

    public override void Write(bool b)
    {
        Write(
            (byte)(b
                ? 1
                : 0));
    }

    public void Write(Guid guid)
    {
        var bytes = guid.ToByteArray();
        Write(bytes, 0, bytes.Length);
    }

    public void WriteList<T>(List<T> list) where T : IOsirisSerializable
    {
        Write((uint)list.Count);
        foreach (var item in list)
        {
            item.Write(this);
        }
    }
}