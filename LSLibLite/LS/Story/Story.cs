namespace LSLibLite.LS.Story;

public class Story
{
    #region Members

    // Use 16-bit instead of 32-bit type IDs, BG3 Patch8+
    public bool ShortTypeIds;
    public byte MinorVersion;
    public byte MajorVersion;
    public Dictionary<string, Function> FunctionSignatureMap;
    public Dictionary<uint, Adapter> Adapters;
    public Dictionary<uint, Database> Databases;
    public Dictionary<uint, Goal> Goals;
    public Dictionary<uint, Node> Nodes;
    public Dictionary<uint, OsirisEnum> Enums;
    public Dictionary<uint, OsirisType> Types;
    public List<Call> GlobalActions;
    public List<Function> Functions;
    public List<OsirisDivObject> DivObjects;
    public List<string?> ExternalStringTable;
    public SaveFileHeader Header;

    #endregion

    #region Properties

    public uint Version => (uint)MajorVersion << 8 | MinorVersion;

    #endregion

    public void DebugDump(TextWriter writer)
    {
        writer.WriteLine(" --- ENUMS ---");
        foreach (var e in Enums)
        {
            e.Value.DebugDump(writer);
        }

        writer.WriteLine(" --- TYPES ---");
        foreach (var type in Types)
        {
            type.Value.DebugDump(writer);
        }

        writer.WriteLine();
        writer.WriteLine(" --- DIV OBJECTS ---");
        foreach (var obj in DivObjects)
        {
            obj.DebugDump(writer);
        }

        writer.WriteLine();
        writer.WriteLine(" --- FUNCTIONS ---");
        foreach (var function in Functions)
        {
            function.DebugDump(writer, this);
        }

        writer.WriteLine();
        writer.WriteLine(" --- NODES ---");
        foreach (var node in Nodes)
        {
            writer.Write("#{0} ", node.Key);
            node.Value.DebugDump(writer, this);
            writer.WriteLine();
        }

        writer.WriteLine();
        writer.WriteLine(" --- ADAPTERS ---");
        foreach (var adapter in Adapters)
        {
            writer.Write("#{0} ", adapter.Key);
            adapter.Value.DebugDump(writer, this);
        }

        writer.WriteLine();
        writer.WriteLine(" --- DATABASES ---");
        foreach (var database in Databases)
        {
            writer.Write("#{0} ", database.Key);
            database.Value.DebugDump(writer, this);
        }

        writer.WriteLine();
        writer.WriteLine(" --- GOALS ---");
        foreach (var goal in Goals)
        {
            writer.Write("#{0} ", goal.Key);
            goal.Value.DebugDump(writer, this);
            writer.WriteLine();
        }

        writer.WriteLine();
        writer.WriteLine(" --- GLOBAL ACTIONS ---");
        foreach (var call in GlobalActions)
        {
            call.DebugDump(writer, this);
            writer.WriteLine();
        }
    }

    public uint FindBuiltinTypeId(uint typeId)
    {
        var aliasId = typeId;

        while (typeId != 0 && Types[aliasId].Alias != 0)
        {
            aliasId = Types[aliasId].Alias;
        }

        return aliasId;
    }
}