﻿using System.Diagnostics;
using System.Xml;
using LSLibLite.LS.Enums;

namespace LSLibLite.LS.Resources.LSX;

public sealed class LSXReader : ILSReader
{
    #region Members

    private int lastLine, lastColumn;
    private List<Node?> stack;
    private LSXVersion Version = LSXVersion.V3;
    private Region? currentRegion;
    private Resource? resource;
    private readonly Stream? stream;
    private XmlReader reader;

    #endregion

    #region Constructors

    public LSXReader(Stream? stream)
    {
        this.stream = stream;
    }

    #endregion

    public void Dispose()
    {
        stream.Dispose();
    }

    public Resource? Read()
    {
        resource = new Resource();
        currentRegion = null;
        stack = new List<Node?>();
        lastLine = lastColumn = 0;
        var resultResource = resource;

        try
        {
            ReadInternal();
        }
        catch (Exception e)
        {
            if (lastLine > 0)
            {
                throw new Exception($"Parsing error at or near line {lastLine}, column {lastColumn}:{Environment.NewLine}{e.Message}", e);
            }
            else
            {
                throw;
            }
        }
        finally
        {
            resource = null;
            currentRegion = null;
            stack = null;
        }

        return resultResource;
    }

    private void ReadTranslatedFSString(TranslatedFSString? fs)
    {
        fs.Value = reader["value"];
        fs.Handle = reader["handle"];
        Debug.Assert(fs.Handle != null);

        var arguments = Convert.ToInt32(reader["arguments"]);
        fs.Arguments = new List<TranslatedFSStringArgument>(arguments);
        if (arguments <= 0)
        {
            return;
        }

        while (reader.Read() && reader.NodeType != XmlNodeType.Element) { }

        if (reader.Name != "arguments")
        {
            throw new InvalidFormatException($"Expected <arguments>: {reader.Name}");
        }

        var processedArgs = 0;
        while (processedArgs < arguments && reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            if (reader.Name != "argument")
            {
                throw new InvalidFormatException($"Expected <argument>: {reader.Name}");
            }

            var arg = new TranslatedFSStringArgument
            {
                Key = reader["key"],
                Value = reader["value"]
            };

            while (reader.Read() && reader.NodeType != XmlNodeType.Element) { }

            if (reader.Name != "string")
            {
                throw new InvalidFormatException($"Expected <string>: {reader.Name}");
            }

            arg.String = new TranslatedFSString();
            ReadTranslatedFSString(arg.String);

            fs.Arguments.Add(arg);
            processedArgs++;

            while (reader.Read() && reader.NodeType != XmlNodeType.EndElement) { }
        }

        while (reader.Read() && reader.NodeType != XmlNodeType.EndElement) { }

        // Close outer element
        while (reader.Read() && reader.NodeType != XmlNodeType.EndElement) { }

        Debug.Assert(processedArgs == arguments);
    }

    private void ReadElement()
    {
        switch (reader.Name)
        {
            case "save":
                // Root element
                if (stack.Any())
                {
                    throw new InvalidFormatException("Node <save> was unexpected.");
                }

                break;

            case "header":
                // LSX metadata part 1
                resource.Metadata.Timestamp = Convert.ToUInt64(reader["time"]);
                break;

            case "version":
                // LSX metadata part 2
                resource.Metadata.MajorVersion = Convert.ToUInt32(reader["major"]);
                resource.Metadata.MinorVersion = Convert.ToUInt32(reader["minor"]);
                resource.Metadata.Revision = Convert.ToUInt32(reader["revision"]);
                resource.Metadata.BuildNumber = Convert.ToUInt32(reader["build"]);
                Version = resource.Metadata.MajorVersion >= 4
                    ? LSXVersion.V4
                    : LSXVersion.V3;

                break;

            case "region":
                if (currentRegion != null)
                {
                    throw new InvalidFormatException("A <region> can only start at the root level of a resource.");
                }

                Debug.Assert(!reader.IsEmptyElement);
                var region = new Region
                {
                    RegionName = reader["id"]
                };

                Debug.Assert(region.RegionName != null);
                resource.Regions.Add(region.RegionName, region);
                currentRegion = region;
                break;

            case "node":
                if (currentRegion == null)
                {
                    throw new InvalidFormatException("A <node> must be located inside a region.");
                }

                Node? node;
                if (!stack.Any())
                {
                    // The node is the root node of the region
                    node = currentRegion;
                }
                else
                {
                    // New node under the current parent
                    node = new Node
                    {
                        Parent = stack[^1]
                    };
                }

                node.Name = reader["id"];
                Debug.Assert(node.Name != null);
                node.Parent.AppendChild(node);

                if (!reader.IsEmptyElement)
                {
                    stack.Add(node);
                }

                break;

            case "attribute":
                uint attrTypeId;
                if (Version >= LSXVersion.V4)
                {
                    attrTypeId = (uint)AttributeTypeMaps.TypeToId[reader["type"]];
                }
                else
                {
                    if (!uint.TryParse(reader["type"], out attrTypeId))
                    {
                        attrTypeId = (uint)AttributeTypeMaps.TypeToId[reader["type"]];
                    }
                }

                var attrName = reader["id"];
                if (attrTypeId > (int)NodeAttribute.DataType.DT_Max)
                {
                    throw new InvalidFormatException($"Unsupported attribute data type: {attrTypeId}");
                }

                Debug.Assert(attrName != null);
                var attr = new NodeAttribute((NodeAttribute.DataType)attrTypeId);

                var attrValue = reader["value"];
                if (attrValue != null)
                {
                    attr.FromString(attrValue);
                }

                switch (attr.Type)
                {
                    case NodeAttribute.DataType.DT_TranslatedString:
                    {
                        attr.Value ??= new TranslatedString();

                        var ts = (TranslatedString)attr.Value;
                        ts.Handle = reader["handle"];
                        Debug.Assert(ts.Handle != null);

                        if (attrValue == null)
                        {
                            ts.Version = ushort.Parse(reader["version"]);
                        }

                        break;
                    }

                    case NodeAttribute.DataType.DT_TranslatedFSString:
                    {
                        var fs = (TranslatedFSString)attr.Value;
                        ReadTranslatedFSString(fs);
                        break;
                    }
                }

                stack[^1].Attributes.Add(attrName, attr);
                break;

            case "children":
                // Child nodes are handled in the "node" case
                break;

            default:
                throw new InvalidFormatException($"Unknown element encountered: {reader.Name}");
        }
    }

    private void ReadEndElement()
    {
        switch (reader.Name)
        {
            case "save":
            case "header":
            case "version":
            case "attribute":
            case "children":
                // These elements don't change the stack, just discard them
                break;

            case "region":
                Debug.Assert(stack.Count == 0);
                Debug.Assert(currentRegion != null);
                Debug.Assert(currentRegion.Name != null);
                currentRegion = null;
                break;

            case "node":
                stack.RemoveAt(stack.Count - 1);
                break;

            default:
                throw new InvalidFormatException($"Unknown element encountered: {reader.Name}");
        }
    }

    private void ReadInternal()
    {
        using (reader = XmlReader.Create(stream))
        {
            try
            {
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            ReadElement();
                            break;

                        case XmlNodeType.EndElement:
                            ReadEndElement();
                            break;

                        default: throw new ArgumentOutOfRangeException();
                    }
                }
            }
            catch (Exception)
            {
                lastLine = ((IXmlLineInfo)reader).LineNumber;
                lastColumn = ((IXmlLineInfo)reader).LinePosition;
                throw;
            }
        }
    }
}