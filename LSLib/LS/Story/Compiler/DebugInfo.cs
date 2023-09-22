using System.Collections.Generic;

namespace LSLib.LS.Story.Compiler;

public class DatabaseDebugInfo
{
    // ID of database in generated story file
    public uint Id;
    public string Name;
    public List<uint> ParamTypes;
}

public class ActionDebugInfo
{
    // Location of action in source file
    public uint Line;
}

public class GoalDebugInfo
{
    // ID of goal in generated story file
    public uint Id;
    // Goal name
    public string Name;
    // Absolute path of goal source file
    public string Path;
    // Actions in INIT section
    public List<ActionDebugInfo> InitActions;
    // Actions in EXIT section
    public List<ActionDebugInfo> ExitActions;
}

public class RuleVariableDebugInfo
{
    // Index of rule variable in local tuple
    public uint Index;
    // Name of rule variable
    public string Name;
    // Type ID of rule variable
    public uint Type;
    // Is the variable slot unused? (i.e. not bound to a physical column)
    public bool Unused;
}

public class RuleDebugInfo
{
    // Local index of rule
    // (this is not stored in the story file and is only used by the debugger)
    public uint Id;
    // ID of parent goal node
    public uint GoalId;
    // Generated rule name (usually the name of the first condition)
    public string Name;
    // Rule local variables
    public List<RuleVariableDebugInfo> Variables;
    // Actions in THEN-part
    public List<ActionDebugInfo> Actions;
    // Line number of the beginning of the "IF" section
    public uint ConditionsStartLine;
    // Line number of the end of the "IF" section
    public uint ConditionsEndLine;
    // Line number of the beginning of the "THEN" section
    public uint ActionsStartLine;
    // Line number of the end of the "THEN" section
    public uint ActionsEndLine;
}

public class NodeDebugInfo
{
    // ID of node in generated story file
    public uint Id;
    // Index of parent rule
    public uint RuleId;
    // Location of action in source file
    public int Line;
    // Local tuple to rule variable index mappings
    public Dictionary<int, int> ColumnToVariableMaps;
    // ID of associated database node
    public uint DatabaseId;
    // Name of node
    public string Name;
    // Type of node
    public Node.Type Type;
    // ID of left parent node
    public uint ParentNodeId;
    // Function (query, proc, etc.) attached to this node
    public FunctionNameAndArity FunctionName;
}

public class FunctionParamDebugInfo
{
    // Intrinsic type ID
    public uint TypeId;
    // Name of parameter
    public string Name;
    // Is an out param (ie. return value)?
    public bool Out;
}

public class FunctionDebugInfo
{
    // Name of function
    public string Name;
    // Type of node
    public List<FunctionParamDebugInfo> Params;
    // Function type ID
    public uint TypeId;
}

public class StoryDebugInfo
{
    /// <summary>
    /// Story debug info format version. Increment each time the format changes.
    /// </summary>
    public const uint CurrentVersion = 2;

    public uint Version;
    public readonly Dictionary<uint, DatabaseDebugInfo> Databases = new();
    public readonly Dictionary<uint, GoalDebugInfo> Goals = new();
    public readonly Dictionary<uint, RuleDebugInfo> Rules = new();
    public readonly Dictionary<uint, NodeDebugInfo> Nodes = new();
    public readonly Dictionary<FunctionNameAndArity, FunctionDebugInfo> Functions = new();
}