using LSLib.LS.Story.GoalParser;
using System;
using System.Collections.Generic;

namespace LSLib.LS.Story.Compiler;

/// <summary>
/// Determines the game version we're targeting during compilation.
/// </summary>
public enum TargetGame
{
    DOS2,
    DOS2DE,
    BG3
}

/// <summary>
/// Type declaration
/// </summary>
public class ValueType
{
    // Type ID
    public uint TypeId;
    // Osiris builtin type ID
    public Value.Type IntrinsicTypeId;
    // Type name
    public string Name;

    /// <summary>
    /// Returns whether this type is an alias of the specified type.
    /// </summary>
    public bool IsAliasOf(ValueType type)
    {
        return 
            // The base types match
            IntrinsicTypeId == type.IntrinsicTypeId
            // The alias ID doesn't match
         && TypeId != type.TypeId
            // This type is an alias type
         && TypeId != (uint)IntrinsicTypeId
            // The other type is a base type
         && type.TypeId == (uint)type.IntrinsicTypeId;
    }
}

/// <summary>
/// Parameter direction. 
/// Only relevant for queries, which get both input and output parameters.
/// </summary>
public enum ParamDirection
{
    In,
    Out
}

/// <summary>
/// Osiris internal function type.
/// </summary>
public enum FunctionType
{
    // Osiris items
    // Query defined by the Osiris runtime
    SysQuery,
    // Call defined by the Osiris runtime
    SysCall,

    // Application defined items
    // Event defined by the application (D:OS)
    Event,
    // Query defined by the application (D:OS)
    Query,
    // Call defined by the application (D:OS)
    Call,

    // User-defined items
    // Proc (~call) defined in user code
    Proc,
    // Query defined in user code
    UserQuery,
    // Database defined in user code
    Database
}

/// <summary>
/// Function name
/// In Osiris, multiple functions are allowed with the same name,
/// if they have different arity (number of parameters).
/// </summary>
public class FunctionNameAndArity : IEquatable<FunctionNameAndArity>
{
    // Function name
    public readonly string Name;
    // Number of parameters
    public readonly int Arity;

    public FunctionNameAndArity(string name, int arity)
    {
        Name = name;
        Arity = arity;
    }

    public override bool Equals(object fun)
    {
        return Equals(fun as FunctionNameAndArity);
    }

    public bool Equals(FunctionNameAndArity fun)
    {
        return Name.ToLowerInvariant() == fun.Name.ToLowerInvariant()
            && Arity == fun.Arity;
    }

    public override int GetHashCode()
    {
        return Name.ToLowerInvariant().GetHashCode() | Arity;
    }

    public override string ToString()
    {
        return $"{Name}({Arity})";
    }
}

/// <summary>
/// Function parameter (or database column, depending on the function type)
/// </summary>
public class FunctionParam
{
    // Parameter direction, i.e. either In or Out.
    public ParamDirection Direction;
    // Parameter type
    // For builtin functions this is always taken from the function header.
    // For user defined functions this is inferred from code.
    public ValueType Type;
    // Parameter name
    public string Name;
}

public class FunctionSignature
{
    // Type of function (call, query, database, etc.)
    public FunctionType Type;
    // Function name
    public string Name;
    // List of arguments
    public List<FunctionParam> Params;
    // Indicates that we were able to infer the type of all parameters
    public bool FullyTyped;
    // Indicates that the database is "inserted into" in at least one place
    public bool Inserted;
    // Indicates that the database is "deleted from" in at least one place
    public bool Deleted;
    // Indicates that the function is "read" in at least one place
    public bool Read;

    public FunctionNameAndArity GetNameAndArity() => new(Name, Params.Count);
}

/// <summary>
/// Metadata for built-in functions
/// </summary>
public class BuiltinFunction
{
    public FunctionSignature Signature;
    // Metadata passed from story headers.
    // These aren't used at all during compilation and are only used in the compiled story file.
    public uint Meta1;
    public uint Meta2;
    public uint Meta3;
    public uint Meta4;
}

/// <summary>
/// Diagnostic message level
/// </summary>
public enum MessageLevel
{
    Error,
    Warning
}

/// <summary>
/// Holder for compiler diagnostic codes.
/// </summary>
public class DiagnosticCode
{
    /// <summary>
    /// Miscellaenous internal error - should not happen.
    /// </summary>
    public const string InternalError = "E00";
    /// <summary>
    /// A type ID was declared multiple times in the story definition file.
    /// </summary>
    public const string TypeIdAlreadyDefined = "E01";
    /// <summary>
    /// A type name (alias)  was declared multiple times in the story definition file.
    /// </summary>
    public const string TypeNameAlreadyDefined = "E02";
    /// <summary>
    /// The type ID is either an intrinsic ID or is outside the allowed range.
    /// </summary>
    public const string TypeIdInvalid = "E03";
    /// <summary>
    /// The alias type ID doesn't point to a valid intrinsic type ID
    /// </summary>
    public const string IntrinsicTypeIdInvalid = "E04";

