namespace LSLibLite.LS.Story;

public class Fact : IOsirisSerializable
{
    #region Members

    public List<Value> Columns;

    #endregion

    public void Read(OsiReader reader)
    {
        Columns = new List<Value>();
        var count = reader.ReadByte();
        while (count-- > 0)
        {
            var value = new Value();
            value.Read(reader);
            Columns.Add(value);
        }
    }

    public void Write(OsiWriter writer)
    {
        writer.Write((byte)Columns.Count);
        foreach (var column in Columns)
        {
            column.Write(writer);
        }
    }

    public void DebugDump(TextWriter writer, Story story)
    {
        writer.Write("(");
        for (var i = 0; i < Columns.Count; i++)
        {
            Columns[i].DebugDump(writer, story);
            if (i < Columns.Count - 1)
            {
                writer.Write(", ");
            }
        }

        writer.Write(")");
    }
}