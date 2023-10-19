namespace LSLibLite.LS;

public static class AttributeTypeMaps
{
    #region Members

    public static readonly Dictionary<string, NodeAttribute.DataType> TypeToId = new()
    {
        { "None", NodeAttribute.DataType.DT_None },
        { "uint8", NodeAttribute.DataType.DT_Byte },
        { "int16", NodeAttribute.DataType.DT_Short },
        { "uint16", NodeAttribute.DataType.DT_UShort },
        { "int32", NodeAttribute.DataType.DT_Int },
        { "uint32", NodeAttribute.DataType.DT_UInt },
        { "float", NodeAttribute.DataType.DT_Float },
        { "double", NodeAttribute.DataType.DT_Double },
        { "ivec2", NodeAttribute.DataType.DT_IVec2 },
        { "ivec3", NodeAttribute.DataType.DT_IVec3 },
        { "ivec4", NodeAttribute.DataType.DT_IVec4 },
        { "fvec2", NodeAttribute.DataType.DT_Vec2 },
        { "fvec3", NodeAttribute.DataType.DT_Vec3 },
        { "fvec4", NodeAttribute.DataType.DT_Vec4 },
        { "mat2x2", NodeAttribute.DataType.DT_Mat2 },
        { "mat3x3", NodeAttribute.DataType.DT_Mat3 },
        { "mat3x4", NodeAttribute.DataType.DT_Mat3x4 },
        { "mat4x3", NodeAttribute.DataType.DT_Mat4x3 },
        { "mat4x4", NodeAttribute.DataType.DT_Mat4 },
        { "bool", NodeAttribute.DataType.DT_Bool },
        { "string", NodeAttribute.DataType.DT_String },
        { "path", NodeAttribute.DataType.DT_Path },
        { "FixedString", NodeAttribute.DataType.DT_FixedString },
        { "LSString", NodeAttribute.DataType.DT_LSString },
        { "uint64", NodeAttribute.DataType.DT_ULongLong },
        { "ScratchBuffer", NodeAttribute.DataType.DT_ScratchBuffer },
        { "old_int64", NodeAttribute.DataType.DT_Long },
        { "int8", NodeAttribute.DataType.DT_Int8 },
        { "TranslatedString", NodeAttribute.DataType.DT_TranslatedString },
        { "WString", NodeAttribute.DataType.DT_WString },
        { "LSWString", NodeAttribute.DataType.DT_LSWString },
        { "guid", NodeAttribute.DataType.DT_UUID },
        { "int64", NodeAttribute.DataType.DT_Int64 },
        { "TranslatedFSString", NodeAttribute.DataType.DT_TranslatedFSString }
    };

    public static readonly Dictionary<NodeAttribute.DataType, string> IdToType = new()
    {
        { NodeAttribute.DataType.DT_None, "None" },
        { NodeAttribute.DataType.DT_Byte, "uint8" },
        { NodeAttribute.DataType.DT_Short, "int16" },
        { NodeAttribute.DataType.DT_UShort, "uint16" },
        { NodeAttribute.DataType.DT_Int, "int32" },
        { NodeAttribute.DataType.DT_UInt, "uint32" },
        { NodeAttribute.DataType.DT_Float, "float" },
        { NodeAttribute.DataType.DT_Double, "double" },
        { NodeAttribute.DataType.DT_IVec2, "ivec2" },
        { NodeAttribute.DataType.DT_IVec3, "ivec3" },
        { NodeAttribute.DataType.DT_IVec4, "ivec4" },
        { NodeAttribute.DataType.DT_Vec2, "fvec2" },
        { NodeAttribute.DataType.DT_Vec3, "fvec3" },
        { NodeAttribute.DataType.DT_Vec4, "fvec4" },
        { NodeAttribute.DataType.DT_Mat2, "mat2x2" },
        { NodeAttribute.DataType.DT_Mat3, "mat3x3" },
        { NodeAttribute.DataType.DT_Mat3x4, "mat3x4" },
        { NodeAttribute.DataType.DT_Mat4x3, "mat4x3" },
        { NodeAttribute.DataType.DT_Mat4, "mat4x4" },
        { NodeAttribute.DataType.DT_Bool, "bool" },
        { NodeAttribute.DataType.DT_String, "string" },
        { NodeAttribute.DataType.DT_Path, "path" },
        { NodeAttribute.DataType.DT_FixedString, "FixedString" },
        { NodeAttribute.DataType.DT_LSString, "LSString" },
        { NodeAttribute.DataType.DT_ULongLong, "uint64" },
        { NodeAttribute.DataType.DT_ScratchBuffer, "ScratchBuffer" },
        { NodeAttribute.DataType.DT_Long, "old_int64" },
        { NodeAttribute.DataType.DT_Int8, "int8" },
        { NodeAttribute.DataType.DT_TranslatedString, "TranslatedString" },
        { NodeAttribute.DataType.DT_WString, "WString" },
        { NodeAttribute.DataType.DT_LSWString, "LSWString" },
        { NodeAttribute.DataType.DT_UUID, "guid" },
        { NodeAttribute.DataType.DT_Int64, "int64" },
        { NodeAttribute.DataType.DT_TranslatedFSString, "TranslatedFSString" }
    };

    #endregion
}