    /// <summary>
    /// A function with the same signature already exists.
    /// </summary>
    public const string SignatureAlreadyDefined = "E05";
    /// <summary>
    /// The type of an argument could not be resolved in a builtin function.
    /// (This only occurs when parsing story headers, not in goal code)
    /// </summary>
    public const string UnresolvedTypeInSignature = "E06";
    /// <summary>
    /// A goal with the same name was seen earlier.
    /// </summary>
    public const string GoalAlreadyDefined = "E07";
    /// <summary>
    /// The parent goal specified in the goal script was not found.
    /// </summary>
    public const string UnresolvedGoal = "E08";
    /// <summary>
    /// Failed to infer the type of a rule-local variable.
    /// </summary>
    public const string UnresolvedVariableType = "E09";
    /// <summary>
    /// The function signature (full typed parameter list) of a function
    /// could not be determined. This is likely the result of a failed type inference.
    /// </summary>
    public const string UnresolvedSignature = "E10";
    /// <summary>
    /// The intrinsic type of a function parameter does not match the expected type.
    /// </summary>
    public const string LocalTypeMismatch = "E11";
    /// <summary>
    /// Value with unknown type encountered during IR generation.
    /// </summary>
    public const string UnresolvedType = "E12";
    /// <summary>
    /// PROC/QRY declarations must start with a PROC/QRY name as the first condition.
    /// </summary>
    public const string InvalidProcDefinition = "E13";
    /// <summary>
    /// Fact contains a function that is not callable
    /// (the function is not a call, database or proc).
    /// </summary>
    public const string InvalidSymbolInFact = "E14";
    /// <summary>
    /// Rule action contains a function that is not callable
    /// (the function is not a call, database or proc).
    /// </summary>
    public const string InvalidSymbolInStatement = "E15";
    /// <summary>
    /// "NOT" action contains a non-database function.
    /// </summary>
    public const string CanOnlyDeleteFromDatabase = "E16";
    /// <summary>
    /// Initial PROC/QRY/IF function type differs from allowed type.
    /// </summary>
    public const string InvalidSymbolInInitialCondition = "E17";
    /// <summary>
    /// Condition contains a function that is not a query or database.
    /// </summary>
    public const string InvalidFunctionTypeInCondition = "E18";
    /// <summary>
    /// Function name could not be resolved.
    /// </summary>
    public const string UnresolvedSymbol = "E19";
    /// <summary>
    /// Use of less/greater operators on strings or guidstrings.
    /// </summary>
    public const string StringLtGtComparison = "W20";
    /// <summary>
    /// The alias type of a function parameter does not match the expected type.
    /// </summary>
    public const string GuidAliasMismatch = "E21";
    /// <summary>
    /// Object name GUID is prefixed with a type that is not known.
    /// </summary>
    public const string GuidPrefixNotKnown = "W22";
    /// <summary>
    /// PROC_/QRY_ naming style violation.
    /// </summary>
    public const string RuleNamingStyle = "W23";
    /// <summary>
    /// A rule variable was used in a read context, but was not yet bound.
    /// </summary>
    public const string ParamNotBound = "E24";
    /// <summary>
    /// The database is likely unused or unpopulated.
    /// (Written but not read, or vice versa)
    /// </summary>
    public const string UnusedDatabaseWarning = "W25";
    /// <summary>
    /// The database is likely unused or unpopulated.
    /// (Written but not read, or vice versa)
    /// </summary>
    public const string UnusedDatabaseError = "E25";
    /// <summary>
    /// Database "DB_" naming convention violation.
    /// </summary>
    public const string DbNamingStyle = "W26";
    /// <summary>
    /// Object name GUID could not be resolved to a game object.
    /// </summary>
    public const string UnresolvedGameObjectName = "W27";
    /// <summary>
    /// Type of name GUID differs from type of game object.
    /// </summary>
    public const string GameObjectTypeMismatch = "W28";
    /// <summary>
    /// Name part of name GUID differs from name of game object.
    /// </summary>
    public const string GameObjectNameMismatch = "W29";
    /// <summary>
    /// Multiple definitions seen for the same function with different signatures.
    /// </summary>
    public const string ProcTypeMismatch = "E30";
    /// <summary>
    /// Attempted to cast a type to an unrelated/incompatible type (i.e. STRING to INTEGER)
    /// </summary>
    public const string CastToUnrelatedType = "E31";
    /// <summary>
    /// Attempted to cast an alias to an unrelated alias (i.e. CHARACTERGUID to ITEMGUID)
    /// </summary>
    public const string CastToUnrelatedGuidAlias = "E32";
    /// <summary>
    /// Left-hand side and right-hand side variables are the same in a binary operation.
    /// This will result in an "invalid compare" error in runtime.
    /// </summary>
    public const string BinaryOperationSameRhsLhs = "E33";
    /// <summary>
    /// comparison on types that have known bugs or side effects
    /// (currently this only triggers on GUIDSTRING - STRING comparison)
    /// </summary>
    public const string RiskyComparison = "E34";
    /// <summary>
    /// The database is possibly used in an incorrect way.
    /// (Deleted and read, but not written)
    /// </summary>
    public const string UnwrittenDatabase = "W35";
}

