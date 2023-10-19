using System.Text;
using LSLib.LS.Story;

namespace LSLibLite.LS.Story;

public class OsiReader : BinaryReader
{
    #region Members

    public readonly Dictionary<uint, uint> TypeAliases = new();

    // TODO: Make RO!
    public readonly Story Story;

    // Use 16-bit instead of 32-bit type IDs, BG3 Patch8+
    public bool? ShortTypeIds = null;
    public byte Scramble = 0x00;
    public uint MinorVersion;
    public uint MajorVersion;

    #endregion

    #region Constructors

    public OsiReader(Stream? stream, Story story) : base(stream)
    {
        Story = story;
    }

    #endregion

    #region Properties

    public uint Ver => MajorVersion << 8 | MinorVersion;

    #endregion

    public override string ReadString()
    {
        List<byte> bytes = new();
        while (true)
        {
            var b = (byte)(ReadByte() ^ Scramble);
            if (b != 0)
            {
                bytes.Add(b);
            }
            else
            {
                break;
            }
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    public override bool ReadBoolean()
    {
        var b = ReadByte();
        if (b != 0 && b != 1)
        {
            throw new InvalidDataException("Invalid boolean value; expected 0 or 1.");
        }

        return b == 1;
    }

    public Guid ReadGuid()
    {
        var guid = ReadBytes(16);
        return new Guid(guid);
    }

    public List<T> ReadList<T>() where T : IOsirisSerializable, new()
    {
        var items = new List<T>();
        ReadList(items);
        return items;
    }

    public void ReadList<T>(List<T> items) where T : IOsirisSerializable, new()
    {
        var count = ReadUInt32();
        while (count-- > 0)
        {
            var item = new T();
            item.Read(this);
            items.Add(item);
        }
    }

    public List<T> ReadRefList<T, RefT>() where T : OsiReference<RefT>, new()
    {
        var items = new List<T>();
        ReadRefList<T, RefT>(items);
        return items;
    }

    public void ReadRefList<T, RefT>(List<T> items) where T : OsiReference<RefT>, new()
    {
        var count = ReadUInt32();
        while (count-- > 0)
        {
            var item = new T();
            item.BindStory(Story);
            item.Read(this);
            items.Add(item);
        }
    }

    public NodeReference ReadNodeRef()
    {
        var nodeRef = new NodeReference();
        nodeRef.BindStory(Story);
        nodeRef.Read(this);
        return nodeRef;
    }

    public AdapterReference ReadAdapterRef()
    {
        var adapterRef = new AdapterReference();
        adapterRef.BindStory(Story);
        adapterRef.Read(this);
        return adapterRef;
    }

    public DatabaseReference ReadDatabaseRef()
    {
        var databaseRef = new DatabaseReference();
        databaseRef.BindStory(Story);
        databaseRef.Read(this);
        return databaseRef;
    }

    public GoalReference ReadGoalRef()
    {
        var goalRef = new GoalReference();
        goalRef.BindStory(Story);
        goalRef.Read(this);
        return goalRef;
    }
}