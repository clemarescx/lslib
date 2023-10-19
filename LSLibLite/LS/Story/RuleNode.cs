using LSLib.LS.Story;

namespace LSLibLite.LS.Story;

public class RuleNode : RelNode
{
    #region Members

    public bool IsQuery;
    public GoalReference DerivedGoalRef;
    public List<Call> Calls;
    public List<Variable> Variables;
    public uint Line;

    #endregion

    public override void Read(OsiReader reader)
    {
        base.Read(reader);
        Calls = reader.ReadList<Call>();

        Variables = new List<Variable>();
        var variables = reader.ReadByte();
        while (variables-- > 0)
        {
            var type = reader.ReadByte();
            if (type != 1)
            {
                throw new InvalidDataException("Illegal value type in rule variable list");
            }

            var variable = new Variable();
            variable.Read(reader);
            if (variable.Adapted)
            {
                variable.VariableName = $"_Var{Variables.Count + 1}";
            }

            Variables.Add(variable);
        }

        Line = reader.ReadUInt32();

        IsQuery = reader.Ver >= OsiVersion.VerAddQuery && reader.ReadBoolean();
    }

    public override void Write(OsiWriter writer)
    {
        base.Write(writer);
        writer.WriteList(Calls);

        writer.Write((byte)Variables.Count);
        foreach (var variable in Variables)
        {
            writer.Write((byte)1);
            variable.Write(writer);
        }

        writer.Write(Line);
        if (writer.Ver >= OsiVersion.VerAddQuery)
        {
            writer.Write(IsQuery);
        }
    }

    public override Type NodeType()
    {
        return Type.Rule;
    }

    public override string TypeName()
    {
        return IsQuery
            ? "Query Rule"
            : "Rule";
    }

    public override void DebugDump(TextWriter writer, Story story)
    {
        base.DebugDump(writer, story);

        writer.WriteLine("    Variables: ");
        foreach (var v in Variables)
        {
            writer.Write("        ");
            v.DebugDump(writer, story);
            writer.WriteLine("");
        }

        writer.WriteLine("    Calls: ");
        foreach (var call in Calls)
        {
            writer.Write("        ");
            call.DebugDump(writer, story);
            writer.WriteLine("");
        }
    }

    public Node GetRoot()
    {
        Node parent = this;
        for (;;)
        {
            switch (parent)
            {
                case RelNode rel:
                    parent = rel.ParentRef.Resolve();
                    break;

                case JoinNode join:
                    parent = join.LeftParentRef.Resolve();
                    break;

                default: return parent;
            }
        }
    }

    public RuleType? GetRuleType(Story story)
    {
        var root = GetRoot();
        switch (root)
        {
            case DatabaseNode: return RuleType.Rule;

            case ProcNode:
            {
                var querySig = $"{root.Name}__DEF__/{root.NumParams}";
                var sig = $"{root.Name}/{root.NumParams}";

                if (!story.FunctionSignatureMap.TryGetValue(querySig, out var func) && !story.FunctionSignatureMap.TryGetValue(sig, out func))
                {
                    return null;
                }

                return func.Type switch
                {
                    FunctionType.Event     => RuleType.Rule,
                    FunctionType.Proc      => RuleType.Proc,
                    FunctionType.UserQuery => RuleType.Query,
                    _                      => throw new InvalidDataException($"Unsupported root function type: {func.Type}")
                };
            }

            default: throw new InvalidDataException("Cannot export rules with this root node");
        }
    }

    public Tuple MakeInitialTuple()
    {
        var tuple = new Tuple();
        for (var i = 0; i < Variables.Count; i++)
        {
            tuple.Physical.Add(Variables[i]);
            tuple.Logical.Add(i, Variables[i]);
        }

        return tuple;
    }

    public override void MakeScript(
        TextWriter writer,
        Story story,
        Tuple tuple,
        bool printTypes = false)
    {
        var ruleType = GetRuleType(story);
        if (ruleType == null)
        {
            return;
        }

        switch (ruleType)
        {
            case RuleType.Proc:
                writer.WriteLine("PROC");
                break;

            case RuleType.Query:
                writer.WriteLine("QRY");
                break;

            case RuleType.Rule:
                writer.WriteLine("IF");
                break;
        }

        var initialTuple = MakeInitialTuple();
        if (AdapterRef.IsValid)
        {
            var adapter = AdapterRef.Resolve();
            initialTuple = adapter.Adapt(initialTuple);
        }

        printTypes = printTypes || ruleType == RuleType.Proc || ruleType == RuleType.Query;
        ParentRef.Resolve().MakeScript(writer, story, initialTuple, printTypes);
        writer.WriteLine("THEN");
        foreach (var call in Calls)
        {
            call.MakeScript(writer, story, initialTuple, false);
            writer.WriteLine(";");
        }
    }

    public override void PostLoad(Story story)
    {
        base.PostLoad(story);
        RemoveQueryPostfix();
    }

    public override void PreSave(Story story)
    {
        base.PreSave(story);

        // Re-add the __DEF__ postfix that is added to the end of Query nodes
        if (!IsQuery)
        {
            return;
        }

        var ruleRoot = GetRoot();
        if (ruleRoot.Name != null && (ruleRoot.Name.Length < 7 || ruleRoot.Name[^7..] != "__DEF__"))
        {
            ruleRoot.Name += "__DEF__";
        }
    }

    public override void PostSave(Story story)
    {
        base.PostSave(story);
        RemoveQueryPostfix();
    }

    private void RemoveQueryPostfix()
    {
        // Remove the __DEF__ postfix that is added to the end of Query nodes
        if (!IsQuery)
        {
            return;
        }

        var ruleRoot = GetRoot();
        if (ruleRoot.Name is { Length: > 7 } && ruleRoot.Name[^7..] == "__DEF__")
        {
            ruleRoot.Name = ruleRoot.Name[..^7];
        }
    }
}