public class Diagnostic
{
    public readonly CodeLocation Location;
    public readonly MessageLevel Level;
    public readonly string Code;
    public readonly string Message;

    public Diagnostic(CodeLocation location, MessageLevel level, string code, string message)
    {
        Location = location;
        Level = level;
        Code = code;
        Message = message;
    }
}

public class CompilationLog
{
    public readonly List<Diagnostic> Log = new();
    /// <summary>
    /// Controls whether specific warnings are enabled or disabled.
    /// All are enabled by default.
    /// </summary>
    public readonly Dictionary<string, bool> WarningSwitches = new();

    public CompilationLog()
    {
        WarningSwitches.Add(DiagnosticCode.RuleNamingStyle, false);
        WarningSwitches.Add(DiagnosticCode.UnwrittenDatabase, false);
    }

    public void Warn(CodeLocation location, string code, string message)
    {
        if (WarningSwitches.TryGetValue(code, out bool enabled) && !enabled) return;

        var diag = new Diagnostic(location, MessageLevel.Warning, code, message);
        Log.Add(diag);
    }

    public void Warn(CodeLocation location, string code, string format, object arg1)
    {
        var message = string.Format(format, arg1);
        Warn(location, code, message);
    }

    public void Warn(CodeLocation location, string code, string format, object arg1, object arg2)
    {
        var message = string.Format(format, arg1, arg2);
        Warn(location, code, message);
    }

    public void Warn(CodeLocation location, string code, string format, object arg1, object arg2, object arg3)
    {
        var message = string.Format(format, arg1, arg2, arg3);
        Warn(location, code, message);
    }

    public void Warn(CodeLocation location, string code, string format, object arg1, object arg2, object arg3, object arg4)
    {
        var message = string.Format(format, arg1, arg2, arg3, arg4);
        Warn(location, code, message);
    }

    public void Warn(CodeLocation location, string code, string format, object arg1, object arg2, object arg3, object arg4, object arg5)
    {
        var message = string.Format(format, arg1, arg2, arg3, arg4, arg5);
        Warn(location, code, message);
    }

    public void Error(CodeLocation location, string code, string message)
    {
        var diag = new Diagnostic(location, MessageLevel.Error, code, message);
        Log.Add(diag);
    }

    public void Error(CodeLocation location, string code, string format, object arg1)
    {
        var message = string.Format(format, arg1);
        Error(location, code, message);
    }

    public void Error(CodeLocation location, string code, string format, object arg1, object arg2)
    {
        var message = string.Format(format, arg1, arg2);
        Error(location, code, message);
    }

    public void Error(CodeLocation location, string code, string format, object arg1, object arg2, object arg3)
    {
        var message = string.Format(format, arg1, arg2, arg3);
        Error(location, code, message);
    }

    public void Error(CodeLocation location, string code, string format, object arg1, object arg2, object arg3, object arg4)
    {
        var message = string.Format(format, arg1, arg2, arg3, arg4);
        Error(location, code, message);
    }

    public void Error(CodeLocation location, string code, string format, object arg1, object arg2, object arg3, object arg4, object arg5)
    {
        var message = string.Format(format, arg1, arg2, arg3, arg4, arg5);
        Error(location, code, message);
    }
}

public class GameObjectInfo
{
    public string Name;
    public ValueType Type;
}

/// <summary>
/// Compilation context that holds input and intermediate data used during the compilation process.
/// </summary>
public class CompilationContext
{
    public const uint MaxIntrinsicTypeId = 5;

