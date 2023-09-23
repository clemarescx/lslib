using LSLib.LS.Story.GoalParser;
using System;
using System.IO;
using OsirisParser.Goal;

namespace LSLib.LS.Story.Compiler;

/// <summary>
/// Generates IR from story AST.
/// </summary>
public class IRGenerator
{
    private CompilationContext Context;
    public CodeLocation LastLocation;

    public IRGenerator(CompilationContext context)
    {
        Context = context;
    }

    private IRGoal ASTGoalToIR(ASTGoal astGoal)
    {
        var goal = new IRGoal
        {
            InitSection = new(astGoal.InitSection.Count),
            KBSection = new(astGoal.KBSection.Count),
            ExitSection = new(astGoal.ExitSection.Count),
            ParentTargetEdges = new(astGoal.ParentTargetEdges.Count),
            Location = astGoal.Location
        };

        foreach (var fact in astGoal.InitSection)
        {
            goal.InitSection.Add(ASTFactToIR(goal, fact));
        }

        foreach (var rule in astGoal.KBSection)
        {
            goal.KBSection.Add(ASTRuleToIR(goal, rule));
        }

        foreach (var fact in astGoal.ExitSection)
        {
            goal.ExitSection.Add(ASTFactToIR(goal, fact));
        }

        foreach (var refGoal in astGoal.ParentTargetEdges)
        {
            var edge = new IRTargetEdge
            {
                Goal = new(refGoal.Goal),
                Location = refGoal.Location
            };

            goal.ParentTargetEdges.Add(edge);
        }

        return goal;
    }

    private IRRule ASTRuleToIR(IRGoal goal, ASTRule astRule)
    {
        var rule = new IRRule
        {
            Goal = goal,
            Type = astRule.Type,
            Conditions = new(astRule.Conditions.Count),
            Actions = new(astRule.Actions.Count),
            Variables = new(),
            VariablesByName = new(),
            Location = astRule.Location
        };

        foreach (var condition in astRule.Conditions)
        {
            rule.Conditions.Add(ASTConditionToIR(rule, condition));
        }

        foreach (var action in astRule.Actions)
        {
            rule.Actions.Add(ASTActionToIR(rule, action));
        }

        return rule;
    }

    private IRStatement ASTActionToIR(IRRule rule, ASTAction astAction)
    {
        if (astAction is ASTGoalCompletedAction astGoal)
        {
            return new()
            {
                Func = null,
                Goal = rule.Goal,
                Not = false,
                Params = new(),
                Location = astGoal.Location
            };
        }
        else if (astAction is ASTStatement astStmt)
        {
            var stmt = new IRStatement
            {
                Func = new(new(astStmt.Name, astStmt.Params.Count)),
                Goal = null,
                Not = astStmt.Not,
                Params = new(astStmt.Params.Count),
                Location = astStmt.Location
            };

            foreach (var param in astStmt.Params)
            {
                stmt.Params.Add(ASTValueToIR(rule, param));
            }

            return stmt;
        }
        else
        {
            throw new InvalidOperationException("Cannot convert unknown AST condition type to IR");
        }
    }

    private IRCondition ASTConditionToIR(IRRule rule, ASTCondition astCondition)
    {
        if (astCondition is ASTFuncCondition astFunc)
        {
            var func = new IRFuncCondition
            {
                Func = new(new(astFunc.Name, astFunc.Params.Count)),
                Not = astFunc.Not,
                Params = new(astFunc.Params.Count),
                TupleSize = -1,
                Location = astFunc.Location
            };

            foreach (var param in astFunc.Params)
            {
                func.Params.Add(ASTValueToIR(rule, param));
            }

            return func;
        }
        else if (astCondition is ASTBinaryCondition astBin)
        {
            return new IRBinaryCondition
            {
                LValue = ASTValueToIR(rule, astBin.LValue),
                Op = astBin.Op,
                RValue = ASTValueToIR(rule, astBin.RValue),
                TupleSize = -1,
                Location = astBin.Location
            };
        }
        else
        {
            throw new InvalidOperationException("Cannot convert unknown AST condition type to IR");
        }
    }

