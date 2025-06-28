// Copyright Zero Games. All Rights Reserved.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ZeroGames.Forge.Runtime;

public class StructMetadata : ICompositeDataTypeMetadata
{
    
    public static void Get(Type structType, out StructMetadata metadata)
    {
        if (!structType.IsClass || !structType.IsAssignableTo(typeof(IStruct)))
        {
            throw new ArgumentOutOfRangeException(nameof(structType));
        }
        
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(structType, out var existing))
            {
                metadata = existing;
                return;
            }
			
            metadata = new()
            {
                StructType = structType,
                Properties = structType.GetProperties().Where(property => property.GetCustomAttribute<PropertyAttribute>() is not null).ToArray(),
            };
            _cache[structType] = metadata;
        }
    }
    
    public static void Get<T>(out StructMetadata metadata) where T : class, IStruct => Get(typeof(T), out metadata);
    
    public required Type StructType { get; init; }
    public required IReadOnlyList<PropertyInfo> Properties { get; init; }

    private static readonly Dictionary<Type, StructMetadata> _cache = new();
    private static readonly Lock _cacheLock = new();
    
    #region ICompositeDataTypeMetadata Interfaces

    Type ICompositeDataTypeMetadata.DataType => StructType;
    
    [field: MaybeNull]
    public IReadOnlyList<PropertyInfo> MapProperties => field ??= Properties.Where(property => property.PropertyType.IsAssignableToSomeGenericInstanceOf(typeof(IReadOnlyDictionary<,>))).ToArray();
    
    [field: MaybeNull]
    public IReadOnlyList<PropertyInfo> CompositeProperties => field ??= Properties.Where(ICompositeDataTypeMetadata.IsCompositeProperty).ToArray();
    
    #endregion
    
}