    public readonly Dictionary<uint, ValueType> TypesById = new();
    public readonly Dictionary<string, ValueType> TypesByName = new();
    public readonly Dictionary<string, IRGoal> GoalsByName = new();
    public readonly Dictionary<FunctionNameAndArity, FunctionSignature> Signatures = new();
    public readonly Dictionary<FunctionNameAndArity, object> Functions = new();
    public readonly Dictionary<string, GameObjectInfo> GameObjects = new();
    public readonly CompilationLog Log = new();

    public CompilationContext()
    {
        RegisterIntrinsicTypes();
    }

    /// <summary>
    /// Registers all Osiris builtin types that are not declared in the story header separately
    /// </summary>
    private void RegisterIntrinsicTypes()
    {
        var tUnknown = new ValueType
        {
            Name = "NONE",
            TypeId = 0,
            IntrinsicTypeId = Value.Type.None
        };
        AddType(tUnknown);

        var tInteger = new ValueType
        {
            Name = "INTEGER",
            TypeId = 1,
            IntrinsicTypeId = Value.Type.Integer
        };
        AddType(tInteger);

        var tInteger64 = new ValueType
        {
            Name = "INTEGER64",
            TypeId = 2,
            IntrinsicTypeId = Value.Type.Integer64
        };
        AddType(tInteger64);

        var tReal = new ValueType
        {
            Name = "REAL",
            TypeId = 3,
            IntrinsicTypeId = Value.Type.Float
        };
        AddType(tReal);

        var tString = new ValueType
        {
            Name = "STRING",
            TypeId = 4,
            IntrinsicTypeId = Value.Type.String
        };
        AddType(tString);

        var tGuidString = new ValueType
        {
            Name = "GUIDSTRING",
            TypeId = 5,
            IntrinsicTypeId = Value.Type.GuidString
        };
        AddType(tGuidString);
    }

    private void AddType(ValueType type)
    {
        TypesById.Add(type.TypeId, type);
        TypesByName.Add(type.Name, type);
    }

    public bool RegisterType(ValueType type)
    {
        if (TypesById.ContainsKey(type.TypeId))
        {
            Log.Error(null, DiagnosticCode.TypeIdAlreadyDefined, "Type ID already in use");
            return false;
        }

        if (TypesByName.ContainsKey(type.Name))
        {
            Log.Error(null, DiagnosticCode.TypeNameAlreadyDefined, "Type name already in use");
            return false;
        }

        if (type.TypeId is < MaxIntrinsicTypeId or > 255)
        {
            Log.Error(null, DiagnosticCode.TypeIdInvalid, "Type ID must be in the range 5..255");
            return false;
        }

        if (type.IntrinsicTypeId <= 0 || (uint)type.IntrinsicTypeId > MaxIntrinsicTypeId)
        {
            Log.Error(null, DiagnosticCode.TypeIdInvalid, "Alias type ID must refer to an intrinsic type");
            return false;
        }

        AddType(type);
        return true;
    }

    public bool RegisterFunction(FunctionSignature signature, object func)
    {
        var nameAndArity = signature.GetNameAndArity();
        if (Signatures.ContainsKey(nameAndArity))
        {
            Log.Error(null, DiagnosticCode.SignatureAlreadyDefined, $"Signature already registered: {nameAndArity.Name}({nameAndArity.Arity})");
            return false;
        }

        Signatures.Add(nameAndArity, signature);
        Functions.Add(nameAndArity, func);
        return true;
    }

    public bool RegisterGoal(IRGoal goal)
    {
        if (GoalsByName.ContainsKey(goal.Name))
        {
            Log.Error(null, DiagnosticCode.GoalAlreadyDefined, $"Goal already registered: {goal.Name}");
            return false;
        }

        GoalsByName.Add(goal.Name, goal);
        return true;
    }

    public ValueType LookupType(string typeName)
    {
        if (TypesByName.TryGetValue(typeName, out ValueType type))
        {
            return type;
        }
        else
        {
            return null;
        }
    }

    public FunctionSignature LookupSignature(FunctionNameAndArity name)
    {
        if (Signatures.TryGetValue(name, out FunctionSignature signature))
        {
            return signature;
        }
        else
        {
            return null;
        }
    }

    public object LookupName(FunctionNameAndArity name)
    {
        if (Functions.TryGetValue(name, out object function))
        {
            return function;
        }
        else
        {
            return null;
        }
    }

    public IRGoal LookupGoal(string name)
    {
        if (GoalsByName.TryGetValue(name, out IRGoal goal))
        {
            return goal;
        }
        else
        {
            return null;
        }
    }
}