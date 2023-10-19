using LSLib.LS.Story;

namespace LSLibLite.LS.Story;

public abstract class Node : IOsirisSerializable
{
    #region Public Enums

    public enum Type : byte
    {
        Database = 1,
        Proc = 2,
        DivQuery = 3,
        And = 4,
        NotAnd = 5,
        RelOp = 6,
        Rule = 7,
        InternalQuery = 8,
        UserQuery = 9
    }

    #endregion

    #region Members

    public byte NumParams;
    public DatabaseReference? DatabaseRef;
    public string? Name;

    public uint Index;

    #endregion

    public virtual void Read(OsiReader reader)
    {
        DatabaseRef = reader.ReadDatabaseRef();
        Name = reader.ReadString();
        if (Name.Length > 0)
        {
            NumParams = reader.ReadByte();
        }
    }

    public virtual void Write(OsiWriter writer)
    {
        DatabaseRef.Write(writer);
        writer.Write(Name);
        if (Name.Length > 0)
        {
            writer.Write(NumParams);
        }
    }

    public abstract Type NodeType();

    public abstract string TypeName();

    public abstract void MakeScript(
        TextWriter writer,
        Story story,
        Tuple tuple,
        bool printTypes = false);

    public virtual void PostLoad(Story story)
    {
        if (!DatabaseRef.IsValid)
        {
            return;
        }

        var database = DatabaseRef.Resolve();
        if (database.OwnerNode != null)
        {
            throw new InvalidDataException("A database cannot be assigned to multiple database nodes!");
        }

        database.OwnerNode = this;
    }

    public virtual void PreSave(Story story) { }

    public virtual void PostSave(Story story) { }

    public virtual void DebugDump(TextWriter writer, Story story)
    {
        if (Name.Length > 0)
        {
            writer.Write("{0}({1}): ", Name, NumParams);
        }

        writer.Write("<{0}>", TypeName());
        if (DatabaseRef.IsValid)
        {
            writer.Write(", Database ");
            DatabaseRef.DebugDump(writer, story);
        }

        writer.WriteLine();
    }
}