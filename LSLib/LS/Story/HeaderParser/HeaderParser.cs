﻿using LSLib.LS.Story.Compiler;
using System;
using System.Text.RegularExpressions;

namespace LSLib.LS.Story.HeaderParser;

public abstract class HeaderScanBase : AbstractScanner<ASTNode, LexLocation>
{
    protected virtual bool yywrap() { return true; }

    protected ASTLiteral MakeLiteral(string lit) => new()
    {
        Literal = lit
    };

    protected ASTLiteral MakeString(string lit)
    {
        return MakeLiteral(Regex.Unescape(lit.Substring(1, lit.Length - 2)));
    }
}

public partial class HeaderParser
{
    public HeaderParser(HeaderScanner scnr) : base(scnr)
    {
    }

    public ASTDeclarations GetDeclarations()
    {
        return CurrentSemanticValue as ASTDeclarations;
    }
        
    private ASTDeclarations MakeDeclarationList() => new();

    private ASTDeclarations MakeDeclarationList(ASTNode declarations, ASTNode declaration)
    {
        var decls = declarations as ASTDeclarations;
        if (declaration is ASTOption option)
        {
            decls.Options.Add(option.Name);
        }
        else if (declaration is ASTAlias alias)
        {
            decls.Aliases.Add(alias);
        }
        else if (declaration is ASTFunction function)
        {
            decls.Functions.Add(function);
        }
        else
        {
            throw new InvalidOperationException("Tried to add unknown node to ASTDeclaration");
        }
        return decls;
    }

    private ASTFunction MakeFunction(ASTNode type, ASTNode name, ASTNode args, ASTNode metadata)
    {
        var meta = metadata as ASTFunctionMetadata;
        return new()
        {
            Type = (type as ASTFunctionTypeNode).Type,
            Name = (name as ASTLiteral).Literal,
            Params = (args as ASTFunctionParamList).Params,
            Meta1 = meta.Meta1,
            Meta2 = meta.Meta2,
            Meta3 = meta.Meta3,
            Meta4 = meta.Meta4
        };
    }

    private ASTFunctionTypeNode MakeFunctionType(Compiler.FunctionType type) => new()
    {
        Type = type
    };

    private ASTFunctionMetadata MakeFunctionMetadata(ASTNode meta1, ASTNode meta2, ASTNode meta3, ASTNode meta4) => new()
    {
        Meta1 = uint.Parse((meta1 as ASTLiteral).Literal),
        Meta2 = uint.Parse((meta2 as ASTLiteral).Literal),
        Meta3 = uint.Parse((meta3 as ASTLiteral).Literal),
        Meta4 = uint.Parse((meta4 as ASTLiteral).Literal)
    };
        
    private ASTFunctionParamList MakeFunctionParamList() => new();

    private ASTFunctionParamList MakeFunctionParamList(ASTNode param)
    {
        var list = new ASTFunctionParamList();
        list.Params.Add(param as ASTFunctionParam);
        return list;
    }

    private ASTFunctionParamList MakeFunctionParamList(ASTNode list, ASTNode param)
    {
        var paramList = list as ASTFunctionParamList;
        paramList.Params.Add(param as ASTFunctionParam);
        return paramList;
    }

    private ASTFunctionParam MakeParam(ASTNode type, ASTNode name) => new()
    {
        Name = (name as ASTLiteral).Literal,
        Type = (type as ASTLiteral).Literal,
        Direction = ParamDirection.In
    };

    private ASTFunctionParam MakeParam(ParamDirection direction, ASTNode type, ASTNode name) => new()
    {
        Name = (name as ASTLiteral).Literal,
        Type = (type as ASTLiteral).Literal,
        Direction = direction
    };

    private ASTAlias MakeAlias(ASTNode typeName, ASTNode typeId, ASTNode aliasId) => new()
    {
        TypeName = (typeName as ASTLiteral).Literal,
        TypeId = uint.Parse((typeId as ASTLiteral).Literal),
        AliasId = uint.Parse((aliasId as ASTLiteral).Literal)
    };

    private ASTOption MakeOption(ASTNode option) => new()
    {
        Name = (option as ASTLiteral).Literal
    };
}