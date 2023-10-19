namespace LSLibLite.LS.Story;

public class Call : IOsirisSerializable
{
    #region Members

    public bool Negate;
    public int GoalIdOrDebugHook;
    public List<TypedValue>? Parameters;
    public string? Name;

    #endregion

    public void Read(OsiReader reader)
    {
        Name = reader.ReadString();
        if (Name.Length > 0)
        {
            var hasParams = reader.ReadByte();
            if (hasParams > 0)
            {
                Parameters = new List<TypedValue>();
                var numParams = reader.ReadByte();
                while (numParams-- > 0)
                {
                    var type = reader.ReadByte();
                    var param = type == 1
                        ? new Variable()
                        : new TypedValue();

                    param.Read(reader);
                    Parameters.Add(param);
                }
            }

            Negate = reader.ReadBoolean();
        }

        GoalIdOrDebugHook = reader.ReadInt32();
    }

    public void Write(OsiWriter writer)
    {
        writer.Write(Name);
        if (Name.Length > 0)
        {
            writer.Write(Parameters != null);
            if (Parameters != null)
            {
                writer.Write((byte)Parameters.Count);
                foreach (var param in Parameters)
                {
                    writer.Write(param is Variable);
                    param.Write(writer);
                }
            }

            writer.Write(Negate);
        }

        writer.Write(GoalIdOrDebugHook);
    }

    public void DebugDump(TextWriter writer, Story story)
    {
        if (Name.Length > 0)
        {
            if (Negate)
            {
                writer.Write("!");
            }

            writer.Write("{0}(", Name);
            if (Parameters != null)
            {
                for (var i = 0; i < Parameters.Count; i++)
                {
                    Parameters[i].DebugDump(writer, story);
                    if (i < Parameters.Count - 1)
                    {
                        writer.Write(", ");
                    }
                }
            }

            writer.Write(") ");
        }

        switch (GoalIdOrDebugHook)
        {
            case 0: return;

            case < 0:
                writer.Write("<Debug hook #{0}>", -GoalIdOrDebugHook);
                break;

            default:
            {
                var goal = story.Goals[(uint)GoalIdOrDebugHook];
                writer.Write("<Complete goal #{0} {1}>", GoalIdOrDebugHook, goal.Name);
                break;
            }
        }
    }

    public void MakeScript(
        TextWriter writer,
        Story story,
        Tuple tuple,
        bool printTypes)
    {
        if (Name.Length > 0)
        {
            if (Negate)
            {
                writer.Write("NOT ");
            }

            writer.Write("{0}(", Name);
            if (Parameters != null)
            {
                for (var i = 0; i < Parameters.Count; i++)
                {
                    var param = Parameters[i];
                    param.MakeScript(writer, story, tuple, printTypes);
                    if (i < Parameters.Count - 1)
                    {
                        writer.Write(", ");
                    }
                }
            }

            writer.Write(")");
        }

        if (GoalIdOrDebugHook > 0)
        {
            writer.Write("GoalCompleted");
        }
    }
}