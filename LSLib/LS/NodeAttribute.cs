using System;
using System.Collections.Generic;
using System.Globalization;

namespace LSLib.LS;

public class TranslatedString
{
    public ushort Version = 0;
    public string Value;
    public string Handle;

    public override string ToString()
    {
        return Value;
    }
}

public class TranslatedFSStringArgument
{
    public string Key;
    public TranslatedFSString String;
    public string Value;
}

public class TranslatedFSString : TranslatedString
{
    public List<TranslatedFSStringArgument> Arguments;
}

public class NodeAttribute
{
    public enum DataType
    {
        DT_None = 0,
        DT_Byte = 1,
        DT_Short = 2,
        DT_UShort = 3,
        DT_Int = 4,
        DT_UInt = 5,
        DT_Float = 6,
        DT_Double = 7,
        DT_IVec2 = 8,
        DT_IVec3 = 9,
        DT_IVec4 = 10,
        DT_Vec2 = 11,
        DT_Vec3 = 12,
        DT_Vec4 = 13,
        DT_Mat2 = 14,
        DT_Mat3 = 15,
        DT_Mat3x4 = 16,
        DT_Mat4x3 = 17,
        DT_Mat4 = 18,
        DT_Bool = 19,
        DT_String = 20,
        DT_Path = 21,
        DT_FixedString = 22,
        DT_LSString = 23,
        DT_ULongLong = 24,
        DT_ScratchBuffer = 25,

        // Seems to be unused?
        DT_Long = 26,
        DT_Int8 = 27,
        DT_TranslatedString = 28,
        DT_WString = 29,
        DT_LSWString = 30,
        DT_UUID = 31,
        DT_Int64 = 32,
        DT_TranslatedFSString = 33,

        // Last supported datatype, always keep this one at the end
        DT_Max = DT_TranslatedFSString
    }

    public DataType Type { get; }

    public object Value { get; set; }

    public NodeAttribute(DataType type)
    {
        Type = type;
    }

    public override string ToString() =>
        Type switch
        {
            DataType.DT_ScratchBuffer =>
                // ScratchBuffer is a special case, as its stored as byte[] and ToString() doesn't really do what we want
                Convert.ToBase64String((byte[])Value),
            DataType.DT_IVec2 => string.Join(" ", new List<int>((int[])Value).ConvertAll(i => i.ToString()).ToArray()),
            DataType.DT_IVec3 => string.Join(" ", new List<int>((int[])Value).ConvertAll(i => i.ToString()).ToArray()),
            DataType.DT_IVec4 => string.Join(" ", new List<int>((int[])Value).ConvertAll(i => i.ToString()).ToArray()),
            DataType.DT_Vec2  => string.Join(" ", new List<float>((float[])Value).ConvertAll(i => i.ToString(CultureInfo.InvariantCulture)).ToArray()),
            DataType.DT_Vec3  => string.Join(" ", new List<float>((float[])Value).ConvertAll(i => i.ToString(CultureInfo.InvariantCulture)).ToArray()),
            DataType.DT_Vec4  => string.Join(" ", new List<float>((float[])Value).ConvertAll(i => i.ToString(CultureInfo.InvariantCulture)).ToArray()),
            _                 => Value.ToString()
        };

    public int GetRows() =>
        Type switch
        {
            DataType.DT_IVec2 or DataType.DT_IVec3 or DataType.DT_IVec4 or DataType.DT_Vec2 or DataType.DT_Vec3 or DataType.DT_Vec4 => 1,
            DataType.DT_Mat2 => 2,
            DataType.DT_Mat3 or DataType.DT_Mat3x4 => 3,
            DataType.DT_Mat4x3 or DataType.DT_Mat4 => 4,
            _ => throw new NotSupportedException("Data type does not have rows")
        };

    public int GetColumns() =>
        Type switch
        {
            DataType.DT_IVec2 or DataType.DT_Vec2 or DataType.DT_Mat2 => 2,
            DataType.DT_IVec3 or DataType.DT_Vec3 or DataType.DT_Mat3 or DataType.DT_Mat4x3 => 3,
            DataType.DT_IVec4 or DataType.DT_Vec4 or DataType.DT_Mat3x4 or DataType.DT_Mat4 => 4,
            _ => throw new NotSupportedException("Data type does not have columns")
        };

