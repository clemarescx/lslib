using LSLib.LS.Story;

namespace LSLibLite.LS.Story;

public class Function : IOsirisSerializable
{
    #region Members

    public FunctionSignature Name;
    public FunctionType Type;
    public NodeReference NodeRef;
    public uint Line;
    public uint ConditionReferences;
    public uint ActionReferences;
    public uint Meta1;
    public uint Meta2;
    public uint Meta3;
    public uint Meta4;

    #endregion

    public void Read(OsiReader reader)
    {
        Line = reader.ReadUInt32();
        ConditionReferences = reader.ReadUInt32();
        ActionReferences = reader.ReadUInt32();
        NodeRef = reader.ReadNodeRef();
        Type = (FunctionType)reader.ReadByte();
        Meta1 = reader.ReadUInt32();
        Meta2 = reader.ReadUInt32();
        Meta3 = reader.ReadUInt32();
        Meta4 = reader.ReadUInt32();
        Name = new FunctionSignature();
        Name.Read(reader);
    }

    public void Write(OsiWriter writer)
    {
        writer.Write(Line);
        writer.Write(ConditionReferences);
        writer.Write(ActionReferences);
        NodeRef.Write(writer);
        writer.Write((byte)Type);
        writer.Write(Meta1);
        writer.Write(Meta2);
        writer.Write(Meta3);
        writer.Write(Meta4);
        Name.Write(writer);
    }

    public void DebugDump(TextWriter writer, Story story)
    {
        writer.Write("{0} ", Type.ToString());
        Name.DebugDump(writer, story);
        if (NodeRef.IsValid)
        {
            var node = NodeRef.Resolve();
            writer.Write(" @ {0}({1})", node.Name, node.NumParams);
        }

        writer.Write(" CondRefs {0}, ActRefs {1}", ConditionReferences, ActionReferences);
        writer.WriteLine(" Meta ({0}, {1}, {2}, {3})", Meta1, Meta2, Meta3, Meta4);
    }
}