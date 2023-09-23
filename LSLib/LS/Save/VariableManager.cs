using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK;

namespace LSLib.LS.Save;

public class OsirisVariableHelper
{
    private readonly Dictionary<string, int> _identifierToKey = new();
    private readonly Dictionary<int, string> _keyToIdentifier = new();

    public void Load(Node helper)
    {
        foreach (var mapping in helper.Children["IdentifierTable"])
        {
            var name = (string)mapping.Attributes["MapKey"].Value;
            var index = (int)mapping.Attributes["MapValue"].Value;
            _identifierToKey.Add(name, index);
            _keyToIdentifier.Add(index, name);
        }
    }

    public int GetKey(string variableName)
    {
        return _identifierToKey[variableName];
    }

    public string GetName(int variableIndex)
    {
        return _keyToIdentifier[variableIndex];
    }
}

public abstract class VariableHolder<TValue>
{
    protected readonly List<TValue> Values = new();
    private readonly List<ushort> _remaps = new();

    public TValue GetRaw(int index)
    {
        if (index == 0)
        {
            return default;
        }

        var valueSlot = _remaps[index - 1];
        return Values[valueSlot];
    }

    public void Load(Node variableList)
    {
        LoadVariables(variableList);

        var remaps = (byte[])variableList.Attributes["Remaps"].Value;

        _remaps.Clear();
        _remaps.Capacity = remaps.Length / 2;

        using var ms = new MemoryStream(remaps);
        using var reader = new BinaryReader(ms);
        for (var i = 0; i < remaps.Length / 2; i++)
        {
            _remaps.Add(reader.ReadUInt16());
        }
    }

    protected abstract void LoadVariables(Node variableList);
}

public class IntVariableHolder : VariableHolder<int>
{
    public int? Get(int index)
    {
        var raw = GetRaw(index);
        if ((uint)raw == 0xbaadf00d) /* -1163005939 */
        {
            return null;
        }

        return raw;
    }

    protected override void LoadVariables(Node variableList)
    {
        var variables = (byte[])variableList.Attributes["Variables"].Value;
        var numVars = variables.Length / 4;

        Values.Clear();
        Values.Capacity = numVars;

        using var ms = new MemoryStream(variables);
        using var reader = new BinaryReader(ms);
        for (var i = 0; i < numVars; i++)
        {
            Values.Add(reader.ReadInt32());
        }
    }
}

public class Int64VariableHolder : VariableHolder<long>
{
    public long? Get(int index)
    {
        var raw = GetRaw(index);
        if ((ulong)raw == 0xbaadf00dbaadf00d) /* -4995072469926809587 */
        {
            return null;
        }

        return raw;
    }

    protected override void LoadVariables(Node variableList)
    {
        var variables = (byte[])variableList.Attributes["Variables"].Value;
        var numVars = variables.Length / 8;

        Values.Clear();
        Values.Capacity = numVars;

        using var ms = new MemoryStream(variables);
        using var reader = new BinaryReader(ms);
        for (var i = 0; i < numVars; i++)
        {
            Values.Add(reader.ReadInt64());
        }
    }
}

public class FloatVariableHolder : VariableHolder<float>
{
    public float? Get(int index)
    {
        var raw = GetRaw(index);
        var intFloat = BitConverter.ToUInt32(BitConverter.GetBytes(raw), 0);
        if (intFloat == 0xbaadf00d)
        {
            return null;
        }

        return raw;
    }

    protected override void LoadVariables(Node variableList)
    {
        var variables = (byte[])variableList.Attributes["Variables"].Value;
        var numVars = variables.Length / 4;

        Values.Clear();
        Values.Capacity = numVars;

        using var ms = new MemoryStream(variables);
        using var reader = new BinaryReader(ms);
        for (var i = 0; i < numVars; i++)
        {
            Values.Add(reader.ReadSingle());
        }
    }
}

public class StringVariableHolder : VariableHolder<string>
{
    public string Get(int index)
    {
        var raw = GetRaw(index);
        if (raw == "0xbaadf00d")
        {
            return null;
        }

        return raw;
    }

    protected override void LoadVariables(Node variableList)
    {
        var variables = (byte[])variableList.Attributes["Variables"].Value;

        using var ms = new MemoryStream(variables);
        using var reader = new BinaryReader(ms);
        var numVars = reader.ReadInt32();

        Values.Clear();
        Values.Capacity = numVars;

        for (var i = 0; i < numVars; i++)
        {
            var length = reader.ReadUInt16();
            var bytes = reader.ReadBytes(length);
            var str = Encoding.UTF8.GetString(bytes);
            Values.Add(str);
        }
    }
}

public class Float3VariableHolder : VariableHolder<Vector3>
{
    public Vector3? Get(int index)
    {
        var raw = GetRaw(index);
        var intFloat = BitConverter.ToUInt32(BitConverter.GetBytes(raw.X), 0);
        if (intFloat == 0xbaadf00d)
        {
            return null;
        }

        return raw;
    }