    private static bool IsNumeric(DataType dataType) =>
        dataType is DataType.DT_Byte
                 or DataType.DT_Short
                 or DataType.DT_Short
                 or DataType.DT_Int
                 or DataType.DT_UInt
                 or DataType.DT_Float
                 or DataType.DT_Double
                 or DataType.DT_ULongLong
                 or DataType.DT_Long
                 or DataType.DT_Int8;

    public void FromString(string str)
    {
        if (IsNumeric(Type))
        {
            // Workaround: Some XML files use empty strings, instead of "0" for zero values.
            if (str == string.Empty)
            {
                str = "0";
            }
            // Handle hexadecimal integers in XML files
            else if (str.Length > 2 && str[..2] == "0x")
            {
                str = Convert.ToUInt64(str[2..], 16).ToString();
            }
        }

        switch (Type)
        {
            case DataType.DT_None:
                // This is a null type, cannot have a value
                Value = null;
                break;

            case DataType.DT_Byte:
                Value = Convert.ToByte(str);
                break;

            case DataType.DT_Short:
                Value = Convert.ToInt16(str);
                break;

            case DataType.DT_UShort:
                Value = Convert.ToUInt16(str);
                break;

            case DataType.DT_Int:
                Value = Convert.ToInt32(str);
                break;

            case DataType.DT_UInt:
                Value = Convert.ToUInt32(str);
                break;

            case DataType.DT_Float:
                Value = Convert.ToSingle(str);
                break;

            case DataType.DT_Double:
                Value = Convert.ToDouble(str);
                break;

            case DataType.DT_IVec2:
            case DataType.DT_IVec3:
            case DataType.DT_IVec4:
            {
                var nums = str.Split(' ');
                var length = GetColumns();
                if (length != nums.Length)
                {
                    throw new FormatException($"A vector of length {length} was expected, got {nums.Length}");
                }

                var vec = new int[length];
                for (var i = 0; i < length; i++)
                {
                    vec[i] = int.Parse(nums[i]);
                }

                Value = vec;
                break;
            }

            case DataType.DT_Vec2:
            case DataType.DT_Vec3:
            case DataType.DT_Vec4:
            {
                var nums = str.Split(' ');
                var length = GetColumns();
                if (length != nums.Length)
                {
                    throw new FormatException($"A vector of length {length} was expected, got {nums.Length}");
                }

                var vec = new float[length];
                for (var i = 0; i < length; i++)
                {
                    vec[i] = float.Parse(nums[i]);
                }

                Value = vec;
                break;
            }

            case DataType.DT_Mat2:
            case DataType.DT_Mat3:
            case DataType.DT_Mat3x4:
            case DataType.DT_Mat4x3:
            case DataType.DT_Mat4:
                var mat = Matrix.Parse(str);
                if (mat.cols != GetColumns() || mat.rows != GetRows())
                {
                    throw new FormatException("Invalid column/row count for matrix");
                }

                Value = mat;
                break;

            case DataType.DT_Bool:
                Value = str switch
                {
                    "0" => false,
                    "1" => true,
                    _   => Convert.ToBoolean(str)
                };

                break;

            case DataType.DT_String:
            case DataType.DT_Path:
            case DataType.DT_FixedString:
            case DataType.DT_LSString:
            case DataType.DT_WString:
            case DataType.DT_LSWString:
                Value = str;
                break;

            case DataType.DT_TranslatedString:
                // We'll only set the value part of the translated string, not the TranslatedStringKey / Handle part
                // That can be changed separately via attribute.Value.Handle
                Value ??= new TranslatedString();

                ((TranslatedString)Value).Value = str;
                break;

            case DataType.DT_TranslatedFSString:
                // We'll only set the value part of the translated string, not the TranslatedStringKey / Handle part
                // That can be changed separately via attribute.Value.Handle
                Value ??= new TranslatedFSString();

                ((TranslatedFSString)Value).Value = str;
                break;

            case DataType.DT_ULongLong:
                Value = Convert.ToUInt64(str);
                break;

            case DataType.DT_ScratchBuffer:
                Value = Convert.FromBase64String(str);
                break;

            case DataType.DT_Long:
            case DataType.DT_Int64:
                Value = Convert.ToInt64(str);
                break;

            case DataType.DT_Int8:
                Value = Convert.ToSByte(str);
                break;

            case DataType.DT_UUID:
                Value = new Guid(str);
                break;

            default:
                // This should not happen!
                throw new NotImplementedException($"FromString() not implemented for type {Type}");
        }
    }
}