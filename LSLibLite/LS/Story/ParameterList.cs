namespace LSLibLite.LS.Story;

public class ParameterList : IOsirisSerializable
{
    #region Members

    public List<uint> Types;

    #endregion

    public void Read(OsiReader reader)
    {
        Types = new List<uint>();
        var count = reader.ReadByte();
        while (count-- > 0)
        {
            // BG3 heuristic: Patch 8 doesn't increment the version number but changes type ID format,
            // so we need to detect it by checking if a 32-bit type ID would be valid.
            if (reader.ShortTypeIds == null)
            {
                var id = reader.ReadUInt32();
                reader.BaseStream.Position -= 4;
                reader.ShortTypeIds = id > 0xff;
            }

            Types.Add(
                reader.ShortTypeIds == true
                    ? reader.ReadUInt16()
                    : reader.ReadUInt32());
        }
    }

    public void Write(OsiWriter writer)
    {
        writer.Write((byte)Types.Count);
        foreach (var type in Types)
        {
            if (writer.ShortTypeIds)
            {
                writer.Write((ushort)type);
            }
            else
            {
                writer.Write(type);
            }
        }
    }

    public void DebugDump(TextWriter writer, Story story)
    {
        for (var i = 0; i < Types.Count; i++)
        {
            writer.Write(story.Types[Types[i]].Name);
            if (i < Types.Count - 1)
            {
                writer.Write(", ");
            }
        }
    }
}