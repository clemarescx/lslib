using LSLib.LS.Story.GoalParser;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LSLib.LS.Stats.StatParser;

/// <summary>
/// A collection of sub-stats.
/// </summary>
using StatCollection = List<object>;

/// <summary>
/// Declarations node - contains every declaration from the story header file.
/// </summary>
using StatDeclarations = List<StatDeclaration>;


public abstract class StatScanBase : AbstractScanner<object, CodeLocation>
{
    protected string fileName;

    public override CodeLocation yylloc { get; set; }
        
    protected virtual bool yywrap() { return true; }

    protected string MakeLiteral(string lit) => lit;

    protected string MakeString(string lit)
    {
        return MakeLiteral(Regex.Unescape(lit.Substring(1, lit.Length - 2)));
    }

    protected StatProperty MakeDataProperty(CodeLocation location, string lit)
    {
        var re = new Regex(@"data\s+""([^""]+)""\s+""(.*)""\s*", RegexOptions.CultureInvariant);
        var matches = re.Match(lit);
        if (!matches.Success)
        {
            throw new("Stat data entry match error");
        }

        return new()
        {
            Key = matches.Groups[1].Value,
            Value = matches.Groups[2].Value,
            Location = location
        };
    }
}

public partial class StatScanner
{
    public StatScanner(string fileName)
    {
        this.fileName = fileName;
    }

    public CodeLocation LastLocation()
    {
        return new(null, tokLin, tokCol, tokELin, tokECol);
    }
}

public partial class StatParser
{
    public StatParser(StatScanner scnr) : base(scnr)
    {
    }

    public StatDeclarations GetDeclarations()
    {
        return (StatDeclarations)CurrentSemanticValue;
    }

    private StatDeclarations MakeDeclarationList() => new();

    private StatDeclarations AddDeclaration(object declarations, object declaration)
    {
        var decls = (StatDeclarations)declarations;
        decls.Add((StatDeclaration)declaration);
        return decls;
    }

    private StatDeclaration MakeDeclaration() => new();

    private StatDeclaration MakeDeclaration(CodeLocation location) => new()
    {
        Location = location
    };

    private StatDeclaration MakeDeclaration(CodeLocation location, StatProperty[] properties)
    {
        var decl = new StatDeclaration()
        {
            Location = location
        };
        foreach (var prop in properties)
        {
            AddProperty(decl, prop);
        }

        return decl;
    }

    private StatDeclaration MakeDeclaration(StatProperty[] properties)
    {
        return MakeDeclaration(null, properties);
    }

    private StatDeclaration MergeItemCombo(object comboNode, object resultNode)
    {
        var combo = (StatDeclaration)comboNode;
        var result = (StatDeclaration)resultNode;
        foreach (var kv in result.Properties)
        {
            if (kv.Key != "EntityType" && kv.Key != "Name")
            {
                combo.Properties[kv.Key] = kv.Value;
            }
        }

        return combo;
    }

    private StatDeclaration AddProperty(object declaration, object property)
    {
        var decl = (StatDeclaration)declaration;
        if (property is StatProperty)
        {
            var prop = (StatProperty)property;
            decl.Properties[prop.Key] = prop.Value;
            if (prop.Location != null)
            {
                decl.PropertyLocations[prop.Key] = prop.Location;
            }
        }
        else if (property is StatElement)
        {
            var ele = (StatElement)property;
            if (!decl.Properties.TryGetValue(ele.Collection, out var cont))
            {
                cont = new List<object>();
                decl.Properties[ele.Collection] = cont;
            }

            (cont as List<object>).Add(ele.Value);
        }
        else if (property is StatDeclaration)
        {
            var otherDecl = (StatDeclaration)property;
            foreach (var kv in otherDecl.Properties)
            {
                decl.Properties[kv.Key] = kv.Value;
            }

            foreach (var kv in otherDecl.PropertyLocations)
            {
                decl.PropertyLocations[kv.Key] = kv.Value;
            }
        }
        else
        {
            throw new("Unknown property type");
        }

        return decl;
    }

    private StatProperty MakeProperty(object key, object value) => new()
    {
        Key = (string)key,
        Value = (string)value
    };

    private StatProperty MakeProperty(string key, object value) => new()
    {
        Key = key,
        Value = (string)value
    };

    private StatProperty MakeProperty(string key, string value) => new()
    {
        Key = key,
        Value = value
    };

    private StatProperty MakeProperty(CodeLocation location, object key, object value) => new()
    {
        Key = (string)key,
        Value = (string)value,
        Location = location
    };

    private StatProperty MakeProperty(CodeLocation location, string key, object value) => new()
    {
        Key = key,
        Value = (string)value,
        Location = location
    };

    private StatProperty MakeProperty(CodeLocation location, string key, string value) => new()
    {
        Key = key,
        Value = value,
        Location = location
    };

    private StatElement MakeElement(string key, object value)
    {
        if (value is string)
        {
            return new()
            {
                Collection = key,
                Value = (string)value
            };
        }
        else if (value is StatCollection)
        {
            return new()
            {
                Collection = key,
                Value = (StatCollection)value
            };
        }
        else if (value is Dictionary<string, object>)
        {
            return new()
            {
                Collection = key,
                Value = (Dictionary<string, object>)value
            };
        }
        else if (value is StatDeclaration)
        {
            return new()
            {
                Collection = key,
                Value = ((StatDeclaration)value).Properties
            };
        }
        else
        {
            throw new("Unknown stat element type");
        }
    }

    private StatCollection MakeCollection() => new();

    private StatCollection AddElement(object collection, object element)
    {
        var coll = (StatCollection)collection;
        var ele = (string)element;
        coll.Add(ele);

        return coll;
    }

    private string Unwrap(object node) => (string)node;
}