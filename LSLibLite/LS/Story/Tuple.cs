namespace LSLibLite.LS.Story;

public class Tuple : IOsirisSerializable
{
    #region Members

    public readonly Dictionary<int, Value> Logical = new();
    public readonly List<Value> Physical = new();

    #endregion

    public void Read(OsiReader reader)
    {
        Physical.Clear();
        Logical.Clear();

        var count = reader.ReadByte();
        while (count-- > 0)
        {
            var index = reader.ReadByte();
            var value = new Value();
            value.Read(reader);

            Physical.Add(value);
            Logical.Add(index, value);
        }
    }

    public void Write(OsiWriter writer)
    {
        writer.Write((byte)Logical.Count);
        foreach (var logical in Logical)
        {
            writer.Write((byte)logical.Key);
            logical.Value.Write(writer);
        }
    }

    public void DebugDump(TextWriter writer, Story story)
    {
        writer.Write("(");
        var keys = Logical.Keys.ToArray();
        for (var i = 0; i < Logical.Count; i++)
        {
            writer.Write("{0}: ", keys[i]);
            Logical[keys[i]].DebugDump(writer, story);
            if (i < Logical.Count - 1)
            {
                writer.Write(", ");
            }
        }

        writer.Write(")");
    }

    public void MakeScript(TextWriter writer, Story story, bool printTypes = false)
    {
        for (var i = 0; i < Physical.Count; i++)
        {
            var value = Physical[i];
            value.MakeScript(writer, story, null, printTypes);
            if (i < Physical.Count - 1)
            {
                writer.Write(", ");
            }
        }
    }
}