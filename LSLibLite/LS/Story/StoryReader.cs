namespace LSLibLite.LS.Story;

public static class StoryReader
{
    public static Story Read(Stream? stream)
    {
        var story = new Story();
        using var reader = new OsiReader(stream, story);
        var header = new SaveFileHeader();
        header.Read(reader);
        reader.MinorVersion = header.MinorVersion;
        reader.MajorVersion = header.MajorVersion;
        story.MinorVersion = header.MinorVersion;
        story.MajorVersion = header.MajorVersion;

        switch (reader.Ver)
        {
            case > OsiVersion.VerLastSupported:
            {
                var msg = $"Osiris version v{reader.MajorVersion}.{reader.MinorVersion} unsupported; this tool supports loading up to version 1.12.";
                throw new InvalidDataException(msg);
            }

            case < OsiVersion.VerRemoveExternalStringTable:
                reader.ShortTypeIds = false;
                break;

            case >= OsiVersion.VerEnums:
                reader.ShortTypeIds = true;
                break;
        }

        if (reader.Ver >= OsiVersion.VerScramble)
        {
            reader.Scramble = 0xAD;
        }

        story.Types = ReadTypes(reader);

        story.ExternalStringTable = reader.Ver is >= OsiVersion.VerExternalStringTable and < OsiVersion.VerRemoveExternalStringTable
            ? ReadStrings(reader)
            : new List<string?>();

        story.Types[0] = OsirisType.MakeBuiltin(0, "UNKNOWN");
        story.Types[1] = OsirisType.MakeBuiltin(1, "INTEGER");

        if (reader.Ver >= OsiVersion.VerEnhancedTypes)
        {
            story.Types[2] = OsirisType.MakeBuiltin(2, "INTEGER64");
            story.Types[3] = OsirisType.MakeBuiltin(3, "REAL");
            story.Types[4] = OsirisType.MakeBuiltin(4, "STRING");
            // BG3 defines GUIDSTRING in the .osi file
            if (!story.Types.ContainsKey(5))
            {
                story.Types[5] = OsirisType.MakeBuiltin(5, "GUIDSTRING");
            }
        }
        else
        {
            story.Types[2] = OsirisType.MakeBuiltin(2, "FLOAT");
            story.Types[3] = OsirisType.MakeBuiltin(3, "STRING");

            // Populate custom type IDs for versions that had no type alias map
            if (reader.Ver < OsiVersion.VerAddTypeMap)
            {
                for (byte typeId = 4; typeId <= 17; typeId++)
                {
                    story.Types[typeId] = OsirisType.MakeBuiltin(typeId, $"TYPE{typeId}");
                    story.Types[typeId].Alias = 3;
                    reader.TypeAliases.Add(typeId, 3);
                }
            }
        }

        story.Enums = reader.Ver >= OsiVersion.VerEnums
            ? ReadEnums(reader)
            : new Dictionary<uint, OsirisEnum>();

        story.DivObjects = reader.ReadList<OsirisDivObject>();
        story.Functions = reader.ReadList<Function>();
        story.Nodes = ReadNodes(reader);
        story.Adapters = ReadAdapters(reader);
        story.Databases = ReadDatabases(reader);
        story.Goals = ReadGoals(reader, story);
        story.GlobalActions = reader.ReadList<Call>();
        story.ShortTypeIds = (bool)reader.ShortTypeIds;

        story.FunctionSignatureMap = new Dictionary<string, Function>();
        foreach (var func in story.Functions)
        {
            story.FunctionSignatureMap.Add($"{func.Name.Name}/{func.Name.Parameters.Types.Count}", func);
        }

        foreach (var node in story.Nodes)
        {
            node.Value.PostLoad(story);
        }

        return story;
    }

    private static List<string?> ReadStrings(BinaryReader reader)
    {
        var stringTable = new List<string?>();
        var count = reader.ReadUInt32();
        while (count-- > 0)
        {
            stringTable.Add(reader.ReadString());
        }

        return stringTable;
    }

    private static Dictionary<uint, OsirisEnum> ReadEnums(OsiReader reader)
    {
        var enums = new Dictionary<uint, OsirisEnum>();
        var count = reader.ReadUInt32();
        while (count-- > 0)
        {
            var e = new OsirisEnum();
            e.Read(reader);
            enums.Add(e.UnderlyingType, e);
        }

        return enums;
    }

    private static Dictionary<uint, Node> ReadNodes(OsiReader reader)
    {
        var nodes = new Dictionary<uint, Node>();
        var count = reader.ReadUInt32();
        while (count-- > 0)
        {
            var type = reader.ReadByte();
            var nodeId = reader.ReadUInt32();
            Node node = (Node.Type)type switch
            {
                Node.Type.Database      => new DatabaseNode(),
                Node.Type.Proc          => new ProcNode(),
                Node.Type.DivQuery      => new DivQueryNode(),
                Node.Type.InternalQuery => new InternalQueryNode(),
                Node.Type.And           => new AndNode(),
                Node.Type.NotAnd        => new NotAndNode(),
                Node.Type.RelOp         => new RelOpNode(),
                Node.Type.Rule          => new RuleNode(),
                Node.Type.UserQuery     => new UserQueryNode(),
                _                       => throw new NotImplementedException("No serializer found for this node type")
            };

            node.Read(reader);
            nodes.Add(nodeId, node);
        }

        return nodes;
    }

    private static Dictionary<uint, Adapter> ReadAdapters(OsiReader reader)
    {
        var adapters = new Dictionary<uint, Adapter>();
        var count = reader.ReadUInt32();
        while (count-- > 0)
        {
            var adapter = new Adapter();
            adapter.Read(reader);
            adapters.Add(adapter.Index, adapter);
        }

        return adapters;
    }

    private static Dictionary<uint, Database> ReadDatabases(OsiReader reader)
    {
        var databases = new Dictionary<uint, Database>();
        var count = reader.ReadUInt32();
        while (count-- > 0)
        {
            var database = new Database();
            database.Read(reader);
            databases.Add(database.Index, database);
        }

        return databases;
    }

    private static Dictionary<uint, Goal> ReadGoals(OsiReader reader, Story story)
    {
        var goals = new Dictionary<uint, Goal>();
        var count = reader.ReadUInt32();
        while (count-- > 0)
        {
            var goal = new Goal(story);
            goal.Read(reader);
            goals.Add(goal.Index, goal);
        }

        return goals;
    }

    private static Dictionary<uint, OsirisType> ReadTypes(OsiReader reader)
    {
        if (reader.Ver < OsiVersion.VerAddTypeMap)
        {
            return new Dictionary<uint, OsirisType>();
        }

        var types = new Dictionary<uint, OsirisType>();
        var count = reader.ReadUInt32();
        while (count-- > 0)
        {
            var type = new OsirisType();
            type.Read(reader);
            types.Add(type.Index, type);
        }

        // Find outermost types
        foreach (var type in types)
        {
            if (type.Value.Alias == 0)
            {
                continue;
            }

            var aliasId = type.Value.Alias;

            while (aliasId != 0 && types.ContainsKey(aliasId) && types[aliasId].Alias != 0)
            {
                aliasId = types[aliasId].Alias;
            }

            reader.TypeAliases.Add(type.Key, aliasId);
        }

        return types;
    }
}