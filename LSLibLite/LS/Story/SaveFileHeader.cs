using System.Text;

namespace LSLibLite.LS.Story;

public class SaveFileHeader : IOsirisSerializable
{
    #region Members

    public bool BigEndian;
    public byte MajorVersion;
    public byte MinorVersion;
    public byte Unused;
    public string? Version;
    public uint DebugFlags;

    #endregion

    #region Properties

    public uint Ver => (uint)MajorVersion << 8 | MinorVersion;

    #endregion

    public void Read(OsiReader reader)
    {
        reader.ReadByte();
        Version = reader.ReadString();
        MajorVersion = reader.ReadByte();
        MinorVersion = reader.ReadByte();
        BigEndian = reader.ReadBoolean();
        Unused = reader.ReadByte();

        if (Ver >= OsiVersion.VerAddVersionString)
        {
            reader.ReadBytes(0x80); // Version string buffer
        }

        DebugFlags = Ver >= OsiVersion.VerAddDebugFlags
            ? reader.ReadUInt32()
            : 0;
    }

    public void Write(OsiWriter writer)
    {
        writer.Write((byte)0);
        writer.Write(Version);
        writer.Write(MajorVersion);
        writer.Write(MinorVersion);
        writer.Write(BigEndian);
        writer.Write(Unused);

        if (Ver >= OsiVersion.VerAddVersionString)
        {
            var versionString = $"{MajorVersion}.{MinorVersion}";
            var versionBytes = Encoding.UTF8.GetBytes(versionString);
            var version = new byte[0x80];
            versionBytes.CopyTo(version, 0);
            writer.Write(version, 0, version.Length);
        }

        if (Ver >= OsiVersion.VerAddDebugFlags)
        {
            writer.Write(DebugFlags);
        }
    }
}