// Copyright Zero Games. All Rights Reserved.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ZeroGames.Forge.Runtime;

public class EntityMetadata
{
    
    public static void Get(Type entityType, out EntityMetadata metadata)
    {
        if (!entityType.IsClass || !entityType.IsAssignableTo(typeof(IEntity)))
        {
            throw new ArgumentOutOfRangeException(nameof(entityType));
        }
        
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(entityType, out var existing))
            {
                metadata = existing;
                return;
            }
			
            PrimaryKeyAttribute primaryKeyAttribute = entityType.GetCustomAttribute<PrimaryKeyAttribute>()!;
            metadata = new()
            {
                PrimaryKeyComponents = primaryKeyAttribute.Components.Select(component => entityType.GetProperty(component)!).ToArray(),
                RemainingProperties = entityType.GetProperties().Where(property => property.GetCustomAttribute<PropertyAttribute>() is not null && !primaryKeyAttribute.Components.Contains(property.Name)).ToArray(),
            };
            _cache[entityType] = metadata;
        }
    }
    
    public static void Get<T>(out EntityMetadata metadata) where T : class, IEntity => Get(typeof(T), out metadata);
    
    public required IReadOnlyList<PropertyInfo> PrimaryKeyComponents { get; init; }
    public required IReadOnlyList<PropertyInfo> RemainingProperties { get; init; }

    [field: MaybeNull]
    public IReadOnlyList<PropertyInfo> Properties => field ??= PrimaryKeyComponents.Concat(RemainingProperties).ToArray();

    public Type PrimaryKeyType => PrimaryKeyComponents.Count switch
    {
        1 => PrimaryKeyComponents[0].PropertyType,
        2 => typeof(ValueTuple<,>).MakeGenericType(PrimaryKeyComponents.Select(property => property.PropertyType).ToArray()),
        3 => typeof(ValueTuple<,,>).MakeGenericType(PrimaryKeyComponents.Select(property => property.PropertyType).ToArray()),
        4 => typeof(ValueTuple<,,,>).MakeGenericType(PrimaryKeyComponents.Select(property => property.PropertyType).ToArray()),
        5 => typeof(ValueTuple<,,,,>).MakeGenericType(PrimaryKeyComponents.Select(property => property.PropertyType).ToArray()),
        6 => typeof(ValueTuple<,,,,,>).MakeGenericType(PrimaryKeyComponents.Select(property => property.PropertyType).ToArray()),
        7 => typeof(ValueTuple<,,,,,,>).MakeGenericType(PrimaryKeyComponents.Select(property => property.PropertyType).ToArray()),
        _ => throw new NotSupportedException("Primary key more than 7-dimension is not supported."),
    };
    
    private static readonly Dictionary<Type, EntityMetadata> _cache = new();
    private static readonly Lock _cacheLock = new();
    
}