﻿using LSLib.LS.Story.Compiler;
using System.Collections.Generic;

namespace LSLib.LS.Story.HeaderParser;

/// <summary>
/// Base class for all AST nodes.
/// (This doesn't do anything meaningful, it is needed only to 
/// provide the GPPG parser a semantic value base class.)
/// </summary>
public class ASTNode
{
}

/// <summary>
/// Declarations node - contains every declaration from the story header file.
/// </summary>
public class ASTDeclarations : ASTNode
{
    // Debug options
    public readonly List<string> Options = new();
    // Declared type aliases
    public readonly List<ASTAlias> Aliases = new();
    // Declared functions
    public readonly List<ASTFunction> Functions = new();
}

/// <summary>
/// Function type wrapper node
/// This is discarded during parsing and does not appear in the final AST.
/// </summary>
public class ASTFunctionTypeNode : ASTNode
{
    // Type of function (SysQuery, SysCall, Event, etc.)
    public Compiler.FunctionType Type;
}

/// <summary>
/// Function meta-information
/// This is discarded during parsing and does not appear in the final AST.
/// </summary>
public class ASTFunctionMetadata : ASTNode
{
    public uint Meta1;
    public uint Meta2;
    public uint Meta3;
    public uint Meta4;
}

/// <summary>
/// Describes a built-in function with its name, number and parameters.
/// </summary>
public class ASTFunction : ASTNode
{
    // Type of function (SysQuery, SysCall, Event, etc.)
    public Compiler.FunctionType Type;
    // Name of the function
    public string Name;
    // Function parameters
    public List<ASTFunctionParam> Params;
    // Function metadata for Osiris internal use - mostly unknown.
    public uint Meta1;
    public uint Meta2;
    public uint Meta3;
    public uint Meta4;
}

/// <summary>
/// List of function parameters
/// This is discarded during parsing and does not appear in the final AST.
/// </summary>
public class ASTFunctionParamList : ASTNode
{
    // Function parameters
    public readonly List<ASTFunctionParam> Params = new();
}

/// <summary>
/// Typed (and optionally direction marked) parameter of a function
/// </summary>
public class ASTFunctionParam : ASTNode
{
    // Parameter name
    public string Name;
    // Parameter type
    public string Type;
    // Parameter direction (IN/OUT)
    // This is only meaningful for Query and SysQuery, for all other types direction is always "IN".
    public ParamDirection Direction;
}
    
/// <summary>
/// Type alias - defines a new type name and type ID, and maps it to an existing base type.
/// </summary>
public class ASTAlias : ASTNode
{
    // Name of the new type
    public string TypeName;
    // ID of the new type (must be a new type ID)
    public uint TypeId;
    // ID of the type this type is mapped to (must be an existing type ID)
    public uint AliasId;
}

/// <summary>
/// Debug/compiler option
/// This is discarded during parsing and does not appear in the final AST.
/// </summary>
public class ASTOption : ASTNode
{
    // Name of debug option
    public string Name;
}

/// <summary>
/// String literal from lexing stage (yytext).
/// This is discarded during parsing and does not appear in the final AST.
/// </summary>
public class ASTLiteral : ASTNode
{
    public string Literal;
}