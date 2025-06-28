// Copyright Zero Games. All Rights Reserved.

using System.Reflection;

namespace ZeroGames.Forge.Runtime;

public interface ICompositeDataTypeMetadata
{
    public static bool IsCompositeProperty(PropertyInfo property)
    {
        Type propertyType = property.PropertyType;
        if (propertyType.IsAssignableTo(typeof(IEntity)) || propertyType.IsAssignableTo(typeof(IStruct)))
        {
            return true;
        }
        
        Type? elementType = null;
        if (propertyType.GetGenericInstanceOf(typeof(IReadOnlyList<>)) is { } genericListType)
        {
            elementType = genericListType.GetGenericArguments()[0];
        }
        else if (propertyType.GetGenericInstanceOf(typeof(IReadOnlySet<>)) is { } genericSetType)
        {
            elementType = genericSetType.GetGenericArguments()[0];
        }
        else if (propertyType.GetGenericInstanceOf(typeof(IReadOnlyDictionary<,>)) is { } genericMapType)
        {
            elementType = genericMapType.GetGenericArguments()[1];
        }

        if (elementType is null)
        {
            return false;
        }

        return elementType.IsAssignableTo(typeof(IEntity)) || elementType.IsAssignableTo(typeof(IStruct));
    }
    
    public Type DataType { get; }
    public IReadOnlyList<PropertyInfo> Properties { get; }
    // We want to cache the following results so they can't be computed.
    public IReadOnlyList<PropertyInfo> MapProperties { get; }
    public IReadOnlyList<PropertyInfo> CompositeProperties { get; } // Property is a reference/struct or a container of reference/struct.
}