    protected override void LoadVariables(Node variableList)
    {
        var variables = (byte[])variableList.Attributes["Variables"].Value;
        var numVars = variables.Length / 12;

        Values.Clear();
        Values.Capacity = numVars;

        using var ms = new MemoryStream(variables);
        using var reader = new BinaryReader(ms);
        for (var i = 0; i < numVars; i++)
        {
            Vector3 vec = new()
            {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Z = reader.ReadSingle()
            };

            Values.Add(vec);
        }
    }
}

internal enum VariableType
{
    Int = 0,
    Int64 = 1,
    Float = 2,
    String = 3,
    FixedString = 4,
    Float3 = 5
}

/// <summary>
/// Node (structure) entry in the LSF file
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct Key2TableEntry
{
    /// <summary>
    /// Index of variable from OsirisVariableHelper.IdentifierTable
    /// </summary>
    public UInt32 NameIndex;

    /// <summary>
    /// Index and type of value
    /// </summary>
    public UInt32 ValueIndexAndType;

    /// <summary>
    /// Handle of the object that this variable is assigned to.
    /// </summary>
    public UInt64 Handle;

    /// <summary>
    /// Index of value in the appropriate variable list
    /// </summary>
    public readonly int ValueIndex => (int)(ValueIndexAndType >> 3 & 0x3ff);

    /// <summary>
    /// Type of value
    /// </summary>
    public readonly VariableType ValueType => (VariableType)(ValueIndexAndType & 7);
}

public class VariableManager
{
    private readonly OsirisVariableHelper _variableHelper;
    private readonly Dictionary<int, Key2TableEntry> _keys = new();
    private readonly IntVariableHolder _intList = new();
    private readonly Int64VariableHolder _int64List = new();
    private readonly FloatVariableHolder _floatList = new();
    private readonly StringVariableHolder _stringList = new();
    private readonly StringVariableHolder _fixedStringList = new();
    private readonly Float3VariableHolder _float3List = new();

    public VariableManager(OsirisVariableHelper variableHelper)
    {
        _variableHelper = variableHelper;
    }

    public Dictionary<string, object> GetAll(bool includeDeleted = false)
    {
        var variables = new Dictionary<string, object>();
        foreach (var key in _keys.Values)
        {
            var name = _variableHelper.GetName((int)key.NameIndex);
            var value = includeDeleted
                ? GetRaw(key.ValueType, key.ValueIndex)
                : Get(key.ValueType, key.ValueIndex);

            if (value != null)
            {
                variables.Add(name, value);
            }
        }

        return variables;
    }

    public object Get(string name)
    {
        var index = _variableHelper.GetKey(name);
        var key = _keys[index];
        return Get(key.ValueType, key.ValueIndex);
    }

    private object Get(VariableType type, int index)
    {
        return type switch
        {
            VariableType.Int         => _intList.Get(index),
            VariableType.Int64       => _int64List.Get(index),
            VariableType.Float       => _floatList.Get(index),
            VariableType.String      => _stringList.Get(index),
            VariableType.FixedString => _fixedStringList.Get(index),
            VariableType.Float3      => _float3List.Get(index),
            _                        => throw new ArgumentException("Unsupported variable type")
        };
    }

    public object GetRaw(string name)
    {
        var index = _variableHelper.GetKey(name);
        var key = _keys[index];
        return GetRaw(key.ValueType, key.ValueIndex);
    }

    private object GetRaw(VariableType type, int index)
    {
        return type switch
        {
            VariableType.Int         => _intList.GetRaw(index),
            VariableType.Int64       => _int64List.GetRaw(index),
            VariableType.Float       => _floatList.GetRaw(index),
            VariableType.String      => _stringList.GetRaw(index),
            VariableType.FixedString => _fixedStringList.GetRaw(index),
            VariableType.Float3      => _float3List.GetRaw(index),
            _                        => throw new ArgumentException("Unsupported variable type")
        };
    }

    private void LoadKeys(byte[] handleList)
    {
        _keys.Clear();

        using var ms = new MemoryStream(handleList);
        using var reader = new BinaryReader(ms);
        var numHandles = reader.ReadInt32();
        for (var i = 0; i < numHandles; i++)
        {
            var entry = BinUtils.ReadStruct<Key2TableEntry>(reader);
            _keys.Add((int)entry.NameIndex, entry);
        }
    }

    public void Load(Node variableManager)
    {
        if (variableManager.Children.TryGetValue("IntList", out var nodes))
        {
            _intList.Load(nodes[0]);
        }

        if (variableManager.Children.TryGetValue("Int64List", out nodes))
        {
            _int64List.Load(nodes[0]);
        }

        if (variableManager.Children.TryGetValue("FloatList", out nodes))
        {
            _floatList.Load(nodes[0]);
        }

        if (variableManager.Children.TryGetValue("StringList", out nodes))
        {
            _stringList.Load(nodes[0]);
        }

        if (variableManager.Children.TryGetValue("FixedStringList", out nodes))
        {
            _fixedStringList.Load(nodes[0]);
        }

        if (variableManager.Children.TryGetValue("Float3List", out nodes))
        {
            _float3List.Load(nodes[0]);
        }

        if (variableManager.Children.TryGetValue("Key2TableList", out nodes))
        {
            var handleList = (byte[])nodes[0].Attributes["HandleList"].Value;
            LoadKeys(handleList);
        }
    }
}