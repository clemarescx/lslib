﻿using System;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
using System.Numerics;
using System.Collections.Generic;
using System.Text;

namespace LSLib.LS;

public class LSJResourceConverter : JsonConverter
{
    private LSMetadata Metadata;

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Node) || objectType == typeof(Resource);
    }

    private TranslatedFSStringArgument ReadFSStringArgument(JsonReader reader)
    {
        var fs = new TranslatedFSStringArgument();
        string key = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndObject)
            {
                break;
            }

            switch (reader.TokenType)
            {
                case JsonToken.PropertyName:
                    key = reader.Value.ToString();
                    break;

                case JsonToken.String when key == "key":
                    fs.Key = reader.Value.ToString();
                    break;

                case JsonToken.String when key == "value":
                    fs.Value = reader.Value.ToString();
                    break;

                case JsonToken.String: throw new InvalidDataException($"Unknown property encountered during TranslatedFSString argument parsing: {key}");

                case JsonToken.StartObject when key == "string":
                    fs.String = ReadTranslatedFSString(reader);
                    break;

                default: throw new InvalidDataException($"Unexpected JSON token during parsing of TranslatedFSString argument: {reader.TokenType}");
            }
        }

        return fs;
    }

    private TranslatedFSString ReadTranslatedFSString(JsonReader reader)
    {
        var fs = new TranslatedFSString();
        var key = string.Empty;

        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.PropertyName)
            {
                key = reader.Value?.ToString();
            }
            else if (reader.TokenType == JsonToken.String)
            {
                switch (key)
                {
                    case "value":
                        fs.Value = reader.Value?.ToString();
                        break;

                    case "handle":
                        fs.Handle = reader.Value?.ToString();
                        break;

                    default: throw new InvalidDataException($"Unknown TranslatedFSString property: {key}");
                }
            }
            else if (reader.TokenType == JsonToken.StartArray && key == "arguments")
            {
                fs.Arguments = ReadFSStringArguments(reader);
            }
            else if (reader.TokenType == JsonToken.EndObject)
            {
                break;
            }
            else
            {
                throw new InvalidDataException($"Unexpected JSON token during parsing of TranslatedFSString: {reader.TokenType}");
            }
        }

        return fs;
    }

    private List<TranslatedFSStringArgument> ReadFSStringArguments(JsonReader reader)
    {
        var args = new List<TranslatedFSStringArgument>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                args.Add(ReadFSStringArgument(reader));
            }
            else if (reader.TokenType == JsonToken.EndArray)
            {
                break;
            }
            else
            {
                throw new InvalidDataException($"Unexpected JSON token during parsing of TranslatedFSString argument list: {reader.TokenType}");
            }
        }

        return args;
    }

    private NodeAttribute ReadAttribute(JsonReader reader)
    {
        string key = "", handle = null;
        List<TranslatedFSStringArgument> fsStringArguments = null;
        NodeAttribute attribute = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndObject)
            {
                break;
            }

            switch (reader.TokenType)
            {
                case JsonToken.PropertyName:
                    key = reader.Value.ToString();
                    break;

                case JsonToken.String or JsonToken.Integer or JsonToken.Float or JsonToken.Boolean or JsonToken.Null when key == "type":
                {
                    if (!uint.TryParse((string)reader.Value, out var type))
                    {
                        type = (uint)AttributeTypeMaps.TypeToId[(string)reader.Value];
                    }

                    attribute = new NodeAttribute((NodeAttribute.DataType)type);
                    attribute.Value = type switch
                    {
                        (uint)NodeAttribute.DataType.DT_TranslatedString   => new TranslatedString { Handle = handle },
                        (uint)NodeAttribute.DataType.DT_TranslatedFSString => new TranslatedFSString { Handle = handle, Arguments = fsStringArguments },
                        _                                                  => attribute.Value
                    };

                    break;
                }

                case JsonToken.String or JsonToken.Integer or JsonToken.Float or JsonToken.Boolean or JsonToken.Null when key == "value":
                    switch (attribute.Type)
                    {
                        case NodeAttribute.DataType.DT_Byte:
                            attribute.Value = Convert.ToByte(reader.Value);
                            break;

                        case NodeAttribute.DataType.DT_Short:
                            attribute.Value = Convert.ToInt16(reader.Value);
                            break;

                        case NodeAttribute.DataType.DT_UShort:
                            attribute.Value = Convert.ToUInt16(reader.Value);
                            break;

                        case NodeAttribute.DataType.DT_Int:
                            attribute.Value = Convert.ToInt32(reader.Value);
                            break;

                        case NodeAttribute.DataType.DT_UInt:
                            attribute.Value = Convert.ToUInt32(reader.Value);
                            break;

                        case NodeAttribute.DataType.DT_Float:
                            attribute.Value = Convert.ToSingle(reader.Value);
                            break;

                        case NodeAttribute.DataType.DT_Double:
                            attribute.Value = Convert.ToDouble(reader.Value);
                            break;

                        case NodeAttribute.DataType.DT_Bool:
                            attribute.Value = Convert.ToBoolean(reader.Value);
                            break;

                        case NodeAttribute.DataType.DT_String:
                        case NodeAttribute.DataType.DT_Path:
                        case NodeAttribute.DataType.DT_FixedString:
                        case NodeAttribute.DataType.DT_LSString:
                        case NodeAttribute.DataType.DT_WString:
                        case NodeAttribute.DataType.DT_LSWString:
                            attribute.Value = reader.Value.ToString();
                            break;

                        case NodeAttribute.DataType.DT_ULongLong:
                            attribute.Value = reader.Value switch
                            {
                                long value         => Convert.ToUInt64(value),
                                BigInteger integer => (ulong)integer,
                                _                  => (ulong)reader.Value
                            };

                            break;

                        // TODO: Not sure if this is the correct format
                        case NodeAttribute.DataType.DT_ScratchBuffer:
                            attribute.Value = Convert.FromBase64String(reader.Value.ToString());
                            break;

                        case NodeAttribute.DataType.DT_Long:
                        case NodeAttribute.DataType.DT_Int64:
                            attribute.Value = Convert.ToInt64(reader.Value);
                            break;

                        case NodeAttribute.DataType.DT_Int8:
                            attribute.Value = Convert.ToSByte(reader.Value);
                            break;

                        case NodeAttribute.DataType.DT_TranslatedString:
                        {
                            attribute.Value ??= new TranslatedString();

                            var ts = (TranslatedString)attribute.Value;
                            ts.Value = reader.Value.ToString();
                            ts.Handle = handle;
                            break;
                        }

                        case NodeAttribute.DataType.DT_TranslatedFSString:
                        {
                            attribute.Value ??= new TranslatedFSString();

                            var fsString = (TranslatedFSString)attribute.Value;
                            fsString.Value = reader.Value?.ToString();

                            fsString.Handle = handle;
                            fsString.Arguments = fsStringArguments;
                            attribute.Value = fsString;
                            break;
                        }

                        case NodeAttribute.DataType.DT_UUID:
                            attribute.Value = new Guid(reader.Value.ToString());
                            break;

                        case NodeAttribute.DataType.DT_IVec2:
                        case NodeAttribute.DataType.DT_IVec3:
                        case NodeAttribute.DataType.DT_IVec4:
                        {
                            var nums = reader.Value.ToString().Split(' ');
                            var length = attribute.GetColumns();
                            if (length != nums.Length)
                            {
                                throw new FormatException($"A vector of length {length} was expected, got {nums.Length}");
                            }

                            var vec = new int[length];
                            for (var i = 0; i < length; i++)
                            {
                                vec[i] = int.Parse(nums[i]);
                            }

                            attribute.Value = vec;
                            break;
                        }

                        case NodeAttribute.DataType.DT_Vec2:
                        case NodeAttribute.DataType.DT_Vec3:
                        case NodeAttribute.DataType.DT_Vec4:
                        {
                            var nums = reader.Value.ToString().Split(' ');
                            var length = attribute.GetColumns();
                            if (length != nums.Length)
                            {
                                throw new FormatException($"A vector of length {length} was expected, got {nums.Length}");
                            }

                            var vec = new float[length];
                            for (var i = 0; i < length; i++)
                            {
                                vec[i] = float.Parse(nums[i]);
                            }

                            attribute.Value = vec;
                            break;
                        }

                        case NodeAttribute.DataType.DT_Mat2:
                        case NodeAttribute.DataType.DT_Mat3:
                        case NodeAttribute.DataType.DT_Mat3x4:
                        case NodeAttribute.DataType.DT_Mat4x3:
                        case NodeAttribute.DataType.DT_Mat4:
                            var mat = Matrix.Parse(reader.Value.ToString());
                            if (mat.cols != attribute.GetColumns() || mat.rows != attribute.GetRows())
                            {
                                throw new FormatException("Invalid column/row count for matrix");
                            }

                            attribute.Value = mat;
                            break;

                        case NodeAttribute.DataType.DT_None:
                        default:
                            throw new NotImplementedException($"Don't know how to unserialize type {attribute.Type}");
                    }

                    break;

                case JsonToken.String or JsonToken.Integer or JsonToken.Float or JsonToken.Boolean or JsonToken.Null when key == "handle":
                {
                    if (attribute != null)
                    {
                        switch (attribute.Type)
                        {
                            case NodeAttribute.DataType.DT_TranslatedString:
                                attribute.Value ??= new TranslatedString();

                                ((TranslatedString)attribute.Value).Handle = reader.Value.ToString();
                                break;

                            case NodeAttribute.DataType.DT_TranslatedFSString:
                                attribute.Value ??= new TranslatedFSString();

                                ((TranslatedFSString)attribute.Value).Handle = reader.Value.ToString();
                                break;
                        }
                    }
                    else
                    {
                        handle = reader.Value.ToString();
                    }

                    break;
                }

                case JsonToken.String or JsonToken.Integer or JsonToken.Float or JsonToken.Boolean or JsonToken.Null when key == "version":
                {
                    attribute.Value ??= new TranslatedString();

                    var ts = (TranslatedString)attribute.Value;
                    ts.Version = ushort.Parse(reader.Value.ToString());
                    break;
                }

                case JsonToken.String or JsonToken.Integer or JsonToken.Float or JsonToken.Boolean or JsonToken.Null:
                    throw new InvalidDataException($"Unknown property encountered during attribute parsing: {key}");

                case JsonToken.StartArray when key == "arguments":
                {
                    var args = ReadFSStringArguments(reader);

                    if (attribute.Value != null)
                    {
                        var fs = (TranslatedFSString)attribute.Value;
                        fs.Arguments = args;
                    }
                    else
                    {
                        fsStringArguments = args;
                    }

                    break;
                }

                default: throw new InvalidDataException($"Unexpected JSON token during parsing of attribute: {reader.TokenType}");
            }
        }

        return attribute;
    }

    private Node ReadNode(JsonReader reader, Node node)
    {
        var key = "";
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndObject)
            {
                break;
            }

            switch (reader.TokenType)
            {
                case JsonToken.PropertyName:
                    key = reader.Value.ToString();
                    break;

                case JsonToken.StartObject:
                {
                    var attribute = ReadAttribute(reader);
                    node.Attributes.Add(key, attribute);
                    break;
                }

                case JsonToken.StartArray:
                {
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonToken.EndArray)
                        {
                            break;
                        }

                        if (reader.TokenType == JsonToken.StartObject)
                        {
                            var childNode = new Node
                            {
                                Name = key
                            };

                            ReadNode(reader, childNode);
                            node.AppendChild(childNode);
                            childNode.Parent = node;
                        }
                        else
                        {
                            throw new InvalidDataException($"Unexpected JSON token during parsing of child node list: {reader.TokenType}");
                        }
                    }

                    break;
                }

                default: throw new InvalidDataException($"Unexpected JSON token during parsing of node: {reader.TokenType}");
            }
        }

        return node;
    }

    private Resource ReadResource(JsonReader reader, Resource resource)
    {
        resource ??= new Resource();

        if (!reader.Read() || reader.TokenType != JsonToken.PropertyName || !reader.Value.Equals("save"))
        {
            throw new InvalidDataException("Expected JSON property 'save'");
        }

        if (!reader.Read() || reader.TokenType != JsonToken.StartObject)
        {
            throw new InvalidDataException($"Expected JSON object start token for 'save': {reader.TokenType}");
        }

        if (!reader.Read() || reader.TokenType != JsonToken.PropertyName || !reader.Value.Equals("header"))
        {
            throw new InvalidDataException("Expected JSON property 'header'");
        }

        if (!reader.Read() || reader.TokenType != JsonToken.StartObject)
        {
            throw new InvalidDataException($"Expected JSON object start token for 'header': {reader.TokenType}");
        }

        var key = "";
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndObject)
            {
                break;
            }

            switch (reader.TokenType)
            {
                case JsonToken.PropertyName:
                    key = reader.Value.ToString();
                    break;

                case JsonToken.String or JsonToken.Integer when key == "time":
                    resource.Metadata.Timestamp = Convert.ToUInt32(reader.Value);
                    break;

                case JsonToken.String or JsonToken.Integer when key == "version":
                {
                    var match = Regexes.VersionRegex.Match(reader.Value.ToString());
                    if (match.Success)
                    {
                        resource.Metadata.MajorVersion = Convert.ToUInt32(match.Groups[1].Value);
                        resource.Metadata.MinorVersion = Convert.ToUInt32(match.Groups[2].Value);
                        resource.Metadata.Revision = Convert.ToUInt32(match.Groups[3].Value);
                        resource.Metadata.BuildNumber = Convert.ToUInt32(match.Groups[4].Value);
                    }
                    else
                    {
                        throw new InvalidDataException($"Malformed version string: {reader.Value}");
                    }

                    break;
                }

                case JsonToken.String or JsonToken.Integer: throw new InvalidDataException($"Unknown property encountered during header parsing: {key}");
                default: throw new InvalidDataException($"Unexpected JSON token during parsing of header: {reader.TokenType}");
            }
        }

        if (!reader.Read() || reader.TokenType != JsonToken.PropertyName || !reader.Value.Equals("regions"))
        {
            throw new InvalidDataException("Expected JSON property 'regions'");
        }

        if (!reader.Read() || reader.TokenType != JsonToken.StartObject)
        {
            throw new InvalidDataException($"Expected JSON object start token for 'regions': {reader.TokenType}");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndObject)
            {
                break;
            }

            switch (reader.TokenType)
            {
                case JsonToken.PropertyName:
                    key = reader.Value.ToString();
                    break;

                case JsonToken.StartObject:
                {
                    var region = new Region();
                    ReadNode(reader, region);
                    region.Name = key;
                    region.RegionName = key;
                    resource.Regions.Add(key, region);
                    break;
                }

                default: throw new InvalidDataException($"Unexpected JSON token during parsing of region list: {reader.TokenType}");
            }
        }

        return resource;
    }

    public override object ReadJson(
        JsonReader reader,
        Type objectType,
        object existingValue,
        JsonSerializer serializer)
    {
        if (objectType == typeof(Node))
        {
            return ReadNode(reader, existingValue as Node);
        }

        if (objectType == typeof(Resource))
        {
            return ReadResource(reader, existingValue as Resource);
        }

        throw new InvalidOperationException("Cannot unserialize unknown type");
    }

    private void WriteResource(JsonWriter writer, Resource resource, JsonSerializer serializer)
    {
        Metadata = resource.Metadata;
        writer.WriteStartObject();

        writer.WritePropertyName("save");
        writer.WriteStartObject();

        writer.WritePropertyName("header");
        writer.WriteStartObject();
        writer.WritePropertyName("time");
        writer.WriteValue(resource.Metadata.Timestamp);
        writer.WritePropertyName("version");
        var versionString = $"{resource.Metadata.MajorVersion}.{resource.Metadata.MinorVersion}.{resource.Metadata.Revision}.{resource.Metadata.BuildNumber}";
        writer.WriteValue(versionString);
        writer.WriteEndObject();

        writer.WritePropertyName("regions");
        writer.WriteStartObject();
        foreach (var region in resource.Regions)
        {
            writer.WritePropertyName(region.Key);
            WriteNode(writer, region.Value, serializer);
        }

        writer.WriteEndObject();

        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private void WriteTranslatedFSString(JsonWriter writer, TranslatedFSString fs)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("value");
        WriteTranslatedFSStringInner(writer, fs);
        writer.WriteEndObject();
    }

    private void WriteTranslatedFSStringInner(JsonWriter writer, TranslatedFSString fs)
    {
        writer.WriteValue(fs.Value);
        writer.WritePropertyName("handle");
        writer.WriteValue(fs.Handle);
        writer.WritePropertyName("arguments");
        writer.WriteStartArray();
        foreach (var arg in fs.Arguments)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("key");
            writer.WriteValue(arg.Key);
            writer.WritePropertyName("string");
            WriteTranslatedFSString(writer, arg.String);
            writer.WritePropertyName("value");
            writer.WriteValue(arg.Value);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private void WriteNode(JsonWriter writer, Node node, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        foreach (var attribute in node.Attributes)
        {
            writer.WritePropertyName(attribute.Key);
            writer.WriteStartObject();
            writer.WritePropertyName("type");
            if (Metadata.MajorVersion >= 4)
            {
                writer.WriteValue(AttributeTypeMaps.IdToType[attribute.Value.Type]);
            }
            else
            {
                writer.WriteValue((int)attribute.Value.Type);
            }

            if (attribute.Value.Type != NodeAttribute.DataType.DT_TranslatedString)
            {
                writer.WritePropertyName("value");
            }

            switch (attribute.Value.Type)
            {
                case NodeAttribute.DataType.DT_Byte:
                    writer.WriteValue(Convert.ToByte(attribute.Value.Value));
                    break;

                case NodeAttribute.DataType.DT_Short:
                    writer.WriteValue(Convert.ToInt16(attribute.Value.Value));
                    break;

                case NodeAttribute.DataType.DT_UShort:
                    writer.WriteValue(Convert.ToUInt16(attribute.Value.Value));
                    break;

                case NodeAttribute.DataType.DT_Int:
                    writer.WriteValue(Convert.ToInt32(attribute.Value.Value));
                    break;

                case NodeAttribute.DataType.DT_UInt:
                    writer.WriteValue(Convert.ToUInt32(attribute.Value.Value));
                    break;

                case NodeAttribute.DataType.DT_Float:
                    writer.WriteValue(Convert.ToSingle(attribute.Value.Value));
                    break;

                case NodeAttribute.DataType.DT_Double:
                    writer.WriteValue(Convert.ToDouble(attribute.Value.Value));
                    break;

                case NodeAttribute.DataType.DT_Bool:
                    writer.WriteValue(Convert.ToBoolean(attribute.Value.Value));
                    break;

                case NodeAttribute.DataType.DT_String:
                case NodeAttribute.DataType.DT_Path:
                case NodeAttribute.DataType.DT_FixedString:
                case NodeAttribute.DataType.DT_LSString:
                case NodeAttribute.DataType.DT_WString:
                case NodeAttribute.DataType.DT_LSWString:
                    writer.WriteValue(attribute.Value.ToString());
                    break;

                case NodeAttribute.DataType.DT_ULongLong:
                    writer.WriteValue(Convert.ToUInt64(attribute.Value.Value));
                    break;

                // TODO: Not sure if this is the correct format
                case NodeAttribute.DataType.DT_ScratchBuffer:
                    writer.WriteValue(Convert.ToBase64String((byte[])attribute.Value.Value));
                    break;

                case NodeAttribute.DataType.DT_Long:
                case NodeAttribute.DataType.DT_Int64:
                    writer.WriteValue(Convert.ToInt64(attribute.Value.Value));
                    break;

                case NodeAttribute.DataType.DT_Int8:
                    writer.WriteValue(Convert.ToSByte(attribute.Value.Value));
                    break;

                case NodeAttribute.DataType.DT_TranslatedString:
                {
                    var ts = (TranslatedString)attribute.Value.Value;

                    if (ts.Value != null)
                    {
                        writer.WritePropertyName("value");
                        writer.WriteValue(ts.Value);
                    }

                    if (ts.Version > 0)
                    {
                        writer.WritePropertyName("version");
                        writer.WriteValue(ts.Version);
                    }

                    writer.WritePropertyName("handle");
                    writer.WriteValue(ts.Handle);
                    break;
                }

                case NodeAttribute.DataType.DT_TranslatedFSString:
                {
                    var fs = (TranslatedFSString)attribute.Value.Value;
                    WriteTranslatedFSStringInner(writer, fs);
                    break;
                }

                case NodeAttribute.DataType.DT_UUID:
                    writer.WriteValue(((Guid)attribute.Value.Value).ToString());
                    break;

                // TODO: haven't seen any vectors/matrices in D:OS JSON files so far
                case NodeAttribute.DataType.DT_Vec2:
                case NodeAttribute.DataType.DT_Vec3:
                case NodeAttribute.DataType.DT_Vec4:
                {
                    var vec = (float[])attribute.Value.Value;
                    writer.WriteValue(string.Join(" ", vec));
                    break;
                }

                case NodeAttribute.DataType.DT_IVec2:
                case NodeAttribute.DataType.DT_IVec3:
                case NodeAttribute.DataType.DT_IVec4:
                {
                    var ivec = (int[])attribute.Value.Value;
                    writer.WriteValue(string.Join(" ", ivec));
                    break;
                }

                case NodeAttribute.DataType.DT_Mat2:
                case NodeAttribute.DataType.DT_Mat3:
                case NodeAttribute.DataType.DT_Mat3x4:
                case NodeAttribute.DataType.DT_Mat4x3:
                case NodeAttribute.DataType.DT_Mat4:
                {
                    var mat = (Matrix)attribute.Value.Value;
                    var str = new StringBuilder();
                    for (var r = 0; r < mat.rows; r++)
                    {
                        for (var c = 0; c < mat.cols; c++)
                        {
                            str.Append($"{mat[r, c]} ");
                        }

                        str.AppendLine();
                    }

                    writer.WriteValue(str.ToString());
                    break;
                }

                case NodeAttribute.DataType.DT_None:
                default:
                    throw new NotImplementedException($"Don't know how to serialize type {attribute.Value.Type}");
            }

            writer.WriteEndObject();
        }

        foreach (var children in node.Children)
        {
            writer.WritePropertyName(children.Key);
            writer.WriteStartArray();
            foreach (var child in children.Value)
            {
                WriteNode(writer, child, serializer);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        switch (value)
        {
            case Node node:
                WriteNode(writer, node, serializer);
                break;

            case Resource resource:
                WriteResource(writer, resource, serializer);
                break;

            default: throw new InvalidOperationException("Cannot serialize unknown type");
        }
    }
}