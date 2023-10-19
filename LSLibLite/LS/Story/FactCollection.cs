using System.ComponentModel;

namespace LSLibLite.LS.Story;

public class FactCollection : List<Fact>, ITypedList
{
    #region Members

    private readonly Database _database;
    private PropertyDescriptorCollection? _properties;
    private readonly Story _story;

    #endregion

    #region Constructors

    public FactCollection(Database database, Story story)
    {
        _database = database;
        _story = story;
    }

    #endregion

    public PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors)
    {
        if (_properties != null)
        {
            return _properties;
        }

        var props = new List<PropertyDescriptor>();
        var types = _database.Parameters.Types;
        for (var i = 0; i < types.Count; i++)
        {
            var type = _story.Types[types[i]];
            Value.Type baseType;
            if (type.Alias != 0)
            {
                baseType = (Value.Type)type.Alias;
            }
            else
            {
                baseType = (Value.Type)type.Index;
            }

            props.Add(new FactPropertyDescriptor(i, baseType, type.Index));
        }

        _properties = new PropertyDescriptorCollection(props.ToArray(), true);

        return _properties;
    }

    public string GetListName(PropertyDescriptor[] listAccessors)
    {
        return "";
    }
}