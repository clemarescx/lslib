using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LSLib.Granny.GR2;

public static class Helpers
{
    private static Dictionary<Type, ObjectCtor> CachedConstructors = new();
    private static Dictionary<Type, ArrayCtor> CachedArrayConstructors = new();

    public delegate object ObjectCtor();
    public delegate object ArrayCtor(int size);

    public static ObjectCtor GetConstructor(Type type)
    {
        if (!CachedConstructors.TryGetValue(type, out var ctor))
        {
            NewExpression newExp = Expression.New(type);
            LambdaExpression lambda = Expression.Lambda(typeof(ObjectCtor), newExp, new ParameterExpression[] { });
            ctor = (ObjectCtor)lambda.Compile();
            CachedConstructors.Add(type, ctor);
        }

        return ctor;
    }

    public static object CreateInstance(Type type)
    {
        ObjectCtor ctor = GetConstructor(type);
        return ctor();
    }

    public static object CreateArrayInstance(Type type, int size)
    {
        if (!CachedArrayConstructors.TryGetValue(type, out var ctor))
        {
            var typeCtor = type.GetConstructor(new Type[] { typeof(int) });
            var sizeParam = Expression.Parameter(typeof(int), "");
            NewExpression newExp = Expression.New(typeCtor, new Expression[] { sizeParam });
            LambdaExpression lambda = Expression.Lambda(typeof(ArrayCtor), newExp, new ParameterExpression[] { sizeParam });
            ctor = (ArrayCtor)lambda.Compile();
            CachedArrayConstructors.Add(type, ctor);
        }

        return ctor(size);
    }
}

class UInt8ListSerializer : NodeSerializer
{
    public object Read(GR2Reader gr2, StructDefinition definition, MemberDefinition member, uint arraySize, object parent)
    {
        var controls = new List<byte>((int)arraySize);
        for (int i = 0; i < arraySize; i++)
            controls.Add(gr2.Reader.ReadByte());
        return controls;
    }

    public void Write(GR2Writer writer, WritableSection section, MemberDefinition member, object obj)
    {
        var items = obj as List<byte>;
        for (int i = 0; i < items.Count; i++)
            section.Writer.Write(items[i]);
    }
}


class UInt16ListSerializer : NodeSerializer
{
    public object Read(GR2Reader gr2, StructDefinition definition, MemberDefinition member, uint arraySize, object parent)
    {
        var controls = new List<ushort>((int)arraySize);
        for (int i = 0; i < arraySize; i++)
            controls.Add(gr2.Reader.ReadUInt16());
        return controls;
    }

    public void Write(GR2Writer writer, WritableSection section, MemberDefinition member, object obj)
    {
        var items = obj as List<ushort>;
        for (int i = 0; i < items.Count; i++)
            section.Writer.Write(items[i]);
    }
}


class Int16ListSerializer : NodeSerializer
{
    public object Read(GR2Reader gr2, StructDefinition definition, MemberDefinition member, uint arraySize, object parent)
    {
        var controls = new List<short>((int)arraySize);
        for (int i = 0; i < arraySize; i++)
            controls.Add(gr2.Reader.ReadInt16());
        return controls;
    }

    public void Write(GR2Writer writer, WritableSection section, MemberDefinition member, object obj)
    {
        var items = obj as List<short>;
        for (int i = 0; i < items.Count; i++)
            section.Writer.Write(items[i]);
    }
}


class UInt32ListSerializer : NodeSerializer
{
    public object Read(GR2Reader gr2, StructDefinition definition, MemberDefinition member, uint arraySize, object parent)
    {
        var controls = new List<uint>((int)arraySize);
        for (int i = 0; i < arraySize; i++)
            controls.Add(gr2.Reader.ReadUInt32());
        return controls;
    }

    public void Write(GR2Writer writer, WritableSection section, MemberDefinition member, object obj)
    {
        var items = obj as List<uint>;
        for (int i = 0; i < items.Count; i++)
            section.Writer.Write(items[i]);
    }
}


class Int32ListSerializer : NodeSerializer
{
    public object Read(GR2Reader gr2, StructDefinition definition, MemberDefinition member, uint arraySize, object parent)
    {
        var controls = new List<int>((int)arraySize);
        for (int i = 0; i < arraySize; i++)
            controls.Add(gr2.Reader.ReadInt32());
        return controls;
    }

    public void Write(GR2Writer writer, WritableSection section, MemberDefinition member, object obj)
    {
        var items = obj as List<int>;
        for (int i = 0; i < items.Count; i++)
            section.Writer.Write(items[i]);
    }
}


class SingleListSerializer : NodeSerializer
{
    public object Read(GR2Reader gr2, StructDefinition definition, MemberDefinition member, uint arraySize, object parent)
    {
        var controls = new List<float>((int)arraySize);
        for (int i = 0; i < arraySize; i++)
            controls.Add(gr2.Reader.ReadSingle());
        return controls;
    }

    public void Write(GR2Writer writer, WritableSection section, MemberDefinition member, object obj)
    {
        var items = obj as List<float>;
        for (int i = 0; i < items.Count; i++)
            section.Writer.Write(items[i]);
    }
}