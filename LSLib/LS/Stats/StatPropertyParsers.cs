﻿using LSLib.LS.Stats.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LSLib.LS.Stats;

public interface IStatValueParser
{
    object Parse(string value, ref bool succeeded, ref string errorText);
}

public class StatReferenceConstraint
{
    public string StatType;
    public string StatSubtype;
}

public interface IStatReferenceValidator
{
    bool IsValidReference(string reference, string statType, string statSubtype);
}

public class BooleanParser : IStatValueParser
{
    public object Parse(string value, ref bool succeeded, ref string errorText)
    {
        if (value is "Yes" or "No")
        {
            succeeded = true;
            return value == "Yes";
        }
        else
        {
            succeeded = false;
            errorText = "expected boolean value 'Yes' or 'No'";
            return null;
        }
    }
}

public class Int32Parser : IStatValueParser
{
    public object Parse(string value, ref bool succeeded, ref string errorText)
    {
        if (int.TryParse(value, out int intval))
        {
            succeeded = true;
            return intval;
        }
        else
        {
            succeeded = false;
            errorText = "expected an integer value";
            return null;
        }
    }
}

public class FloatParser : IStatValueParser
{
    public object Parse(string value, ref bool succeeded, ref string errorText)
    {
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatval))
        {
            succeeded = true;
            return floatval;
        }
        else
        {
            succeeded = false;
            errorText = "expected a float value";
            return null;
        }
    }
}

public class EnumParser : IStatValueParser
{
    private readonly StatEnumeration Enumeration;

    public EnumParser(StatEnumeration enumeration)
    {
        Enumeration = enumeration ?? throw new ArgumentNullException();
    }

    public object Parse(string value, ref bool succeeded, ref string errorText)
    {
        if (value is null or "")
        {
            value = "None";
        }

        if (Enumeration.ValueToIndexMap.ContainsKey(value))
        {
            succeeded = true;
            return value;
        }
        else
        {
            succeeded = false;
            errorText = $"expected one of: {string.Join(", ", Enumeration.Values.Take(4))}, ...";
            return null;
        }
    }
}

public class MultiValueEnumParser : IStatValueParser
{
    private readonly EnumParser Parser;

    public MultiValueEnumParser(StatEnumeration enumeration)
    {
        Parser = new(enumeration);
    }

    public object Parse(string value, ref bool succeeded, ref string errorText)
    {
        succeeded = true;

        foreach (var item in value.Split(new char[] { ';' }))
        {
            Parser.Parse(item.Trim(new char[] { ' ' }), ref succeeded, ref errorText);
            if (!succeeded)
            {
                errorText = $"Value '{item}' not supported; {errorText}";
                return null;
            }
        }

        return value;
    }
}

public class StringParser : IStatValueParser
{
    public object Parse(string value, ref bool succeeded, ref string errorText)
    {
        succeeded = true;
        return value;
    }
}

public class UUIDParser : IStatValueParser
{
    public object Parse(string value, ref bool succeeded, ref string errorText)
    {
        if (Guid.TryParseExact(value, "D", out Guid parsed))
        {
            succeeded = true;
            return parsed;
        }
        else
        {
            errorText = $"'{value}' is not a valid UUID";
            succeeded = false;
            return null;
        }
    }
}

public class StatReferenceParser : IStatValueParser
{
    private IStatReferenceValidator Validator;
    private List<StatReferenceConstraint> Constraints;

    public StatReferenceParser(IStatReferenceValidator validator, List<StatReferenceConstraint> constraints)
    {
        Validator = validator;
        Constraints = constraints;
    }
        
    public object Parse(string value, ref bool succeeded, ref string errorText)
    {
        foreach (var constraint in Constraints)
        {
            if (Validator.IsValidReference(value, constraint.StatType, constraint.StatSubtype))
            {
                succeeded = true;
                return value;
            }
        }

        var refTypes = string.Join("/", Constraints.Select(c => c.StatType));
        errorText = $"'{value}' is not a valid {refTypes} reference";
        succeeded = false;
        return null;
    }
}

public class MultiValueStatReferenceParser : IStatValueParser
{
    private readonly StatReferenceParser Parser;