    private IRValue ASTValueToIR(IRRule rule, ASTRValue astValue)
    {
        if (astValue is ASTConstantValue value)
        {
            return ASTConstantToIR(value);
        }
        else if (astValue is ASTLocalVar astVar)
        {
            // TODO - compiler error if type resolution fails
            ValueType type;
            if (astVar.Type != null)
            {
                type = Context.LookupType(astVar.Type);
                if (type == null)
                {
                    Context.Log.Error(astVar.Location, DiagnosticCode.UnresolvedType, $"Type \"{astVar.Type}\" does not exist");
                }
            }
            else
            {
                type = null;
            }

            var ruleVar = rule.FindOrAddVariable(astVar.Name, type);

            return new IRVariable
            {
                Index = ruleVar.Index,
                Type = type,
                Location = astVar.Location
            };
        }
        else
        {
            throw new InvalidOperationException("Cannot convert unknown AST value type to IR");
        }
    }

    private IRFact ASTFactToIR(IRGoal goal, ASTBaseFact astFact)
    {
        if (astFact is ASTFact fact1)
        {
            var fact = new IRFact
            {
                Database = new(new(fact1.Database, fact1.Elements.Count)),
                Not = fact1.Not,
                Elements = new(fact1.Elements.Count),
                Goal = null,
                Location = fact1.Location
            };

            foreach (var element in fact1.Elements)
            {
                fact.Elements.Add(ASTConstantToIR(element));
            }

            return fact;
        }
        else if (astFact is ASTGoalCompletedFact fact)
        {
            return new()
            {
                Database = null,
                Not = false,
                Elements = new(),
                Goal = goal,
                Location = fact.Location
            };
        }
        else
        {
            throw new InvalidOperationException("Cannot convert unknown AST fact type to IR");
        }
    }

    // TODO - un-copy + move to constant code?
    private ValueType ConstantTypeToValueType(IRConstantType type)
    {
        return type switch
        {
            IRConstantType.Unknown => null,
            // TODO - lookup type ID from enum
            IRConstantType.Integer => Context.TypesById[1],
            IRConstantType.Float   => Context.TypesById[3],
            IRConstantType.String  => Context.TypesById[4],
            IRConstantType.Name    => Context.TypesById[5],
            _                      => throw new ArgumentException("Invalid IR constant type")
        };
    }

    private IRConstant ASTConstantToIR(ASTConstantValue astConstant)
    {
        ValueType type;
        if (astConstant.TypeName != null)
        {
            type = Context.LookupType(astConstant.TypeName);
            if (type == null)
            {
                Context.Log.Error(astConstant.Location, DiagnosticCode.UnresolvedType, $"Type \"{astConstant.TypeName}\" does not exist");
            }
        }
        else
        {
            type = ConstantTypeToValueType(astConstant.Type);
        }

        return new()
        {
            ValueType = astConstant.Type,
            Type = type,
            InferredType = astConstant.TypeName != null,
            IntegerValue = astConstant.IntegerValue,
            FloatValue = astConstant.FloatValue,
            StringValue = astConstant.StringValue,
            Location = astConstant.Location
        };
    }

    public ASTGoal ParseGoal(string path, Stream stream)
    {
        var scanner = new GoalScanner(path);
        scanner.SetSource(stream);
        var parser = new OsirisParser.Goal.GoalParser(scanner);
        bool parsed = parser.Parse();

        if (parsed)
        {
            return parser.GetGoal();
        }
        else
        {
            LastLocation = scanner.LastLocation();
            return null;
        }
    }

    public IRGoal GenerateGoalIR(ASTGoal goal)
    {
        return ASTGoalToIR(goal);
    }
}