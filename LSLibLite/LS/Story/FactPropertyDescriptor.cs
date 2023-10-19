using System.ComponentModel;

namespace LSLibLite.LS.Story;

internal class FactPropertyDescriptor : PropertyDescriptor
{
    #region Constructors

    public FactPropertyDescriptor(int index, Value.Type baseType, byte type) : base(index.ToString(), Array.Empty<Attribute>())
    {
        Index = index;
        BaseType = baseType;
        Type = type;
    }

    #endregion

    #region Properties

    public override bool IsReadOnly => false;
    public byte Type { get; private set; }
    public int Index { get; }
    public Value.Type BaseType { get; }

    public override Type ComponentType => typeof(Fact);

    public override Type PropertyType
    {
        get
        {
            return BaseType switch
            {
                Value.Type.Integer    => typeof(int),
                Value.Type.Integer64  => typeof(long),
                Value.Type.Float      => typeof(float),
                Value.Type.String     => typeof(string),
                Value.Type.GuidString => typeof(string),
                Value.Type.None       => throw new InvalidOperationException("Cannot retrieve type of an unknown column"),
                _                     => throw new InvalidOperationException("Cannot retrieve type of an unknown column")
            };
        }
    }

    #endregion

    public override bool CanResetValue(object component)
    {
        return false;
    }

    public override object? GetValue(object? component)
    {
        if (component is Fact f)
        {
            return f.Columns[Index].ToString();
        }

        return null;
    }

    public override void ResetValue(object component)
    {
        throw new NotImplementedException();
    }

    public override void SetValue(object? component, object? value)
    {
        var fact = (Fact)component;
        var column = fact.Columns[Index];

        switch (BaseType)
        {
            case Value.Type.Integer:
            {
                column.IntValue = value switch
                {
                    string s => int.Parse(s),
                    int i    => i,
                    _        => throw new ArgumentException("Invalid Int32 value")
                };

                break;
            }

            case Value.Type.Integer64:
            {
                column.Int64Value = value switch
                {
                    string s => long.Parse(s),
                    long l   => l,
                    _        => throw new ArgumentException("Invalid Int64 value")
                };

                break;
            }

            case Value.Type.Float:
            {
                column.FloatValue = value switch
                {
                    string s => float.Parse(s),
                    float f  => f,
                    _        => throw new ArgumentException("Invalid float value")
                };

                break;
            }

            case Value.Type.String:
            case Value.Type.GuidString:
            {
                column.StringValue = (string)value;
                break;
            }

            case Value.Type.None:
            default:
                throw new InvalidOperationException("Cannot retrieve type of an unknown column");
        }
    }

    public override bool ShouldSerializeValue(object component)
    {
        return false;
    }
}