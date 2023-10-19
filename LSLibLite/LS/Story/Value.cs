using System.Globalization;

namespace LSLibLite.LS.Story;

public class Value : IOsirisSerializable
{
    #region Public Enums

    // Original Sin 2 (v1.11) Type ID-s
    public enum Type : uint
    {
        None = 0,
        Integer = 1,
        Integer64 = 2,
        Float = 3,
        String = 4,
        GuidString = 5
    }

    // Original Sin 1 (v1.0 - v1.7) Type ID-s
    public enum Type_OS1 : uint
    {
        None = 0,
        Integer = 1,
        Float = 2,
        String = 3
    }

    #endregion

    #region Members

    public float FloatValue;
    public int IntValue;
    public long Int64Value;
    public string? StringValue;

    public uint TypeId;

    #endregion

    public virtual void Read(OsiReader reader)
    {
        // possibly isReference?
        var wtf = reader.ReadByte();
        if (wtf == '1')
        {
            TypeId = reader.ShortTypeIds == true
                ? reader.ReadUInt16()
                : reader.ReadUInt32();

            IntValue = reader.ReadInt32();
        }
        else if (wtf == '0')
        {
            TypeId = reader.ShortTypeIds == true
                ? reader.ReadUInt16()
                : reader.ReadUInt32();

            var writtenTypeId = TypeId;

            var dos1alias = false;
            if (reader.TypeAliases.TryGetValue(writtenTypeId, out var alias))
            {
                writtenTypeId = alias;
                if (reader.Ver < OsiVersion.VerEnhancedTypes)
                {
                    dos1alias = true;
                }
            }

            if (reader.Ver < OsiVersion.VerEnhancedTypes)
            {
                // Convert D:OS 1 type ID to D:OS 2 type ID
                writtenTypeId = ConvertOS1ToOS2Type(writtenTypeId);
            }

            switch ((Type)writtenTypeId)
            {
                case Type.None:
                    break;

                case Type.Integer:
                    IntValue = reader.ReadInt32();
                    break;

                case Type.Integer64:
                    Int64Value = reader.ReadInt64();
                    break;

                case Type.Float:
                    FloatValue = reader.ReadSingle();
                    break;

                case Type.GuidString:
                case Type.String:
                    // D:OS 1 aliased strings didn't have a flag byte
                    if (dos1alias)
                    {
                        StringValue = reader.ReadString();
                    }
                    else if (reader.ReadByte() > 0)
                    {
                        StringValue = reader.ReadString();
                    }

                    break;

                default:
                    StringValue = reader.ReadString();
                    break;
            }
        }
        else if (wtf == 'e')
        {
            TypeId = reader.ReadUInt16();

            if (!reader.Story.Enums.TryGetValue(TypeId, out var e))
            {
                throw new InvalidDataException($"Enum label serialized for a non-enum type: {TypeId}");
            }

            StringValue = reader.ReadString();
            var ele = e.Elements.Find(v => v.Name == StringValue);
            if (ele == null)
            {
                throw new InvalidDataException($"Enumeration {TypeId} has no label named '{StringValue}'");
            }
        }
        else
        {
            throw new InvalidDataException("Unrecognized value type");
        }
    }

    public virtual void Write(OsiWriter writer)
    {
        if (writer.Enums.ContainsKey(TypeId))
        {
            writer.Write((byte)'e');
            writer.Write((ushort)TypeId);
            writer.Write(StringValue);
            return;
        }

        // TODO: Is the == 0x31 case ever used when reading?
        writer.Write((byte)'0');

        var writtenTypeId = TypeId;
        var aliased = false;
        if (writer.TypeAliases.TryGetValue(TypeId, out var alias))
        {
            aliased = true;
            writtenTypeId = alias;
        }

        if (writer.ShortTypeIds)
        {
            writer.Write((ushort)TypeId);
        }
        else
        {
            writer.Write(TypeId);
        }

        if (writer.Ver < OsiVersion.VerEnhancedTypes)
        {
            // Make sure that we're serializing using the D:OS2 type ID
            // (The alias map contains the D:OS 1 ID)
            writtenTypeId = ConvertOS1ToOS2Type(writtenTypeId);
        }

        switch ((Type)writtenTypeId)
        {
            case Type.None:
                break;

            case Type.Integer:
                writer.Write(IntValue);
                break;

            case Type.Integer64:
                // D:OS 1 aliased strings didn't have a flag byte
                if (writer.Ver >= OsiVersion.VerEnhancedTypes)
                {
                    writer.Write(Int64Value);
                }
                else
                {
                    writer.Write((int)Int64Value);
                }

                break;

            case Type.Float:
                writer.Write(FloatValue);
                break;

            case Type.String:
            case Type.GuidString:
                if (!aliased || writer.Ver >= OsiVersion.VerEnhancedTypes)
                {
                    writer.Write(StringValue != null);
                }

                if (StringValue != null)
                {
                    writer.Write(StringValue);
                }

                break;

            default:
                writer.Write(StringValue);
                break;
        }
    }

    public override string? ToString()
    {
        return (Type)TypeId switch
        {
            Type.None       => "",
            Type.Integer    => IntValue.ToString(),
            Type.Integer64  => Int64Value.ToString(),
            Type.Float      => FloatValue.ToString(CultureInfo.InvariantCulture),
            Type.String     => StringValue,
            Type.GuidString => StringValue,
            _               => StringValue
        };
    }

    public static uint ConvertOS1ToOS2Type(uint os1TypeId)
    {
        // Convert D:OS 1 type ID to D:OS 2 type ID
        return (Type_OS1)os1TypeId switch
        {
            Type_OS1.None    => (uint)Type.None,
            Type_OS1.Integer => (uint)Type.Integer,
            Type_OS1.Float   => (uint)Type.Float,
            Type_OS1.String  => (uint)Type.String,
            _                => os1TypeId
        };
    }

    public Type GetBuiltinTypeId(Story story)
    {
        var aliasId = story.FindBuiltinTypeId(TypeId);

        if (story.Version < OsiVersion.VerEnhancedTypes)
        {
            return (Type)ConvertOS1ToOS2Type(aliasId);
        }

        return (Type)aliasId;
    }

    public virtual void DebugDump(TextWriter writer, Story story)
    {
        var builtinTypeId = GetBuiltinTypeId(story);

        switch (builtinTypeId)
        {
            case Type.None:
                writer.Write("<unknown>");
                break;

            case Type.Integer:
                writer.Write(IntValue);
                break;

            case Type.Integer64:
                writer.Write(Int64Value);
                break;

            case Type.Float:
                writer.Write(FloatValue);
                break;

            case Type.String:
                writer.Write("'{0}'", StringValue);
                break;

            case Type.GuidString:
                writer.Write(StringValue);
                break;

            default:
                throw new Exception("Unsupported builtin type ID");
        }
    }

    public virtual void MakeScript(
        TextWriter writer,
        Story story,
        Tuple tuple,
        bool printTypes = false)
    {
        var builtinTypeId = GetBuiltinTypeId(story);

        switch (builtinTypeId)
        {
            case Type.None:
                throw new InvalidDataException("Script cannot contain unknown values");

            case Type.Integer:
                writer.Write(IntValue);
                break;

            case Type.Integer64:
                writer.Write(IntValue);
                break;

            case Type.Float:
                writer.Write((decimal)FloatValue);
                break;

            case Type.String:
                writer.Write("\"{0}\"", StringValue);
                break;

            case Type.GuidString:
                writer.Write(StringValue);
                break;

            default:
                throw new Exception("Unsupported builtin type ID");
        }
    }
}