    public MultiValueStatReferenceParser(IStatReferenceValidator validator, List<StatReferenceConstraint> constraints)
    {
        Parser = new(validator, constraints);
    }

    public object Parse(string value, ref bool succeeded, ref string errorText)
    {
        succeeded = true;

        foreach (var item in value.Split(new char[] { ';' }))
        {
            var trimmed = item.Trim(new char[] { ' ' });
            if (trimmed.Length > 0)
            {
                Parser.Parse(trimmed, ref succeeded, ref errorText);
                if (!succeeded)
                {
                    return null;
                }
            }
        }

        return value;
    }
}

public class ExpressionParser : IStatValueParser
{
    private readonly string ExpressionType;
    private readonly StatDefinitionRepository Definitions;
    private readonly StatValueParserFactory ParserFactory;

    public ExpressionParser(string expressionType, StatDefinitionRepository definitions,
        StatValueParserFactory parserFactory)
    {
        ExpressionType = expressionType;
        Definitions = definitions;
        ParserFactory = parserFactory;
    }
        
    public virtual object Parse(string value, ref bool succeeded, ref string errorText)
    {
        var valueBytes = Encoding.UTF8.GetBytes($"__TYPE_{ExpressionType}__ {value}");
        using var buf = new MemoryStream(valueBytes);
        List<string> errorTexts = new();

        var scanner = new StatPropertyScanner();
        scanner.SetSource(buf);
        var parser = new StatPropertyParser(scanner, Definitions, ParserFactory);
        parser.OnError += (string message) => errorTexts.Add(message);
        succeeded = parser.Parse();
        if (!succeeded)
        {
            var location = scanner.LastLocation();
            var column = location.StartColumn - 10 - ExpressionType.Length + 1;
            errorText = $"Syntax error at or near character {column}";
            return null;
        }
        else if (errorTexts.Count > 0)
        {
            succeeded = false;
            errorText = string.Join("; ", errorTexts);
            return null;
        }
        else
        {
            succeeded = true;
            return parser.GetParsedObject();
        }
    }
}

public class ConditionsParser : IStatValueParser
{
    private readonly ExpressionParser ExprParser;

    public ConditionsParser(StatDefinitionRepository definitions, StatValueParserFactory parserFactory)
    {
        ExprParser = new("Conditions", definitions, parserFactory);
    }

    public object Parse(string value, ref bool succeeded, ref string errorText)
    {
        value = value
               .Replace(" ", "")
               .Replace(";", "&")
               .Trim(new char[] { '&' });

        return ExprParser.Parse(value, ref succeeded, ref errorText);
    }
}

public class StatValueParserFactory
{
    private readonly IStatReferenceValidator ReferenceValidator;

    public StatValueParserFactory(IStatReferenceValidator referenceValidator)
    {
        ReferenceValidator = referenceValidator;
    }

    public IStatValueParser CreateReferenceParser(List<StatReferenceConstraint> constraints)
    {
        return new StatReferenceParser(ReferenceValidator, constraints);
    }

    public IStatValueParser CreateParser(StatField field, StatDefinitionRepository definitions)
    {
        return field.Type switch
        {
            "Requirements"     => new ExpressionParser("Requirements", definitions, this),
            "Properties"       => new ExpressionParser("Properties", definitions, this),
            "Conditions"       => new ConditionsParser(definitions, this),
            "Enumeration"      => new EnumParser(field.EnumType),
            "EnumerationList"  => new MultiValueEnumParser(field.EnumType),
            "Boolean"          => new BooleanParser(),
            "Integer"          => new Int32Parser(),
            "Float"            => new FloatParser(),
            "UUID"             => new UUIDParser(),
            "RootTemplate"     => new UUIDParser(),
            "StatReference"    => new StatReferenceParser(ReferenceValidator, field.ReferenceTypes),
            "StatReferences"   => new MultiValueStatReferenceParser(ReferenceValidator, field.ReferenceTypes),
            "BaseClass"        => new StringParser(),
            "Name"             => new StringParser(),
            "String"           => new StringParser(),
            "TranslatedString" => new StringParser(),
            "Comment"          => new StringParser(),
            "Color"            => new StringParser(),
            _                  => throw new ArgumentException($"Could not create parser for type '{field.Type}'")
        };
    }
}