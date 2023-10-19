namespace LSLibLite.LS.Story;

public class StoryWriter
{
    #region Members

    private OsiWriter Writer;

    #endregion

    public void Write(Stream stream, Story story, bool leaveOpen)
    {
        using (Writer = new OsiWriter(stream, leaveOpen))
        {
            foreach (var node in story.Nodes)
            {
                node.Value.PreSave(story);
            }

            Writer.MajorVersion = story.MajorVersion;
            Writer.MinorVersion = story.MinorVersion;
            Writer.ShortTypeIds = story.ShortTypeIds;
            Writer.Enums = story.Enums;

            var header = new SaveFileHeader();
            if (Writer.Ver >= OsiVersion.VerExternalStringTable)
            {
                header.Version = Writer.ShortTypeIds
                    ? "Osiris save file dd. 07/09/22 00:20:54. Version 1.8."
                    : "Osiris save file dd. 03/30/17 07:28:20. Version 1.8.";
            }
            else
            {
                header.Version = "Osiris save file dd. 02/10/15 12:44:13. Version 1.5.";
            }

            header.MajorVersion = story.MajorVersion;
            header.MinorVersion = story.MinorVersion;
            header.BigEndian = false;
            header.Unused = 0;
            // Debug flags used in D:OS EE and D:OS 2
            header.DebugFlags = 0x000C10A0;
            header.Write(Writer);

            switch (Writer.Ver)
            {
                case > OsiVersion.VerLastSupported:
                {
                    var msg = $"Osiris version v{Writer.MajorVersion}.{Writer.MinorVersion} unsupported; this tool supports saving up to version 1.11.";
                    throw new InvalidDataException(msg);
                }

                case >= OsiVersion.VerScramble:
                    Writer.Scramble = 0xAD;
                    break;
            }

            if (Writer.Ver >= OsiVersion.VerAddTypeMap)
            {
                var types = Writer.Ver >= OsiVersion.VerEnums
                    // BG3 Patch 9 writes all types to the blob except type 0
                    ? story.Types.Values.Where(t => t.Name != "UNKNOWN").ToList()
                    // Don't export builtin types, only externally declared ones
                    : story.Types.Values.Where(t => !t.IsBuiltin).ToList();

                WriteTypes(types, story);
            }

            if (Writer.Ver >= OsiVersion.VerEnums)
            {
                Writer.WriteList(story.Enums.Values.ToList());
            }

            // TODO: regenerate string table?
            if (Writer.Ver is >= OsiVersion.VerExternalStringTable and < OsiVersion.VerRemoveExternalStringTable)
            {
                WriteStrings(story.ExternalStringTable);
            }

            Writer.WriteList(story.DivObjects);
            Writer.WriteList(story.Functions);
            WriteNodes(story.Nodes);
            WriteAdapters(story.Adapters);
            WriteDatabases(story.Databases);
            WriteGoals(story.Goals);
            Writer.WriteList(story.GlobalActions);

            foreach (var node in story.Nodes)
            {
                node.Value.PostSave(story);
            }
        }
    }

    private void WriteStrings(List<string?> stringTable)
    {
        Writer.Write((uint)stringTable.Count);
        foreach (var s in stringTable)
        {
            Writer.Write(s);
        }
    }

    private void WriteTypes(ICollection<OsirisType> types, Story story)
    {
        Writer.Write((uint)types.Count);
        foreach (var type in types)
        {
            type.Write(Writer);
            if (type.Alias != 0)
            {
                Writer.TypeAliases.Add(type.Index, story.FindBuiltinTypeId(type.Index));
            }
        }
    }

    private void WriteNodes(Dictionary<uint, Node> nodes)
    {
        Writer.Write((uint)nodes.Count);
        foreach (var node in nodes)
        {
            Writer.Write((byte)node.Value.NodeType());
            Writer.Write(node.Key);
            node.Value.Write(Writer);
        }
    }

    private void WriteAdapters(Dictionary<uint, Adapter> adapters)
    {
        Writer.Write((uint)adapters.Count);
        foreach (var adapter in adapters)
        {
            Writer.Write(adapter.Key);
            adapter.Value.Write(Writer);
        }
    }

    private void WriteDatabases(Dictionary<uint, Database> databases)
    {
        Writer.Write((uint)databases.Count);
        foreach (var database in databases)
        {
            Writer.Write(database.Key);
            database.Value.Write(Writer);
        }
    }

    private void WriteGoals(Dictionary<uint, Goal> goals)
    {
        Writer.Write((uint)goals.Count);
        foreach (var goal in goals)
        {
            goal.Value.Write(Writer);
        }
    }
}